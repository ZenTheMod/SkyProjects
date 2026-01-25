using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Sky;

namespace ZenSkies.Common.Systems.Compat;

[Autoload(Side = ModSide.Client)]
public static class CalamityFablesCompat
{
    private static readonly int[] edge_case_styles =
        [1, 2, 8, 9, 10, 13, 14];

    private const float moon_phase_rotation = 0.125f;

    private static readonly Color dark_atmosphere = new(13, 69, 96);

    private const float shatter_scale = 1.35f;

    private static readonly Vector4 atmosphere = Vector4.Zero;
    private static readonly Vector4 atmosphere_shadow = new(.1f, 0.02f, 0.06f, 1f);

    private static readonly Vector2 shatter_target_size = new(200);

    public static int PriorMoonStyles { get; private set; }

    public static bool IsEnabled { get; private set; }

    [ModSystemHooks.PostSetupContent]
    private static void PostSetupContent()
    {
        PriorMoonStyles = TextureAssets.Moon.Length;

        if (!ModLoader.TryGetMod("CalamityFables", out Mod fables))
        {
            return;
        }

        IsEnabled = true;

        Assembly fablessAsm = fables.Code;

        Type? moddedMoons = fablessAsm.GetType("CalamityFables.Core.ModdedMoons");

        Debug.Assert(moddedMoons is not null);

        FieldInfo? vanillaMoonCount = moddedMoons?.GetField("VanillaMoonCount", BindingFlags.Public | BindingFlags.Static);

        Debug.Assert(vanillaMoonCount is not null);

        int? count = (int?)vanillaMoonCount.GetValue(null);

        Debug.Assert(count is not null);

        PriorMoonStyles = (int)count;

        for (int i = 0; i < FablesTextures.Moon.Length; i++)
        {
            AddMoonStyle(PriorMoonStyles + i, FablesTextures.Moon[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEdgeCase()
    {
        return edge_case_styles.Contains(Main.moonType - PriorMoonStyles);
    }

    [SunAndMoonHooks.PreDrawMoonExtras]
    private static bool FablesExtras(
        GraphicsDevice device,
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        bool eventMoon
    )
    {
        if (eventMoon || !IsEdgeCase())
        {
            return true;
        }

        switch (Main.moonType - PriorMoonStyles)
        {
            case 1:
            {
                DrawDark(spriteBatch, moon.Value, position, rotation, scale);
                return false;
            }
            case 8:
            {
                DrawShatter(spriteBatch, moon.Value, position, color, rotation, scale, moonColor, shadowColor, device);
                return false;
            }
            case 9:
            {
                DrawCyst(spriteBatch, moon.Value, position, rotation, scale, moonColor, shadowColor);
                return false;
            }
        }

        return true;
    }

    private static void DrawDark(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale)
    {
        ApplyPlanetShader(Main.moonPhase * moon_phase_rotation, Color.Black, dark_atmosphere, Color.Transparent);

        Vector2 size = new(MoonSize * scale);

        spriteBatch.Draw(moon,
            position,
            null,
            Color.White,
            rotation,
            moon.Size() * 0.5f,
            size,
            SpriteEffects.None,
            0f
        );
    }

    private static void DrawShatter(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        Vector2 targetSize = shatter_target_size;

        RenderTargetLease leasedTarget = RenderTargetPool.Shared.Rent(
            device,
            (int)targetSize.X,
            (int)targetSize.Y,
            RenderTargetDescriptor.Default with { Depth = DepthFormat.Depth16 }
        );

        using (spriteBatch.Scope())
        {
            using (leasedTarget.Scope(clearColor: Color.Transparent))
            {
                device.Textures[0] = moon;

                device.RasterizerState = RasterizerState.CullNone;

                device.DepthStencilState = DepthStencilState.Default;

                Viewport viewport = device.Viewport;
                Vector2 screenSize = new(viewport.Width, viewport.Height);
                CompatEffects.Shatter.ScreenSize = screenSize;

                Matrix projection = ShatterMatrix();
                CompatEffects.Shatter.Projection = projection;

                CompatEffects.Shatter.Color = color.ToVector4();
                CompatEffects.Shatter.ShadowColor = shadowColor.ToVector4();

                CompatEffects.Shatter.InnerColor = Color.Red.ToVector4();

                float shadowAngle = Main.moonPhase * moon_phase_rotation;
                CompatEffects.Shatter.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

                CompatEffects.Shatter.Apply();

                Models.Shatter.DrawMoon(device);

                Models.Shatter.DrawRocks(device);

                device.Textures[0] = MiscTextures.Pixel;

                Models.Shatter.DrawShaderPlane(device);
            }
        }

        Vector2 size = new Vector2(MoonSize * scale * shatter_scale) / shatter_target_size;

        spriteBatch.Draw(
            leasedTarget.Target,
            position,
            null,
            Color.White,
            rotation,
            targetSize * 0.5f,
            size,
            SpriteEffects.None,
            0f
        );

        leasedTarget.Dispose();

        static Matrix ShatterMatrix()
        {
            return
                Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY) *
                Matrix.CreateOrthographicOffCenter(-1, 1, 1, -1, -1, 1);
        }
    }

    private static void DrawCyst(SpriteBatch spriteBatch, Texture2D moon, Vector2 position, float rotation, float scale, Color moonColor, Color shadowColor)
    {
        float shadowAngle = Main.moonPhase * moon_phase_rotation;

        CompatEffects.Cyst.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

        CompatEffects.Cyst.ShadowColor = shadowColor.ToVector4();
        CompatEffects.Cyst.AtmosphereColor = atmosphere;

        Vector4 atmoShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent.ToVector4() : atmosphere_shadow;
        CompatEffects.Cyst.AtmosphereShadowColor = atmoShadowColor;

        CompatEffects.Cyst.Apply();

        Vector2 size = new Vector2(MoonSize * scale) / moon.Size();

        spriteBatch.Draw(
            moon,
            position,
            null,
            moonColor,
            rotation,
            moon.Size() * 0.5f,
            size,
            SpriteEffects.None,
            0f
        );
    }
}
