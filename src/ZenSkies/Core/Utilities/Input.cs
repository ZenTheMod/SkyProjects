using Daybreak.Common.Features.Hooks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using ReLogic.Localization.IME;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameInput;

namespace ZenSkies.Core;

[Obsolete("Move to DAYBREAK's implementation")]
public static class Input
{
    public enum CancellationType : byte
    {
        None,
        Escaped,
        Confirmed
    }

    #region Private Fields

    private const int MaxStrokeLength = 20;

    private const int KeyDelay = 45;

    private static readonly char[] InvalidChars =
        [.. '\x0'.Range('\x1F'), // Invisible characters from Null to Unit Separator, stopping before Space.
        '\x7F' // Delete.
        ];

    #endregion

    #region Public Properties

    /// <summary>
    /// If text can be input.<br/>
    /// Simply a wrapper for <see cref="PlayerInput.WritingText"/>.
    /// </summary>
    public static bool WritingText
    {
        get => PlayerInput.WritingText;
        set
        {
            WasWritingText |= value;
            PlayerInput.WritingText = value;
        }
    }

    /// <summary>
    /// If text was being written outside of a drawing scope, where most input logic unfortunatly takes place.
    /// </summary>
    public static bool WasWritingText { get; private set; }

    /// <summary>
    /// The index of the "cursor" where written text is inserted.
    /// </summary>
    public static int CursorPositon { get; set; }

    public static string KeyStroke { get; private set; }
        = string.Empty;

    public static int BackspaceTimer { get; private set; }
        = KeyDelay;

    public static int LeftArrowTimer { get; private set; }
        = KeyDelay;

    public static int RightArrowTimer { get; private set; }
        = KeyDelay;

    #endregion

    #region Loading

    [OnLoad]
    public static void Load()
    {
        On_Main.DoUpdate_HandleInput += UpdateWasWritingText;
        Platform.Get<IImeService>().AddKeyListener(OnKeyStroke);
    }

    [OnUnload]
    public static void Unload()
    {
        On_Main.DoUpdate_HandleInput -= UpdateWasWritingText;
        Platform.Get<IImeService>().RemoveKeyListener(OnKeyStroke);
    }

    private static void UpdateWasWritingText(On_Main.orig_DoUpdate_HandleInput orig, Main self)
    {
        WasWritingText = WritingText;
        orig(self);
    }

    private static void OnKeyStroke(char key)
    {
        if (WritingText && 
            KeyStroke.Length <= MaxStrokeLength)
            KeyStroke += key;
    }

    #endregion

    #region Input

    /// <summary>
    /// Vastly simplified, and improved; vanilla logic for handling text input, should be calling during drawing to prevent issues.<br/>
    /// Additional features include:
    /// <list type="bullet">
    ///     Support for a "cursor" to specify where text should be inserted, best used with <c>DrawInputString</c> methods.
    ///     <item/>
    ///     Black-listing/white-listing of specific characters with <paramref name="blacklistedChars"/>, and <paramref name="whitelistedChars"/>.
    ///     <item/>
    ///     Better input cancellation system using <see cref="CancellationType"/>.
    /// </list>
    /// </summary>
    public static CancellationType GetInput(
        string input,
        out string output,
        bool allowLineBreaks = false,
        IEnumerable<char>? blacklistedChars = null,
        IEnumerable<char>? whitelistedChars = null)
    {
        output = input;

            // Perhaps I may want to use my own KeyboardStates?
        Main.oldInputText = Main.inputText;
	    Main.inputText = Keyboard.GetState();

            // Should only write while focused.
        WritingText &= Main.hasFocus;

        if (!WritingText)
            return CancellationType.None;

        Main.instance.HandleIME();

        blacklistedChars ??= [];
        blacklistedChars = blacklistedChars.Concat(InvalidChars);

        #region Cursor

            // Left
        if (Keys.Left.Held)
            LeftArrowTimer--;
        else
            LeftArrowTimer = KeyDelay;

        if (Keys.Left.JustPressed ||
            (Keys.Left.Held &&
            LeftArrowTimer <= 0))
            CursorPositon--;

            // Right
        if (Keys.Right.Held)
            RightArrowTimer--;
        else
            RightArrowTimer = KeyDelay;

        if (Keys.Right.JustPressed ||
            (Keys.Right.Held &&
            RightArrowTimer <= 0))
            CursorPositon++;

        CursorPositon = Math.Clamp(CursorPositon, 0, output.Length);

        #endregion

        #region Special Actions

        bool controlPressed =
            (Keys.LeftControl.Pressed ||
            Keys.RightControl.Pressed) &&
            !Keys.LeftAlt.Pressed &&
            !Keys.RightAlt.Pressed;

        bool shiftPressed =
            Keys.LeftShift.Pressed ||
            Keys.RightShift.Pressed;

            // Clear
        if (controlPressed && Keys.Z.JustPressed)
        {
            output = string.Empty;
            CursorPositon = 0;
        }
            // Cut
        else if (
            (controlPressed && Keys.X.JustPressed) ||
            (shiftPressed && Keys.Delete.JustPressed))
        {
            Platform.Get<IClipboard>().Value = output;
            output = string.Empty;
            CursorPositon = 0;
        }
            // Copy
        else if (Keys.C.JustPressed)
            Platform.Get<IClipboard>().Value = output;
            // Paste
        else if (
            (controlPressed && Keys.V.JustPressed) ||
            (shiftPressed && Keys.Insert.JustPressed))
        {
            string paste = GetPaste(output, allowLineBreaks);

            paste = paste.Replace(blacklistedChars, string.Empty);

            if (whitelistedChars is not null &&
                whitelistedChars.Any())
                paste = paste.Only(whitelistedChars);

            output = output.Insert(CursorPositon, paste);

            CursorPositon += paste.Length;
        }

        #endregion

        #region Input

        if (KeyStroke.Length >= 1)
        {
            string stroke = KeyStroke.Replace(blacklistedChars, string.Empty);

            if (whitelistedChars is not null &&
                whitelistedChars.Any())
                stroke = stroke.Only(whitelistedChars);

            KeyStroke = string.Empty;

            output = output.Insert(CursorPositon, stroke);

            CursorPositon += stroke.Length;
        }

        #endregion

        #region Backspace

        if (Keys.Back.Held)
            BackspaceTimer--;
        else
            BackspaceTimer = KeyDelay;

        if ((Keys.Back.JustPressed ||
            (Keys.Back.Held &&
            BackspaceTimer <= 0)) &&
            output.Length >= 1 &&
            CursorPositon >= 1)
        {
            output = string.Concat(output.AsSpan(0, CursorPositon - 1),
                output.AsSpan(CursorPositon, output.Length - CursorPositon));

            CursorPositon--;
        }

        #endregion

        #region Escapes

            // Unsure of why vanilla checks if you're on Windows before allowing escape inputs.
                // if (!Platform.IsWindows && Main.inputText.IsKeyDown(Keys.Escape) && !Main.oldInputText.IsKeyDown(Keys.Escape))
        if (Keys.Escape.JustPressed)
        {
                // Definitly sketchy, but is designed to prevent UI from vanishing whilst typing.
            PlayerInput.WritingText = false;
            return CancellationType.Escaped;
        }

        if (Keys.Enter.JustPressed)
        {
            WritingText = false;
            return CancellationType.Confirmed;
        }

        #endregion

        return CancellationType.None;
    }

    private static string GetPaste(string input, bool allowLineBreaks) =>
        input.Insert(CursorPositon,
            allowLineBreaks ? Platform.Get<IClipboard>().MultiLineValue : Platform.Get<IClipboard>().Value);

    #endregion

    #region Drawing

    /// <summary>
    /// Ultimately slower version of typical string drawing methods, that allows for a cursor to be drawn in-between characters.<br/>
    /// Made for use with <c>Input.GetInput</c>, and <see cref="CursorPositon"/> as <paramref name="blinkerIndex"/>.
    /// </summary>
    /// <param name="mousePosition">Position that should be checked to find <paramref name="hoveredChar"/>.</param>
    /// <param name="drawBlinker">Weither or not the cursor/"blinker" should be drawn, best used with a timer.</param>
    /// <param name="blinkerIndex">The index at which the blinker should be drawn, usually <see cref="CursorPositon"/>.</param>
    public static void DrawInputString(
        this SpriteBatch spriteBatch,
        Vector2 mousePosition,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        Vector2 origin,
        Vector2 scale,
        out int hoveredChar,
        bool drawBlinker = false,
        int blinkerIndex = -1) =>
        spriteBatch.DrawInputStringWithShadow(mousePosition, font, text, position, color, Color.Black, origin, scale, out hoveredChar, drawBlinker, blinkerIndex, -1f);

    /// <inheritdoc cref="DrawInputString"/>
    public static void DrawInputStringWithShadow(
        this SpriteBatch spriteBatch,
        Vector2 mousePosition,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        Vector2 origin,
        Vector2 scale,
        out int hoveredChar,
        bool drawBlinker = false,
        int blinkerIndex = -1,
        float spread = 2f) =>
        spriteBatch.DrawInputStringWithShadow(mousePosition, font, text, position, color, Color.Black, origin, scale, out hoveredChar, drawBlinker, blinkerIndex, spread);

    /// <inheritdoc cref="DrawInputString"/>
    public static void DrawInputStringWithShadow(
        this SpriteBatch spriteBatch,
        Vector2 mousePosition,
        DynamicSpriteFont font,
        string text,
        Vector2 position,
        Color color,
        Color shadowColor,
        Vector2 origin,
        Vector2 scale,
        out int hoveredChar,
        bool drawBlinker = false,
        int blinkerIndex = -1,
        float spread = 2f)
    {
        bool first = true;
        float lastKerning = 0f;

        hoveredChar = 0;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            spriteBatch.DrawStringWithShadow(font, c.ToString(), position, color, shadowColor, 0f, origin, scale, spread);

            if (drawBlinker &&
                i == blinkerIndex)
            {
                Vector2 blinkerPosition = new(position.X - (2f * scale.X), position.Y);

                spriteBatch.DrawStringWithShadow(font, "|", blinkerPosition, color, shadowColor, 0f, origin, scale, spread);
            }

            Vector2 charSize = font.MeasureChar(c, first, lastKerning, out lastKerning);

            if (mousePosition.X >= position.X && mousePosition.X <= position.X + charSize.X)
                hoveredChar = mousePosition.X >= position.X + (charSize.X * 0.5f) ? i + 1 : i;

            position.X += font.MeasureChar(c, first, lastKerning, out lastKerning).X;
            first = false;
        }

        if (mousePosition.X >= position.X)
            hoveredChar = text.Length;

        if (drawBlinker &&
            blinkerIndex >= text.Length)
        {
            Vector2 blinkerPosition = new(position.X - (2f * scale.X), position.Y);

            spriteBatch.DrawStringWithShadow(font, "|", blinkerPosition, color, shadowColor, 0f, origin, scale, spread);
        }
    }

    #endregion

    #region Extension Members

    extension(Keys key)
    {
        public bool Pressed =>
            Main.inputText.IsKeyDown(key);

        public bool Held =>
            Main.inputText.IsKeyDown(key) &&
            Main.oldInputText.IsKeyDown(key);

        public bool JustPressed =>
            Main.inputText.IsKeyDown(key) &&
            !Main.oldInputText.IsKeyDown(key);

        public bool JustReleased =>
            !Main.inputText.IsKeyDown(key) &&
            Main.oldInputText.IsKeyDown(key);
    }

    #endregion
}
