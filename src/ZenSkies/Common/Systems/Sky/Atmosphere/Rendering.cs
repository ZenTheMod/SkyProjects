using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ZenSkies.Common.Config;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

[Autoload(Side = ModSide.Client)]
public static partial class Atmosphere
{
    [SunAndMoon.PostDrawSunAndMoon]
    private static void AtmospherePostDraw(SpriteBatch spriteBatch, in SpriteBatchSnapshot snapshot)
    {
        Texture2D gradient = SkyTextures.SkyGradient;

        Color color =
            SkyConfig.Instance.SkyGradient.GetColor(Main.TimeRatio) *
            Easings.InCubic(Main.atmo);

        color.A = 0;

        spriteBatch.Begin(in snapshot);
        {
            spriteBatch.Draw(gradient, Utilities.ScreenDimensions, color);
        }
        spriteBatch.End();
    }
}
