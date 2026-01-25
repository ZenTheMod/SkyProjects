using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ZenSkies.Common.Systems.Compat;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
internal static class SunAndMoonFlinging
{
    private static readonly Vector2 velocity_multiplier = new(.92f, .85f);
    private const float mod_multiplier = .976f;

    private static Vector2 sunMoonOldPosition;

    private static Vector2 sunMoonVelocity;

    [SunAndMoon.PostDrawSunAndMoon]
    private static void PostDrawSunAndMoon_Fling(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot)
    {
        if (!Main.gameMenu ||
            Main.netMode == NetmodeID.MultiplayerClient)
        {
            return;
        }

        Vector2 position = Main.dayTime ? SunAndMoon.Info.SunPosition : SunAndMoon.Info.MoonPosition;

        float sunMoonWidth =
            Main.dayTime ?
            TextureAssets.Sun.Value.Width :
            TextureAssets.Moon[Main.moonType].Value.Width;

        double timeLength =
            Main.dayTime ?
            Main.dayLength :
            Main.nightLength;

        if (Main.alreadyGrabbingSunOrMoon)
        {
            sunMoonVelocity = position - sunMoonOldPosition;

            sunMoonOldPosition = position;

            return;
        }

        sunMoonVelocity *= velocity_multiplier;

        if (Main.dayTime)
        {
            Main.sunModY += (short)sunMoonVelocity.Y;
        }
        else
        {
            Main.moonModY += (short)sunMoonVelocity.Y;
        }

        Main.sunModY = (short)(Main.sunModY * mod_multiplier);
        Main.moonModY = (short)(Main.moonModY * mod_multiplier);

        double newTime =
            RedSunSystem.FlipSunAndMoon ?
            RedSunFlinging(position, sunMoonWidth) :
            (position.X + sunMoonVelocity.X + sunMoonWidth);

        newTime /= Utilities.ScreenSize.X + (sunMoonWidth * 2f);

        newTime *= timeLength;

        Main.time = newTime;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double RedSunFlinging(Vector2 position, float sunMoonWidth)
    {
        float moonAdjust = Main.dayTime ? 0 : RedSunSystem.MoonAdjustment.X;

        return Utilities.ScreenSize.X - (position.X + sunMoonVelocity.X) + moonAdjust + sunMoonWidth;
    }
}
