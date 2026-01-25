using Daybreak.Common.Features.Hooks;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using System.Diagnostics;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Animations;
using Terraria.GameContent.Events;
using Terraria.GameContent.Skies;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems;

[Autoload(Side = ModSide.Client)]
internal static class MenuEdits
{
    [OnLoad]
    private static void Load()
    {
        // Screen capturing
        IL_Main.DoDraw += DoDraw_CaptureOnMainMenu;

        IL_Main.ClearVisualPostProcessEffects += ClearVisualPostProcessEffects_ShowEffects;

        // Credits
        On_CreditsRollSky.Draw += Draw_HideCredits;

        MonoModHooks.Modify(
            typeof(MenuLoader).GetMethod(nameof(MenuLoader.UpdateAndDrawModMenuInner), BindingFlags.NonPublic | BindingFlags.Static),
            UpdateAndDrawModMenuInner_Credits
        );

        // Moon textures
        MonoModHooks.Modify(
            typeof(ModMenu).GetProperty(nameof(ModMenu.MoonTexture), BindingFlags.Public | BindingFlags.Instance)?.GetGetMethod(),
            get_MoonTexture_Uncap
        );

        // Re-JIT vanilla methods that use ModMenu.MoonTexture
        IL_Main.DrawSunAndMoon += _ => { };

        // Moon phases 1-7 => 1-8
        IL_Main.UpdateMenu += UpdateMenu_MoonPhases;
    }

    #region Screen capturing

    private static void DoDraw_CaptureOnMainMenu(ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel? jumpEndCaptureTarget = null;

        int menuCaptureFlagIndex = -1; // loc
        int shouldCaptureIndex = -1; // loc

        // Allow capturing to start; will only capture if there are active filters.
        c.GotoNext(
            MoveType.After,
            i => i.MatchCallvirt<EffectManager<Filter>>("get_Item"),
            i => i.MatchCallvirt<Filter>(nameof(Filter.IsInUse)),
            i => i.MatchBrfalse(out _)
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdcI4(0),
            i => i.MatchStloc(out menuCaptureFlagIndex)
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.drawToScreen))
        );

        c.MoveAfterLabels();

        c.EmitDelegate(() => ModImpl.Unloading);

        c.EmitStloc(menuCaptureFlagIndex);

        c.GotoNext(
            MoveType.After,
            i => i.MatchBr(out _),
            i => i.MatchLdcI4(0),
            i => i.MatchStloc(out shouldCaptureIndex),
            i => i.MatchLdloc(shouldCaptureIndex)
        );

        // Move EndCapture to before UI drawing is done.
        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out _),
            i => i.MatchLdloca(out _),
            i => i.MatchLdloca(out _),
            i => i.MatchCall<Main>(nameof(Main.PreDrawMenu))
        );

        c.MoveAfterLabels();

        c.EmitLdloc(shouldCaptureIndex);

        c.EmitDelegate(
            (bool capture) =>
            {
                if (capture)
                {
                    Filters.Scene.EndCapture(null, Main.screenTarget, Main.screenTargetSwap, Color.Black);
                }
            }
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(shouldCaptureIndex),
            i => i.MatchBrfalse(out jumpEndCaptureTarget)
        );

        Debug.Assert(jumpEndCaptureTarget is not null);

        c.EmitBr(jumpEndCaptureTarget);
    }

    private static void ClearVisualPostProcessEffects_ShowEffects(ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel jumpResettingTarget = c.DefineLabel();

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall<CreditsRollEvent>(nameof(CreditsRollEvent.Reset))
        );

        c.EmitBr(jumpResettingTarget);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
            i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.DeactivateAll))
        );

        c.MarkLabel(jumpResettingTarget);
    }

    #endregion

    #region Credits

    private static void Draw_HideCredits(On_CreditsRollSky.orig_Draw orig, CreditsRollSky self, SpriteBatch spriteBatch, float minDepth, float maxDepth)
    {
        if (Main.gameMenu)
        {
            return;
        }

        orig(self, spriteBatch, minDepth, maxDepth);
    }

    private static void UpdateAndDrawModMenuInner_Credits(ILContext il)
    {
        var c = new ILCursor(il);

        int spriteBatchIndex = -1; // arg

        c.GotoNext(
            i => i.MatchCallvirt<ModMenu>(nameof(ModMenu.PreDrawLogo))
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld(typeof(MenuLoader), nameof(MenuLoader.currentMenu)),
            i => i.MatchLdarg(out spriteBatchIndex)
        );

        c.MoveAfterLabels();

        c.EmitLdarg(spriteBatchIndex);

        c.EmitDelegate(DrawCredits);
    }

    private static void DrawCredits(SpriteBatch spriteBatch)
    {
        CreditsRollSky creditsRoll = (CreditsRollSky)SkyManager.Instance["CreditsRoll"];

        if (!creditsRoll.IsActive() ||
            !creditsRoll.IsLoaded)
        {
            return;
        }

        using (spriteBatch.Scope())
        {
            Matrix transform = Main.CurrentFrameFlags.Hacks.CurrentBackgroundMatrixForCreditsRoll;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                transform
            );

            Vector2 anchorPositionOnScreen = new(Utilities.HalfScreenSize.X, 300);

            var info = new GameAnimationSegment()
            {
                SpriteBatch = spriteBatch,
                AnchorPositionOnScreen = anchorPositionOnScreen,
                TimeInAnimation = creditsRoll._currentTime,
                DisplayOpacity = creditsRoll._opacity
            };

            var segments = creditsRoll._segmentsInMainMenu;

            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].Draw(ref info);
            }

            spriteBatch.End();
        }
    }

    #endregion

    #region Moon texture

    private static void get_MoonTexture_Uncap(ILContext il)
    {
        ILCursor c = new(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdcI4(out _),
            i => i.MatchLdcI4(out _)
        );

        c.EmitPop();

        c.EmitDelegate(() => TextureAssets.Moon.Length - 1);
    }

    #endregion

    #region Moon phases

    private static void UpdateMenu_MoonPhases(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.moonPhase)),
            i => i.MatchLdcI4(1),
            i => i.MatchAdd(),
            i => i.MatchStsfld<Main>(nameof(Main.moonPhase))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.moonPhase)),
            i => i.MatchLdcI4(7)
        );

        c.EmitPop();

        c.EmitLdcI4(8);
    }

    #endregion
}
