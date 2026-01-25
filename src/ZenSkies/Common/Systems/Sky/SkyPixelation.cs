using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Core;
using Daybreak.Common.Features.Hooks;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
internal static class SkyPixelation
{
    private static RenderTargetLease? rtLease;
    private static RenderTargetScope? rtScope;

    [OnLoad]
    private static void Load()
    {
        IL_Main.DoDraw += DoDraw_CaptureSky;
        IL_Main.DrawSurfaceBG += DrawSurfaceBG_CaptureSky;

        IL_Main.DrawCapture += DrawCapture_CaptureSky;
    }

    #region DoDraw

    private static void DoDraw_CaptureSky(ILContext il)
    {
        var c = new ILCursor(il);

        // Swap to a seperate target
        c.GotoNext(
            MoveType.After,
            i => i.MatchCall(typeof(TimeLogger), nameof(TimeLogger.DetailedDrawTime)),
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
        );

        c.EmitDelegate(StartCapture);

        // Change the sampler state used for the background
        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchLdcI4(0),
            i => i.MatchLdcI4(0),
            i => i.MatchCallvirt<OverlayManager>(nameof(OverlayManager.Draw))
        );

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<SamplerState>(nameof(SamplerState.LinearClamp))
        );

        c.EmitPop();

        c.EmitDelegate(() => SamplerState.PointClamp);

        // Incase our scopes don't get disposed in DrawBG
        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg(out _),
            i => i.MatchCall<Main>(nameof(Main.DrawBG))
        );

        c.EmitDelegate(EndCapture);
    }

    #endregion

    #region DrawSurfaceBG

    private static void DrawSurfaceBG_CaptureSky(ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel jumpDepthResetTarget = c.DefineLabel();

        // Draw the first CustomSky layer before the first clouds
        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
            i => i.MatchMul(),
            i => i.MatchStloc(out _)
        );

        c.EmitDelegate(() =>
        {
            SkyManager.Instance.ResetDepthTracker();

            SkyManager.Instance.DrawToDepth(Main.spriteBatch, float.MaxValue * .5f);

            EndCapture();
        });

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
            i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.ResetDepthTracker))
        );

        c.MoveAfterLabels();

        c.EmitBr(jumpDepthResetTarget);

        c.Index--;

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<SkyManager>(nameof(SkyManager.Instance)),
            i => i.MatchCallvirt<SkyManager>(nameof(SkyManager.ResetDepthTracker))
        );

        c.MarkLabel(jumpDepthResetTarget);
    }

    #endregion

    #region DrawCapture

    private static void DrawCapture_CaptureSky(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall<Main>(nameof(Main.DrawSimpleSurfaceBackground)),
            i => i.MatchLdsfld<Main>(nameof(Main.tileBatch)),
            i => i.MatchCallvirt<TileBatch>(nameof(TileBatch.End))
        );

        c.EmitDelegate(StartCapture);

        c.GotoNext(
            MoveType.After,
            i => i.MatchCall<Main>(nameof(Main.DrawSurfaceBG)),
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchCallvirt<SpriteBatch>(nameof(SpriteBatch.End))
        );

        c.EmitDelegate(EndCapture);
    }

    #endregion

    private static void StartCapture()
    {
        if (!ModImpl.CanDrawSky ||
            !SkyConfig.Instance.UsePixelatedSky)
        {
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        using (spriteBatch.Scope())
        {
            GraphicsDevice device = Main.instance.GraphicsDevice;

            rtLease = ScreenspaceTargetPool.Shared.Rent(device);

            rtScope = rtLease.Scope(clearColor: Color.Transparent);
        }
    }

    private static void EndCapture()
    {
        if (!ModImpl.CanDrawSky ||
            !SkyConfig.Instance.UsePixelatedSky ||
            Main.mapFullscreen ||
            rtLease is null ||
            rtScope is null)
        {
            return;
        }

        SpriteBatch spriteBatch = Main.spriteBatch;

        GraphicsDevice device = Main.instance.GraphicsDevice;

        using (spriteBatch.Scope())
        {
            rtScope?.Dispose();
            rtScope = null;

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise,
                null,
                Matrix.Identity);
            {
                Viewport viewport = device.Viewport;

                Vector2 screenSize = new(viewport.Width, viewport.Height);

                SkyEffects.PixelateAndQuantize.ScreenSize = screenSize;
                SkyEffects.PixelateAndQuantize.PixelSize = new(2);

                SkyEffects.PixelateAndQuantize.Steps = SkyConfig.Instance.ColorSteps;

                int pass = (SkyConfig.Instance.ColorSteps == 255).ToInt();

                SkyEffects.PixelateAndQuantize.Apply(pass);

                spriteBatch.Draw(rtLease.Target, Vector2.Zero, Color.White);
            }
            spriteBatch.End();

            rtLease.Dispose();
            rtLease = null;
        }
    }
}
