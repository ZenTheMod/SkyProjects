using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.Utilities;
using ZenSkies.Core.Utils;

namespace ZenSkies.Common.Systems.Sky.Space;

/// <summary>
/// A simpler version of <see cref="Terraria.Star"/> that allows for multiple styles.
/// </summary>
public record struct Star
    (Vector2 Position, Color Color, float Scale, float Rotation, float TwinklePhase, int Style, bool IsActive = true)
{
    #region Private Fields

    private static readonly Color LowestTemperature = new(255, 174, 132);
    private static readonly Color LowTemperature = new(255, 242, 238);
    private static readonly Color HighTemperature = new(236, 238, 255);
    private static readonly Color HighestTemperature = new(113, 135, 255);

    private const float MinScale = 0.3f;
    private const float MaxScale = 1.25f;

    private const float MaxTwinkle = MathHelper.TwoPi;

    private const int StarStyles = 4;

    private const float LowTempThreshold = 0.4f;
    private const float HighTempThreshold = 0.6f;

    private const float TwinkleTimeMultiplier = MathHelper.TwoPi * 0.35f;

    private const float VanillaScale = 0.95f;
    private const float VanillaTwinkleMin = 0.73f;
    private const float VanillaTwinkleMax = 1.03f;

    private const float DiamondSize = 0.124f;
    private const float DiamondAlpha = 0.75f;
    private const float DiamondTwinkleMin = 0.8f;
    private const float DiamondTwinkleMax = 1.2f;

    private const float FlareSize = 0.14f;
    private const float FlareInnerSize = 0.03f;
    private const float FlareTwinkleMin = 0.85f;
    private const float FlareTwinkleMax = 1.45f;

    private const float CircleSize = 0.3f;
    private const float CircleAlpha = 0.67f;
    private const float CircleTwinkleMin = 0.85f;
    private const float CircleTwinkleMax = 1.3f;

    #endregion

    #region Public Constructors

    public Star(UnifiedRandom rand, float circularRadius)
        : this(
              rand.NextUniformVector2Circular(circularRadius),
              GenerateColor(rand.NextFloat(1)),
              rand.NextFloat(MinScale, MaxScale),
              rand.NextFloatDirection(),
              rand.NextFloat(MaxTwinkle),
              rand.Next(0, StarStyles))
    { }

    #endregion

    #region Drawing

    public readonly void DrawVanilla(SpriteBatch spriteBatch, float alpha)
    {
        Texture2D texture = TextureAssets.Star[Style].Value;
        Vector2 origin = texture.Size() * 0.5f;

        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);

        float twinkle = TwinkleScale(VanillaTwinkleMin, VanillaTwinkleMax);

        float scale = Scale * VanillaScale * twinkle;

        float rotation = (Main.GlobalTimeWrappedHourly * 0.1f * TwinklePhase) + Rotation;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawDiamond(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * DiamondAlpha;
        color.A = 0;

        float twinkle = TwinkleScale(DiamondTwinkleMin, DiamondTwinkleMax);

        float scale = twinkle * Scale * DiamondSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawFlare(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha);
        color.A = 0;

        float twinkle = TwinkleScale(FlareTwinkleMin, FlareTwinkleMax);

        float scale = twinkle * Scale * FlareSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);

        Color white = Color.White * GetAlpha(alpha);
        color.A = 0;

        scale = Scale * FlareInnerSize;

        spriteBatch.Draw(texture, position, null, white, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    public readonly void DrawCircle(SpriteBatch spriteBatch, Texture2D texture, float alpha, Vector2 origin, float rotation)
    {
        Vector2 position = Position;

        Color color = Color * GetAlpha(alpha) * CircleAlpha;
        color.A = 0;

        float twinkle = TwinkleScale(CircleTwinkleMin, CircleTwinkleMax);

        float scale = twinkle * Scale * CircleSize;

        spriteBatch.Draw(texture, position, null, color, rotation, origin, scale, SpriteEffects.None, 0f);
    }

    #endregion

    #region Private Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color GenerateColor(float temperature) =>
        temperature switch
        {
            <= LowTempThreshold => Color.Lerp(LowestTemperature, LowTemperature, Utils.Remap(temperature, 0f, LowTempThreshold, 0f, 1f)),
            <= HighTempThreshold => Color.Lerp(LowTemperature, HighTemperature, Utils.Remap(temperature, LowTempThreshold, HighTempThreshold, 0f, 1f)),
            _ => Color.Lerp(HighTemperature, HighestTemperature, Utils.Remap(temperature, HighTempThreshold, 1f, 0f, 1f))
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly float TwinkleScale(float min, float max) =>
        Utils.Remap(MathF.Sin(TwinklePhase + (Main.GlobalTimeWrappedHourly * TwinkleTimeMultiplier)), -1, 1, min, max);

    #endregion

    #region Public Methods

    public readonly float GetAlpha(float a) =>
        Utilities.Saturate(MathF.Pow(a + Scale, 3) * a);

    #endregion
}
