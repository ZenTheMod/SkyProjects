using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace ZenSkies.Common.Systems.Sky;

public record struct SkyLightInfo(Color Color, Vector2 Position, float Size, Texture2D? Texture = null);

public static partial class Lighting
{
    private static readonly List<SkyLight> lights = [];

    internal static void Register(SkyLight light)
    {
        lights.Add(light);
    }

    public static void AddOnGetInfo<T>(SkyLight.hook_OnGetInfo evt) where T : SkyLight
    {
        ModContent.GetInstance<T>().OnGetInfo += evt;
    }

    public static void ForLights(Action<SkyLightInfo> action)
    {
        foreach (SkyLight light in lights)
        {
            SkyLightInfo info = light.GetInfo();

            Color color = info.Color;

            if (light.Active && color is { R: > 0, G: > 0, B: > 0 })
            {
                action(light.GetInfo());
            }
        }
    }
}

public abstract class SkyLight : ModType
{
    public delegate void hook_OnGetInfo(ref SkyLightInfo info);

    public event hook_OnGetInfo? OnGetInfo;

    public virtual bool Active => true;

    protected abstract Color Color { get; }

    protected abstract Vector2 Position { get; }

    protected abstract float Size { get; }

    protected virtual Texture2D? Texture => null;

    protected override void Register()
    {
        ModTypeLookup<SkyLight>.Register(this);
        Lighting.Register(this);
    }

    public SkyLightInfo GetInfo()
    {
        SkyLightInfo info = new(Color, Position, Size, Texture);

        OnGetInfo?.Invoke(ref info);

        return info;
    }
}
