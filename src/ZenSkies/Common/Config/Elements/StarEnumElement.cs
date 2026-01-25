using Daybreak.Common.Features.TmlConfig;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.UI.Chat;
using ZenSkies.Common.Systems.Sky.Space;
using ZenSkies.Core;
using Star = ZenSkies.Common.Systems.Sky.Space.Star;

namespace ZenSkies.Common.Config.Elements;

[ProvidesConfigElementFor<StarVisual>]
[Obsolete("Obsolete following Configuration being merged into DAYBREAK")]
internal sealed class StarEnumElement : ConfigElement<StarVisual>
{
    private const float time_multiplier = 1.2f;

    private Star display_star = new() 
    { 
        Position = Vector2.Zero,
        Color = Color.White,
        Scale = 2f,
        Style = 0,
        Rotation = 0,
        TwinklePhase = 1f,
        IsActive = true
    };

    private string[]? enumNames;

    public override void OnBind()
    {
        base.OnBind();

        OnLeftClick += (_, _) => Value = Value.NextEnum();

        OnRightClick += (_, _) => Value = Value.PreviousEnum();

        enumNames = Enum.GetNames(typeof(StarVisual));

        for (int i = 0; i < enumNames.Length; i++)
        {
            FieldInfo? enumFieldFieldInfo = MemberInfo.Type.GetField(enumNames[i]);

            if (enumFieldFieldInfo is null)
                continue;

            string name = ConfigManager.GetLocalizedLabel(new(enumFieldFieldInfo));
            enumNames[i] = name;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        Rectangle dims = this.Dimensions;

        string text = enumNames?[(int)Value] ?? string.Empty;

        DynamicSpriteFont font = FontAssets.ItemStack.Value;

        Vector2 textSize = font.MeasureString(text);
        Vector2 origin = new(textSize.X, 0);
        {
            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2(dims.X + dims.Width - 36f, dims.Y + 8f), Color.White, 0f, origin, new(0.8f));
        }

        {
            DrawPanel2(spriteBatch, new(dims.X + dims.Width - dims.Height, dims.Y + 2), TextureAssets.SettingsPanel.Value, dims.Height - 4, dims.Height - 4, Color.Black);

            display_star.Position = new(dims.X + dims.Width - (dims.Height * 0.5f) - 2, dims.Y + (dims.Height * 0.5f));

            StarVisual style = Value;

            if (Value == StarVisual.Random)
            {
                style = (StarVisual)((int)(Main.GlobalTimeWrappedHourly * time_multiplier) % 3) + 1;
            }

            StarRendering.DrawStar(spriteBatch, 1, 0f, display_star, style);
        }
    }
}
