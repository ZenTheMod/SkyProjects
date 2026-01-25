using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Compat;
using ZenSkies.Core;
using ZenSkies.Core.ModCall;
using ZenSkies.Core.Utils;
using static ZenSkies.Common.Systems.Sky.Space.StarHooks;
using static ZenSkies.Common.Systems.Sky.Space.StarSystem;

namespace ZenSkies.Common.Systems.Sky.Space;

public static class StarRendering
{
    #region Loading

    [OnLoad(Side = ModSide.Client)]
    public static void Load() => 
        On_Main.DrawStarsInBackground += DrawStarsInBackground;

    [OnUnload(Side = ModSide.Client)]
    public static void Unload() => 
        On_Main.DrawStarsInBackground -= DrawStarsInBackground;

    #endregion

    #region Drawing

    #region Stars

    [ModCall("DrawAllStars")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawStars(SpriteBatch spriteBatch, float alpha) =>
        DrawStars(spriteBatch, alpha, -StarRotation, Stars, SkyConfig.Instance.StarStyle);

    public static void DrawStars(SpriteBatch spriteBatch, float alpha, float rotation, Star[] stars, StarVisual style)
    {
        Texture2D texture;

        Vector2 origin;

        ReadOnlySpan<Star> activeStars = [.. stars.Where(s => s.IsActive)];

        switch (style)
        {
            case StarVisual.Vanilla:
                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawVanilla(spriteBatch, alpha);
                return;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * 0.5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * 0.5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * 0.5f;

                for (int i = 0; i < activeStars.Length; i++)
                    activeStars[i].DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.Random:
                for (int i = 0; i < stars.Length; i++)
                    if (stars[i].IsActive)
                        DrawStar(spriteBatch, alpha, rotation, stars[i], (StarVisual)(i % 3 + 1));
                return;
        }
    }

    public static void DrawStar(SpriteBatch spriteBatch, float alpha, float rotation, Star star, StarVisual style)
    {
        if (!star.IsActive)
            return;

        Texture2D texture;
        Vector2 origin;

        switch (style)
        {
            case StarVisual.Vanilla:
                star.DrawVanilla(spriteBatch, alpha);
                return;

            case StarVisual.Diamond:
                texture = StarTextures.DiamondStar;
                origin = texture.Size() * 0.5f;

                star.DrawDiamond(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.FourPointed:
                texture = StarTextures.FourPointedStar;
                origin = texture.Size() * 0.5f;

                star.DrawFlare(spriteBatch, texture, alpha, origin, rotation);
                return;

            case StarVisual.OuterWilds:
                texture = StarTextures.CircleStar;
                origin = texture.Size() * 0.5f;

                star.DrawCircle(spriteBatch, texture, alpha, origin, rotation);
                return;
        }
    }

    #endregion

    private static void DrawStarsInBackground(On_Main.orig_DrawStarsInBackground orig, Main self, Main.SceneArea sceneArea, bool artificial)
    {
        if (!ModImpl.CanDrawSky ||
            artificial)
        {
            orig(self, sceneArea, artificial);
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        float alpha = StarAlpha;

        DrawStarsToSky(spriteBatch, alpha);
    }

    #endregion

    #region Public Methods

    [ModCall("DrawSkyStars")]
    public static void DrawStarsToSky(SpriteBatch spriteBatch, float alpha)
    {
        SpriteBatchSnapshot snapshot = new(spriteBatch);

        Matrix transform = RotationMatrix() * snapshot.TransformMatrix;

        spriteBatch.End();

        if (InvokePreDrawStars(spriteBatch, in snapshot, ref alpha, ref transform))
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, snapshot.DepthStencilState, snapshot.RasterizerState, RealisticSkySystem.ApplyStarShader(), transform);

            if (alpha > 0)
                DrawStars(spriteBatch, alpha);

            spriteBatch.End();
        }

        InvokePostDrawStars(spriteBatch, in snapshot, alpha, transform);

        spriteBatch.Begin(in snapshot);
    }


    [ModCall(false, "StarRotationMatrix", "GetStarRotationMatrix", "StarDrawMatrix", "StarTransform")]
    public static Matrix RotationMatrix()
    {
        Matrix rotation = Matrix.CreateRotationZ(StarRotation);
        Matrix offset = Matrix.CreateTranslation(new(Utilities.HalfScreenSize, 0f));

        return rotation * offset;
    }

    #endregion
}
