using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using ZenSkies.Core.Utils;

namespace ZenSkies.Core.UI;

public class EasingStyleOption : UITextPanel<LocalizedText>
{
    #region Private Fields

    private const string EasingsKeyPrefix = "Mods.ZenSkies.Easings";

    #endregion

    #region Public Fields

    public readonly EasingStyle Value;

    #endregion

    #region Public Constructors

    public EasingStyleOption(EasingStyle style, float textScale = 0.8f, bool large = false)
        : base(GetEasingName(style), textScale, large)
    {
        Value = style;

        _backgroundTexture = UITextures.FullPanel;
        _borderTexture = MiscTextures.Invis;

        Width.Set(0, 1f);

        TextHAlign = 0f;

        SetPadding(6);

        SetText(_text, textScale, large);
    }

    #endregion

    #region Updating

    public override void Update(GameTime gameTime)
    {
        if (IsMouseHovering)
            BackgroundColor = UICommon.DefaultUIBlue;
        else
            BackgroundColor = UICommon.DefaultUIBlueMouseOver;
    }

    #endregion

    #region Sorting

    public override int CompareTo(object obj)
    {
        if (obj is EasingStyleOption element)
            return element.Value > Value ? -1 : 1;

        return 0;
    }

    #endregion

    #region Private Methods

    private static LocalizedText GetEasingName(EasingStyle style) =>
        Language.GetText($"{EasingsKeyPrefix}.{style}");

    #endregion
}
