using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace ZenSkies.Core;

public static partial class Utilities
{
    public static Rectangle ScreenDimensions => new(0, 0, Main.screenWidth, Main.screenHeight);

    public static Vector2 ScreenSize => new(Main.screenWidth, Main.screenHeight);

    public static Vector2 HalfScreenSize => ScreenSize * 0.5f;

    extension(Main)
    {
        public static Vector2 MousePosition => new(PlayerInput.MouseX, PlayerInput.MouseY);

        public static Vector2 UIMousePosition => UserInterface.ActiveInstance.MousePosition;

        public static float TimeRatio => Utils.GetDayTimeAs24FloatStartingFromMidnight() / 24f;
    }

    public static string GetReadableTime() => GetReadableTime(Utils.GetDayTimeAs24FloatStartingFromMidnight());

    public static string GetReadableTime(float time)
    {
        int hour = (int)MathF.Floor(time % 24);

        int minute = (int)MathF.Floor(time % 1 * 100 * 0.6f);

        DateTime date = new(1, 1, 1, hour, minute, 0);

        return date.ToShortTimeString();
    }

    public static Color FromHex3(string hexString)
    {
        Color output = new();

        // Filter the starting hash.
        if (hexString.StartsWith('#'))
            hexString = hexString[1..];

        if (uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out uint hex))
        {
            uint r = (hex >> 16) & 0xFFu;
            uint g = (hex >> 8) & 0xFFu;
            uint b = hex & 0xFFu;

            output = new((int)r, (int)g, (int)b);
        }

        return output;
    }

    /// <param name="accending">
    /// <list type="bullet">
    ///     <term><see cref="false"/></term>
    ///     The instance of <typeparamref name="T"/> should be found based on preceding order.
    ///     <item/>
    ///     <term><see cref="true"/></term>
    ///     The instance of <typeparamref name="T"/> should be found based on accending order.
    /// </list>
    /// </param>
    public static T CompareFor<T>(
        this IEnumerable<T> collection,
        Func<T, IComparable> getComparable,
        bool accending = true
    )
    {
        T matching = collection.First();
        IComparable lastComparison = getComparable(matching);

        foreach (T item in collection)
        {
            IComparable compare = getComparable(item);

            if ((compare.CompareTo(lastComparison) >= 0) == accending)
            {
                matching = item;
                lastComparison = compare;
            }
        }

        return matching;
    }

    /// <inheritdoc cref="CompareFor{T}(IEnumerable{T}, Func{T, IComparable}, bool)"/>
    public static T CompareFor<T, TComparable>(
        this IEnumerable<T> collection,
        Func<T, TComparable> getComparable,
        out TComparable lastComparison,
        bool accending = true
    ) where TComparable : IComparable
    {
        T matching = collection.First();
        lastComparison = getComparable(matching);

        foreach (T item in collection)
        {
            TComparable compare = getComparable(item);

            if ((compare.CompareTo(lastComparison) >= 0) == accending)
            {
                matching = item;
                lastComparison = compare;
            }
        }

        return matching;
    }

    /// <returns>Inclusive range between <paramref name="start"/> and <paramref name="end"/>.</returns>
    public static IEnumerable<char> Range(this char start, char end) => Enumerable.Range(start, end - start + 1).Select(i => (char)i);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T As<T>(this object @this) where T : class => (T)@this;
}
