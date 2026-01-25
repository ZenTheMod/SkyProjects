using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
public static partial class SunAndMoon
{
    private static readonly Color moon_sky_color = new(128, 168, 248);

    private const float default_moon_size = 62f;

    private const float moon_phase = 0.125f;

    private const float moon_radius = 0.88f;

    private static readonly Vector4 moon_atmosphere = Vector4.Zero;
    private static readonly Vector4 moon_atmosphere_shadow = new(0.2f, 0.04f, 0.12f, 1f);

    public static int MoonSize => TextureAssets.Moon[Main.moonType].Value.Width + 12;

    [OnLoad(Side = ModSide.Client)]
    private static void Rendering_Load()
    {
        On_Main.DrawSunAndMoon += DrawSunAndMoonToSky;
    }

    [ModCall]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DrawSunAndMoon(SpriteBatch spriteBatch, GraphicsDevice device, bool showSun, bool showMoon)
    {
        if (showSun)
        {
            DrawSun(spriteBatch, device);
        }
        if (showMoon){

            DrawMoon(spriteBatch, device);
        }
    }

    #region Sun

    [ModCall]
    public static void DrawSun(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Viewport viewport = device.Viewport;

        float centerX = viewport.Width * .5f;
        float distanceFromCenter = MathF.Abs(centerX - Info.SunPosition.X) / centerX;

        DrawSun(spriteBatch, Info.SunPosition, Info.SunColor, Info.SunRotation, Info.SunScale, distanceFromCenter, device);
    }

    [ModCall]
    public static void DrawSun(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, float distanceFromCenter, GraphicsDevice device)
    {
        const float outer_glow_scale = 0.35f;
        const float outer_glow_multiplier = 0.2f;
        const float inner_glow_scale = 0.23f;
        const float inner_glow_muliplier = 3.4f;
        const float huge_glow_scale = 0.7f;
        const float huge_glow_multiplier = 0.25f;

        const float fade_start = 1f;
        const float fade_end = 1.11f;

        color.A = 0;

        if (PreDrawSun.Invoke(spriteBatch, device, ref position, ref color, ref rotation, ref scale))
        {
            float offscreenMultiplier = Utils.Remap(distanceFromCenter, fade_start, fade_end, 1f, 0f);

            Texture2D bloom = SkyTextures.SunBloom;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            // Bloom
            Color outerGlowColor = color * outer_glow_multiplier;
            spriteBatch.Draw(bloom, position, null, outerGlowColor, 0, bloomOrigin, outer_glow_scale * scale, SpriteEffects.None, 0f);

            Color innerGlowColor = color * (1 + distanceFromCenter * inner_glow_muliplier);
            spriteBatch.Draw(bloom, position, null, innerGlowColor, 0, bloomOrigin, inner_glow_scale * scale, SpriteEffects.None, 0f);

            float hugeGlowMultiplier = Main.atmo * distanceFromCenter;
            Color hugeGlowColor = color * huge_glow_multiplier * offscreenMultiplier * hugeGlowMultiplier;
            spriteBatch.Draw(bloom, position, null, hugeGlowColor, 0, bloomOrigin, huge_glow_scale * hugeGlowMultiplier * scale, SpriteEffects.None, 0f);

            // No more astigmatism sun #dailytruthnukes
        }

        PostDrawSun.Invoke(spriteBatch, device, position, color, rotation, scale);
    }

    [PreDrawSun]
    private static bool PreDrawSun_Eclipse(SpriteBatch spriteBatch, ref Vector2 position, ref Color color, ref float rotation, ref float scale, GraphicsDevice device)
    {
        if (!Main.eclipse)
        {
            return true;
        }

        float[] glowScales = [0.36f, 0.27f, 0.2f];
        float[] glowMultipliers = [0.2f, 1f, 1.6f];

        Texture2D bloom = SkyTextures.SunBloom.Value;
        Vector2 bloomOrigin = bloom.Size() * 0.5f;

        color.A = 0;

        for (int i = 0; i < glowScales.Length; i++)
        {
            spriteBatch.Draw(bloom, position, null, color * glowMultipliers[i], 0, bloomOrigin, scale * glowScales[i], SpriteEffects.None, 0f);
        }

        DrawMoon(spriteBatch, position, Color.Black, rotation, scale, Color.Black, Color.Black, device);

        return false;
    }

    [PostDrawSun]
    public static void PostDrawSun_Sunglasses(
        SpriteBatch spriteBatch,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        GraphicsDevice device)
    {
        const float sunglasses_scale = 0.3f;

        if (Main.gameMenu || Main.LocalPlayer.head != 12)
        {
            return;
        }

        Texture2D sunglasses = SkyTextures.Sunglasses.Value;
        spriteBatch.Draw(sunglasses, position, null, Color.White, rotation, sunglasses.Size() * .5f, sunglasses_scale * scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Moon

    [ModCall]
    public static void DrawMoon(SpriteBatch spriteBatch, GraphicsDevice device)
    {
        Color skyColor = Main.ColorOfTheSkies.MultiplyRGB(moon_sky_color);

        Color moonShadowColor = SkyConfig.Instance.TransparentMoonShadow ? Color.Transparent : skyColor;
        Color moonColor = Info.MoonColor * Info.MoonScale;
        moonColor.A = 255;

        DrawMoon(spriteBatch, Info.MoonPosition, Info.MoonColor, Info.MoonRotation, Info.MoonScale, moonColor, moonShadowColor, device);
    }

    [ModCall]
    public static void DrawMoon(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation, float scale, Color moonColor, Color shadowColor, GraphicsDevice device)
    {
        Asset<Texture2D> moon = MoonTexture;

        device.Textures[1] = MiscTextures.Pixel;

        bool drawExtras = true;

        bool eventMoon = EventMoon;

        if (PreDrawMoon.Invoke(
            spriteBatch,
            device,
            ref moon,
            ref position,
            ref color,
            ref rotation,
            ref scale,
            ref moonColor,
            ref shadowColor,
            ref drawExtras,
            eventMoon
        ))
        {
            spriteBatch.End(out var snapshot);
            spriteBatch.Begin(snapshot with { SortMode = SpriteSortMode.Immediate });

            bool drawPlanet = true;

            // Extra details (e.g. rings/debris)
            if (drawExtras)
            {
                drawPlanet = PreDrawMoonExtras.Invoke(
                    spriteBatch,
                    device,
                    ref moon,
                    ref position,
                    ref color,
                    ref rotation,
                    ref scale,
                    ref moonColor,
                    ref shadowColor,
                    eventMoon
                );
            }

            if (drawPlanet)
            {
                Vector2 size = new(eventMoon || !drawExtras ? MoonSize : default_moon_size);

                size *= scale;

                size /= moon.Value.Size();

                ApplyPlanetShader(Main.moonPhase * moon_phase, shadowColor);
                spriteBatch.Draw(moon.Value, position, null, moonColor, rotation, moon.Value.Size() * .5f, size, SpriteEffects.None, 0f);
            }

            if (drawExtras)
            {
                PostDrawMoonExtras.Invoke(
                    spriteBatch,
                    device,
                    moon,
                    position,
                    color,
                    rotation,
                    scale,
                    moonColor,
                    shadowColor,
                    eventMoon
                );
            }

            spriteBatch.Restart(in snapshot);
        }

        PostDrawMoon.Invoke(
            spriteBatch,
            device,
            moon,
            position,
            color,
            rotation,
            scale,
            moonColor,
            shadowColor,
            drawExtras,
            eventMoon
        );

        MoonTexture = moon;
    }

    public static void ApplyPlanetShader(float shadowAngle, Color shadowColor, Color? atmosphereColor = null, Color? atmosphereShadowColor = null)
    {
        if (!SkyEffects.Planet.IsReady)
            return;

        SkyEffects.Planet.Radius = moon_radius;

        SkyEffects.Planet.ShadowRotation = -shadowAngle * MathHelper.TwoPi;

        SkyEffects.Planet.ShadowColor = shadowColor.ToVector4();
        SkyEffects.Planet.AtmosphereColor = atmosphereColor?.ToVector4() ?? moon_atmosphere;

        Vector4 atmoShadowColor = SkyConfig.Instance.TransparentMoonShadow ? 
            Color.Transparent.ToVector4() : 
            atmosphereShadowColor?.ToVector4() ?? moon_atmosphere_shadow;
        SkyEffects.Planet.AtmosphereShadowColor = atmoShadowColor;

        SkyEffects.Planet.Apply();
    }

    [PreDrawMoonExtras]
    private static bool PreDrawMoonExtras_Moon2(
        SpriteBatch spriteBatch,
        GraphicsDevice device,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        bool eventMoon)
    {
        if (Main.moonType != 2 || eventMoon)
        {
            return true;
        }

        Texture2D rings = SkyTextures.Rings.Value;

        DrawMoon2Rings(spriteBatch, rings, position, rings.Frame(1, 2, 0, 0), rotation, rings.Size() * .5f, scale, moonColor, shadowColor);

        return true;
    }

    [PostDrawMoonExtras]
    private static void PostDrawExtras_Moon2(
        SpriteBatch spriteBatch,
        Asset<Texture2D> moon,
        Vector2 position,
        Color color,
        float rotation,
        float scale,
        Color moonColor,
        Color shadowColor,
        bool eventMoon,
        GraphicsDevice device)
    {
        if (Main.moonType != 2 || eventMoon)
        {
            return;
        }

        Texture2D rings = SkyTextures.Rings.Value;

        DrawMoon2Rings(spriteBatch, rings, position, rings.Frame(1, 2, 0, 1), rotation, new(rings.Width * .5f, 0f), scale, moonColor, shadowColor);
    }

    private static void DrawMoon2Rings(
        SpriteBatch spriteBatch,
        Texture2D texture,
        Vector2 position,
        Rectangle frame,
        float rotation,
        Vector2 origin,
        float scale,
        Color moonColor,
        Color shadowColor)
    {
        Vector2 ringSize = new(.28f, .07f);

        const float ring_rotation = 0.13f;
        const float shadow_exponent = 15f;
        const float shadow_size = 4.6f;

        if (!SkyEffects.Rings.IsReady)
        {
            return;
        }

        SkyEffects.Rings.Angle = Main.moonPhase * moon_phase * MathHelper.TwoPi;

        SkyEffects.Rings.ShadowColor = shadowColor.ToVector4();
        SkyEffects.Rings.ShadowExponent = shadow_exponent;
        SkyEffects.Rings.ShadowSize = shadow_size;

        SkyEffects.Rings.Apply();

        rotation -= ring_rotation;

        spriteBatch.Draw(texture, position, frame, moonColor, rotation, origin, ringSize * scale, SpriteEffects.None, 0f);
    }

    [PreDrawMoonExtras]
    private static bool PreDrawMoonExtras_Moon8(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        bool eventMoon,
        GraphicsDevice device)
    {
        if (Main.moonType != 8 || eventMoon)
        {
            return true;
        }
        const float moon8_scale = .74f;

        Vector2 extraUpperPosition = new(-22, -19);
        Vector2 extraLowerPosition = new(25);

        const float extra_upper_scale = .22f;
        const float extra_lower_scale = .41f;

        ApplyPlanetShader(Main.moonPhase * moon_phase, shadowColor);

        Texture2D texture = moon.Value;

        Vector2 upperMoonOffset = extraUpperPosition.RotatedBy(rotation) * scale;
        Vector2 lowerMoonOffset = extraLowerPosition.RotatedBy(rotation) * scale;

        Vector2 origin = moon.Size() * .5f;
        Vector2 size = new Vector2(MoonSize * scale) / texture.Size();

        spriteBatch.Draw(texture, position + upperMoonOffset, null, moonColor, rotation, origin, size * extra_upper_scale, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, position + lowerMoonOffset, null, moonColor, rotation, origin, size * extra_lower_scale, SpriteEffects.None, 0f);

        scale *= moon8_scale;

        return true;
    }

    [PreDrawMoon]
    private static bool PreDrawMoon_DrunkWorldMoon(
        SpriteBatch spriteBatch,
        ref Asset<Texture2D> moon,
        ref Vector2 position,
        ref Color color,
        ref float rotation,
        ref float scale,
        ref Color moonColor,
        ref Color shadowColor,
        ref bool drawExtras,
        bool eventMoon,
        GraphicsDevice device)
    {
        // Drunk world moon overrides event moons
        if (!WorldGen.drunkWorldGen)
        {
            return true;
        }

        Vector2 leftEyePosition = new(-24, -32);
        Vector2 rightEyePosition = new(13, -44);

        const float phase = 0.3125f;

        Texture2D texture = SkyTextures.Moon[0];

        Texture2D star = StarTextures.FourPointedStar.Value;

        Vector2 starLeftOffset = leftEyePosition.RotatedBy(rotation) * scale;
        Vector2 starRightOffset = rightEyePosition.RotatedBy(rotation) * scale;

        spriteBatch.Draw(star, position + starLeftOffset, null, (moonColor * .4f) with { A = 0 }, 0, star.Size() * .5f, scale * .33f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, (moonColor * .4f) with { A = 0 }, 0, star.Size() * .5f, scale * .33f, SpriteEffects.None, 0f);

        spriteBatch.Draw(star, position + starLeftOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * .5f, scale * .2f, SpriteEffects.None, 0f);
        spriteBatch.Draw(star, position + starRightOffset, null, color with { A = 0 }, MathHelper.PiOver4, star.Size() * .5f, scale * .2f, SpriteEffects.None, 0f);

        ApplyPlanetShader(phase, shadowColor);

        Vector2 size = new Vector2(MoonSize * scale) / texture.Size();
        spriteBatch.Draw(texture, position, null, moonColor, rotation - MathHelper.PiOver2, texture.Size() * .5f, size, SpriteEffects.None, 0f);

        drawExtras = false;

        return false;
    }

    #endregion

    private static void DrawSunAndMoonToSky(On_Main.orig_DrawSunAndMoon orig, Main self, Main.SceneArea sceneArea, Color moonColor, Color sunColor, float tempMushroomInfluence)
    {
        if (!ModImpl.CanDrawSky)
        {
            orig(
                self,
                sceneArea,
                moonColor,
                sunColor,
                tempMushroomInfluence
            );

            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        MoonTexture = GetMoonTexture();

        GraphicsDevice device = Main.instance.GraphicsDevice;

        spriteBatch.End(out var snapshot);

        if (PreDrawSunAndMoon.Invoke(spriteBatch, in snapshot))
        {
            if (SkyConfig.Instance.UseSunAndMoon)
            {
                spriteBatch.Begin(snapshot with { SortMode = SpriteSortMode.Deferred, BlendState = BlendState.AlphaBlend, SamplerState = SamplerState.PointWrap });
                {
                    DrawSunAndMoon(
                        spriteBatch,
                        device,
                        Main.dayTime && ShowSun,
                        !Main.dayTime && ShowMoon
                    );
                }
                spriteBatch.End();
            }

            spriteBatch.Begin(in snapshot);
            {
                orig(
                    self,
                    sceneArea,
                    moonColor,
                    sunColor,
                    tempMushroomInfluence
                );
            }
            spriteBatch.End();
        }

        PostDrawSunAndMoon.Invoke(spriteBatch, in snapshot);

        spriteBatch.Begin(in snapshot);
    }

    private static Asset<Texture2D> GetMoonTexture()
    {
        Asset<Texture2D> ret = SkyTextures.Moon[0];

        if (MoonStyles.TryGetValue(Main.moonType, out Asset<Texture2D>? style))
        {
            ret = style;
        }

        if (Main.pumpkinMoon)
        {
            ret = SkyTextures.MoonPumpkin;
        }
        else if (Main.snowMoon)
        {
            ret = SkyTextures.MoonSnow;
        }

        bool eventMoon = EventMoon;

        ModifyMoonTexture.Invoke(ref ret, eventMoon);

        return ret;
    }
}
