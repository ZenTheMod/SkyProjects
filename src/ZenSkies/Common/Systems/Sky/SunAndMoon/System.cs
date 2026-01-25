using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

public readonly record struct SunAndMoonInfo(
    Vector2 SunPosition, Color SunColor, float SunRotation, float SunScale,
    Vector2 MoonPosition, Color MoonColor, float MoonRotation, float MoonScale
);

[Autoload(Side = ModSide.Client)]
public static partial class SunAndMoon
{
    public static bool ShowSun
    {
        [ModCall(true, nameof(ShowSun), $"Get{nameof(ShowSun)}")]
        get;
        [ModCall(true, $"Set{nameof(ShowSun)}")]
        set; 
    } = true;

    public static bool ShowMoon
    {
        [ModCall(true, nameof(ShowMoon), $"Get{nameof(ShowMoon)}n")]
        get;
        [ModCall(true, $"Set{nameof(ShowMoon)}")]
        set;
    } = true;

    public static Dictionary<int, Asset<Texture2D>> MoonStyles { get; private set; } = [];

    public static SunAndMoonInfo Info
    {
        get;

        set
        {
            field = value;

            UpdateSunAndMoonInfo.Invoke(value);
        }
    }

    public static bool EventMoon
    {
        get
        {
            bool vanilla = WorldGen.drunkWorldGen || Main.pumpkinMoon || Main.snowMoon;

            return vanilla || EventMoonActive.Invoke();
        }
    }

    public static Asset<Texture2D> MoonTexture { get; private set; } = SkyTextures.Moon[0];

    [OnLoad]
    private static void System_Load()
    {
        IL_Main.DrawSunAndMoon += DrawSunAndMoon_Patch;

        for (int i = 0; i < SkyTextures.Moon.Length; i++)
        {
            MoonStyles.Add(i, SkyTextures.Moon[i]);
        }
    }

    private static void DrawSunAndMoon_Patch(ILContext il)
    {
        const int y_offset = -80;

        const float min_sun_brightness = 0.82f;
        const float min_moon_brightness = 0.65f;

        var c = new ILCursor(il);

        // Replace dynamic vanilla offset with a static value
        c.GotoNext(
            MoveType.After,
            i => i.MatchLdarg1(),
            i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.bgTopY))
        );

        c.EmitPop();
        c.EmitLdcI4(y_offset);

        // ---- Sun ----

        ILLabel skipSunDrawingTarget = c.DefineLabel();

        int sunAlphaIndex = -1; // loc

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
            i => i.MatchMul(),
            i => i.MatchSub(),
            i => i.MatchStloc(out sunAlphaIndex)
        );

        c.EmitLdloca(sunAlphaIndex);

        c.EmitDelegate(
            static (ref float mult) =>
            {
                mult = MathF.Max(mult, min_sun_brightness);
            }
        );

        int sunPositionIndex = -1; // loc
        int sunColorIndex = -1; // loc
        int sunRotationIndex = -1; // loc
        int sunScaleIndex = -1; // loc

        // If UseSunAndMoon then store sunPosition and skip both adding SceneLocalScreenPositionOffset and vanilla drawing
        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg1(),
            i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
            i => i.MatchCall<Vector2>("op_Addition"),
            i => i.MatchStloc(out sunPositionIndex)
        );

        c.EmitStloc(sunPositionIndex);

        c.EmitDelegate(static () => SkyConfig.Instance.UseSunAndMoon);
        c.EmitBrtrue(skipSunDrawingTarget);

        c.EmitLdloc(sunPositionIndex);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
            i => i.MatchBrtrue(out _)
        );

        c.MarkLabel(skipSunDrawingTarget);

        // Fetch various indicies
        c.FindNext(
            out _,
            i => i.MatchLdsfld<Main>(nameof(Main.spriteBatch)),
            i => i.MatchLdloc0(),
            i => i.MatchLdloc(sunPositionIndex),
            i => i.MatchLdloca(out _),
            i => i.MatchInitobj<Rectangle?>(),
            i => i.MatchLdloc(out _),
            i => i.MatchLdloc(out sunColorIndex),
            i => i.MatchLdloc(out sunRotationIndex),
            i => i.MatchLdloc(out _),
            i => i.MatchLdloc(out sunScaleIndex),
            i => i.MatchLdcI4(0)
        );

        // ---- Moon ----

        ILLabel skipMoonDrawingTarget = c.DefineLabel();

        int moonAlphaIndex = -1; // loc

        c.GotoNext(
            MoveType.After,
            i => i.MatchLdsfld<Main>(nameof(Main.atmo)),
            i => i.MatchMul(),
            i => i.MatchSub(),
            i => i.MatchStloc(out moonAlphaIndex)
        );

        c.EmitLdloca(moonAlphaIndex);

        c.EmitDelegate(
            (ref float mult) =>
            {
                mult = MathF.Max(mult, min_moon_brightness);
            }
        );

        int moonPositionIndex = -1; // loc
        int moonColorIndex = -1; // arg
        int moonRotationIndex = -1; // loc
        int moonScaleIndex = -1; // loc

        // Ditto
        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdarg1(),
            i => i.MatchLdfld<Main.SceneArea>(nameof(Main.SceneArea.SceneLocalScreenPositionOffset)),
            i => i.MatchCall<Vector2>("op_Addition"),
            i => i.MatchStloc(out moonPositionIndex)
        );

        c.EmitStloc(moonPositionIndex);

        c.EmitDelegate(static () => SkyConfig.Instance.UseSunAndMoon);
        c.EmitBrtrue(skipMoonDrawingTarget);

        c.EmitLdloc(moonPositionIndex);

        c.GotoNext(
            MoveType.Before,
            i => i.MatchLdsfld<Main>(nameof(Main.dayTime)),
            i => i.MatchBrfalse(out _)
        );

        c.MarkLabel(skipMoonDrawingTarget);

        c.FindNext(
            out _,
            i => i.MatchNewobj<Rectangle?>(),
            i => i.MatchLdarg(out moonColorIndex),
            i => i.MatchLdloc(out moonRotationIndex),
            i => i.MatchDiv(),
            i => i.MatchConvR4(),
            i => i.MatchNewobj<Vector2>(),
            i => i.MatchLdloc(out moonScaleIndex)
        );

        // Move past skipMoonDrawingTarget
        c.MoveAfterLabels();

        c.EmitLdloc(sunPositionIndex);
        c.EmitLdloc(sunColorIndex);
        c.EmitLdloc(sunRotationIndex);
        c.EmitLdloc(sunScaleIndex);

        c.EmitLdloc(moonPositionIndex);
        c.EmitLdarg(moonColorIndex);
        c.EmitLdloc(moonRotationIndex);
        c.EmitLdloc(moonScaleIndex);

        c.EmitDelegate(
            static (
                Vector2 sunPosition, Color sunColor, float sunRotation, float sunScale,
                Vector2 moonPosition, Color moonColor, float moonRotation, float moonScale
            ) =>
            {
                Info = new(
                    sunPosition, sunColor, sunRotation, sunScale,
                    moonPosition, moonColor, moonRotation, moonScale
                );
            }
        );
    }
}
