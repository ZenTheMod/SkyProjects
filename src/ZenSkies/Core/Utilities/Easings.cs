using Microsoft.Xna.Framework;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ZenSkies.Core;

public static class Easings
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Style : byte
    {
        Linear,

        InCubic,
        OutCubic,
        InOutCubic,

        InQuart,
        OutQuart,
        InOutQuart,

        InQuint,
        OutQuint,
        InOutQuint,

        InSine,
        OutSine,
        InOutSine,

        InExpo,
        OutExpo,
        InOutExpo,

        InCirc,
        OutCirc,
        InOutCirc,

        InElastic,
        OutElastic,
        InOutElastic,

        InBack,
        OutBack,
        InOutBack
    }

    private readonly static Dictionary<Style, Func<float, float>> easing_functions = new()
    {
        { Style.Linear, Linear },

        { Style.InCubic, InCubic },
        { Style.OutCubic, OutCubic },
        { Style.InOutCubic, InOutCubic },

        { Style.InQuart, InQuart },
        { Style.OutQuart, OutQuart },
        { Style.InOutQuart, InOutQuart },

        { Style.InQuint, InQuint },
        { Style.OutQuint, OutQuint },
        { Style.InOutQuint, InOutQuint },

        { Style.InSine, InSine },
        { Style.OutSine, OutSine },
        { Style.InOutSine, InOutSine },

        { Style.InExpo, InExpo },
        { Style.OutExpo, OutExpo },
        { Style.InOutExpo, InOutExpo },

        { Style.InCirc, InCirc },
        { Style.OutCirc, OutCirc },
        { Style.InOutCirc, InOutCirc },

        { Style.InElastic, InElastic },
        { Style.OutElastic, OutElastic },
        { Style.InOutElastic, InOutElastic },

        { Style.InBack, InBack },
        { Style.OutBack, OutBack },
        { Style.InOutBack, InOutBack }
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Ease(Style style, float t)
    {
        return easing_functions[style](t);
    }

    #region Linear

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Linear(float t) =>
        t;

    #endregion

    #region Polynomial

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InCubic(float t) =>
        InPolynomial(t, 3);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float OutCubic(float t) =>
        OutPolynomial(t, 3);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InOutCubic(float t) =>
        InOutPolynomial(t, 3);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InQuart(float t) =>
        InPolynomial(t, 4);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float OutQuart(float t) =>
        OutPolynomial(t, 4);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InOutQuart(float t) =>
        InOutPolynomial(t, 4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InQuint(float t) =>
        InPolynomial(t, 5);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float OutQuint(float t) =>
        OutPolynomial(t, 5);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InOutQuint(float t) =>
        InOutPolynomial(t, 5);

    public static float InPolynomial(float t, float e) =>
        MathF.Pow(t, e);
    public static float OutPolynomial(float t, float e) =>
        1 - InPolynomial(1 - t, e);
    public static float InOutPolynomial(float t, float e) =>
        t < 0.5 ?
            InPolynomial(t * 2, e) * 0.5f :
            1 - InPolynomial((1 - t) * 2, e) * 0.5f;

    #endregion

    #region Sine

    public static float InSine(float t) =>
        1 - MathF.Cos(t * MathHelper.PiOver2);
    public static float OutSine(float t) =>
        MathF.Sin(t * MathHelper.PiOver2);
    public static float InOutSine(float t) =>
        (MathF.Cos(t * MathF.PI) - 1) * -.5f;

    #endregion

    #region Expo

    public static float InExpo(float t) =>
        MathF.Pow(2, 10 * (t - 1));
    public static float OutExpo(float t) =>
        1 - InExpo(1 - t);
    public static float InOutExpo(float t) =>
        t < 0.5 ? 
            InExpo(t * 2) * 0.5f :
            1 - InExpo((1 - t) * 2) * 0.5f;

    #endregion

    #region Circ

    public static float InCirc(float t) =>
        -(MathF.Sqrt(1 - t * t) - 1);
    public static float OutCirc(float t) =>
        1 - InCirc(1 - t);
    public static float InOutCirc(float t) =>
        t < 0.5 ?
            InCirc(t * 2) * 0.5f :
            1 - InCirc((1 - t) * 2) * 0.5f;

    #endregion

    #region Elastic

    public static float InElastic(float t) =>
        InElastic(t, default);
    public static float OutElastic(float t) =>
        OutElastic(t, default);
    public static float InOutElastic(float t) =>
        OutElastic(t, default);

    public static float InElastic(float t, float p = 0.3f) =>
        1 - OutElastic(1 - t, p);
    public static float OutElastic(float t, float p = 0.3f) =>
        MathF.Pow(2, -10 * t) * MathF.Sin((t - p / 4) * (2 * MathF.PI) / p) + 1;
    public static float InOutElastic(float t, float p = 0.3f) =>
        t < 0.5 ?
            InElastic(t * 2, p) * 0.5f :
            1 - InElastic((1 - t) * 2, p) * 0.5f;

    #endregion

    #region Back

    public static float InBack(float t) =>
        InBack(t, default);
    public static float OutBack(float t) =>
        OutBack(t, default);
    public static float InOutBack(float t) =>
        InOutBack(t, default);

    public static float InBack(float t, float s = 1.7f) =>
        t * t * ((s + 1) * t - s);
    public static float OutBack(float t, float s = 1.7f) =>
        1 - InBack(1 - t, s);
    public static float InOutBack(float t, float s = 1.7f) =>
        t < 0.5 ?
            InBack(t * 2, s) * 0.5f :
            1 - InBack((1 - t) * 2, s) * 0.5f;

    #endregion
}
