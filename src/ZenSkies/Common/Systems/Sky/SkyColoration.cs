using Daybreak.Common.Features.Hooks;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Compat;
using ZenSkies.Common.Systems.Sky.Space;
using ZenSkies.Core;
using hook_ModifySunLightColor = Terraria.ModLoader.SystemLoader.DelegateModifySunLightColor;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
public static class SkyColoration
{
    // Used for running certain ModSystem.ModifySunLightColor hooks on the menu
    public static event hook_ModifySunLightColor? ModifyInMenu;

    [OnLoad]
    private static void Load()
    {
        ModifyInMenu += ModifySunLightColor;

        // Sky lighting in menu
        MonoModHooks.Add(
            typeof(SystemLoader).GetMethod(nameof(SystemLoader.ModifySunLightColor)),
            ModifySunLightColor_MainMenu
        );

        // Tile lighting fixes to support pitch black nights
        IL_Main.DrawBlack += DrawBlack_NonSolid;

        IL_TileDrawing.DrawSingleTile += DrawSingleTile_NonSolid;

        IL_WallDrawing.DrawWalls += DrawWalls_NonSolid;
    }

    [ModSystemHooks.ModifySunLightColor]
    private static void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (!ModImpl.CanDrawSky ||
            !SkyConfig.Instance.PitchBlackBackground ||
            DarkSurfaceCompat.IsEnabled)
        {
            return;
        }

        // TODO: Use a different value not based on stars
        float interpolator = Easings.InCubic(StarSystem.StarAlpha);

        backgroundColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
        tileColor = Color.Lerp(Main.ColorOfTheSkies, Color.Black, interpolator);
    }

    private static void ModifySunLightColor_MainMenu(hook_ModifySunLightColor orig, ref Color tileColor, ref Color backgroundColor)
    {
        orig(ref tileColor, ref backgroundColor);

        if (Main.gameMenu)
        {
            ModifyInMenu?.Invoke(ref tileColor, ref backgroundColor);
        }
    }

    private static void DrawBlack_NonSolid(ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel? breakTarget = c.DefineLabel();

        int tileXIndex = -1; // loc
        int tileYIndex = -1; // loc

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloc(out _),
            i => i.MatchBrtrue(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchBrfalse(out breakTarget),
            i => i.MatchLdsfld<Main>(nameof(Main.drawToScreen))
        );

        c.GotoPrev(
            MoveType.After,
            i => i.MatchLdsflda<Main>(nameof(Main.tile)),
            i => i.MatchLdloc(out tileXIndex),
            i => i.MatchLdloc(out tileYIndex),
            i => i.MatchCall<Tilemap>("get_Item"),
            i => i.MatchStloc(out _)
        );

        c.EmitLdloc(tileXIndex);
        c.EmitLdloc(tileYIndex);

        c.EmitDelegate(Utilities.IgnoresDrawBlack);

        c.EmitBrtrue(breakTarget);
    }

    private static void DrawSingleTile_NonSolid(ILContext il)
    {
        var c = new ILCursor(il);

        int tileXIndex = -1; // arg
        int tileYIndex = -1; // arg

        c.GotoNext(
            i => i.MatchLdsflda<Main>(nameof(Main.tile)),
            i => i.MatchLdarg(out tileXIndex),
            i => i.MatchLdarg(out tileYIndex),
            i => i.MatchCall<Tilemap>("get_Item")
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg(out _),
            i => i.MatchLdflda<TileDrawInfo>(nameof(TileDrawInfo.tileLight)),
            i => i.MatchCall<Color>($"get_{nameof(Color.R)}"),
            i => i.MatchLdcI4(1),
            i => i.MatchBge(out _)
        );

        c.GotoPrev(
            MoveType.Before,
            i => i.MatchStloc(out _)
        );

        c.EmitPop();

        c.EmitLdarg(tileXIndex);
        c.EmitLdarg(tileYIndex);

        c.EmitDelegate(Utilities.IgnoresDrawBlack);
    }

    private static void DrawWalls_NonSolid(ILContext il)
    {
        var c = new ILCursor(il);

        ILLabel? jumpColorCheckTarget = c.DefineLabel();

        int tileXIndex = -1; // loc
        int tileYIndex = -1; // loc

        int colorIndex = -1; // loc

        c.GotoNext(
            i => i.MatchLdloc(out tileXIndex),
            i => i.MatchLdloc(out tileYIndex),
            i => i.MatchCall<Terraria.Lighting>(nameof(Terraria.Lighting.GetColor)),
            i => i.MatchStloc(out colorIndex)
        );

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdloca(colorIndex),
            i => i.MatchCall<Color>($"get_{nameof(Color.R)}"),
            i => i.MatchBrtrue(out jumpColorCheckTarget)
        );

        c.MoveAfterLabels();

        c.EmitLdloc(tileXIndex);
        c.EmitLdloc(tileYIndex);

        c.EmitDelegate(Utilities.IgnoresDrawBlack);

        c.EmitBrtrue(jumpColorCheckTarget);
    }
}
