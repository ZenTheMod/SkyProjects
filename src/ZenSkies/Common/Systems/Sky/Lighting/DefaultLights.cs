using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using ZenSkies.Common.Config;
using ZenSkies.Common.Systems.Compat;
using ZenSkies.Core;

namespace ZenSkies.Common.Systems.Sky;

public sealed class SunLight : SkyLight
{
    public override bool Active => SunAndMoon.ShowSun && Main.dayTime;

    protected override Color Color
    {
        get
        {
            const float sun_noon_alpha = 0.082f;

            const float fade_start = 1f;
            const float fade_end = 1.11f;

            Color sunMultiplier = new(255, 245, 225);

            Vector2 position = SunAndMoon.Info.SunPosition;
            float centerX = Utilities.HalfScreenSize.X;

            float dist = MathF.Abs(centerX - position.X) / centerX;

            Color color = SunAndMoon.Info.SunColor.MultiplyRGB(sunMultiplier);

            // Fade in/out
            color *= Utils.Remap(dist, fade_start, fade_end, 1f, 0f);

            // Decrease intensity at noon
            color *= MathHelper.Lerp(sun_noon_alpha, 1f, Easings.InQuart(dist));

            color.A = byte.MaxValue;

            return color;
        }
    }

    protected override Vector2 Position
    {
        get
        {
            Vector2 viewportSize = Main.instance.GraphicsDevice.Viewport.Bounds.Size();

            Vector2 position = SunAndMoon.Info.SunPosition;

            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            {
                position.Y = viewportSize.Y - position.Y;
            }

            return position;
        }
    }

    protected override float Size
    {
        get
        {
            const float size = 4f;

            return SunAndMoon.Info.SunScale * size;
        }
    }
}

public sealed class MoonLight : SkyLight
{
    public override bool Active => SunAndMoon.ShowMoon && (RedSunSystem.IsEnabled || !Main.dayTime);

    protected override Color Color
    {
        get
        {
            const float fade_start = 1f;
            const float fade_end = 1.11f;

            Color moonMultiplier = new(50, 50, 55);

            Vector2 position = SunAndMoon.Info.MoonPosition;
            float centerX = Utilities.HalfScreenSize.X;

            float dist = MathF.Abs(centerX - position.X) / centerX;

            Color color = SunAndMoon.Info.MoonColor.MultiplyRGB(moonMultiplier);

            // Fade in/out
            color *= Utils.Remap(dist, fade_start, fade_end, 1f, 0f);

            // Decrease intensity based on moon phase
            color *= MathF.Abs(4 - Main.moonPhase) * 0.25f;

            color.A = byte.MaxValue;

            return color;
        }
    }

    protected override Vector2 Position
    {
        get
        {
            Vector2 viewportSize = Main.instance.GraphicsDevice.Viewport.Bounds.Size();

            Vector2 position = SunAndMoon.Info.MoonPosition;

            if (Main.BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically))
            {
                position.Y = viewportSize.Y - position.Y;
            }

            return position;
        }
    }

    protected override float Size
    {
        get
        {
            const float size = 1.2f;

            return SunAndMoon.Info.SunScale * size;
        }
    }

    protected override Texture2D? Texture => SkyConfig.Instance.UseSunAndMoon ? SunAndMoon.MoonTexture.Value : null;
}