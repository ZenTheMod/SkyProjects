using Microsoft.Xna.Framework;
using System;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace ZenSkies.Core.UI;

public class ColorInputFields : UIElement
{
    #region Private Fields

    private readonly ColorPicker TargetPicker;

    private static readonly char[] AllowedHexChars =
        [.. '0'.Range('9'), .. 'A'.Range('F'), .. 'a'.Range('f')];

    private readonly InputField HexInput;

    private static readonly char[] AllowedRGBChars =
        [.. '0'.Range('9')];

    private static readonly string[] RGBLabels =
        ["R", "G", "B"];

    private const int RGBInputWidth = 70;

    private readonly InputField[] RGBInputs;

    #endregion

    #region Public Events

        // TODO: Generic impl of UIElementAction.
    public event Action<ColorInputFields>? OnAcceptInput;

    #endregion

    #region Public Properties

    public Color Color
    {
        get => TargetPicker.Color;
        set => TargetPicker.Color = value;
    }

    #endregion

    #region Constructor

    public ColorInputFields(ColorPicker picker, Color? panelColor = null) : base()
    {
        TargetPicker = picker;

        Width.Set(0f, 1f);
        Height.Set(26f, 0f);

        #region Hex

        UIText hashtag = new("#");

        hashtag.Top.Set(4f, 0f);
        hashtag.Left.Set(4f, 0f);

        Append(hashtag);

        HexInput = new(string.Empty, 6);

        HexInput.Width.Set(76f, 0f);

        HexInput.Left.Set(16f, 0f);

        HexInput.WhitelistedChars = AllowedHexChars;

        HexInput.OnEnter += AcceptHex;

        HexInput.BackgroundColor = panelColor ?? HexInput.BackgroundColor;

        Append(HexInput);

        #endregion

        #region RGB

        RGBInputs = new InputField[3];

        for (int i = 0; i < RGBInputs.Length; i++)
            CreateRGBInput(i, panelColor);

        #endregion
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        HexInput.Hint = Terraria.Utils.Hex3(Color);

        RGBInputs[0]?.Hint = Color.R.ToString();
        RGBInputs[1]?.Hint = Color.G.ToString();
        RGBInputs[2]?.Hint = Color.B.ToString();
    }

    #endregion

    #region Inputs

    private void AcceptHex(InputField field)
    {
        Color newColor = Utilities.FromHex3(field.Text);

        if (newColor != Color.Transparent)
            Color = newColor;

        field.Text = string.Empty;

        OnAcceptInput?.Invoke(this);
    }

    private void AcceptRGB(InputField field, int component)
    {
        string text = field.Text;

        field.Text = string.Empty;

        if (!int.TryParse(text, out int value))
            return;

        Color = component switch
        {
            0 => new(value, Color.G, Color.B),
            1 => new(Color.R, value, Color.B),
            2 => new(Color.R, Color.G, value),
            _ => Color
        };

        OnAcceptInput?.Invoke(this);
    }

    private void CreateRGBInput(int i, Color? panelColor = null)
    {
        UIText label = new(RGBLabels[i]);

        label.Top.Set(4f, 0f);
        label.Left.Set(4 - (RGBInputWidth * (3 - i)), 1f);

        Append(label);

        RGBInputs[i] = new(string.Empty, 3);

        RGBInputs[i].Width.Set(50f, 0f);

        RGBInputs[i].Left.Set(-50f - (RGBInputWidth * (2 - i)), 1f);

        RGBInputs[i].WhitelistedChars = AllowedRGBChars;

        RGBInputs[i].OnEnter +=
            f => AcceptRGB(f, i);

        RGBInputs[i].BackgroundColor = panelColor ?? RGBInputs[i].BackgroundColor;

        Append(RGBInputs[i]);
    }

    #endregion
}
