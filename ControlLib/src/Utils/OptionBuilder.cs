using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ControlLib.Utils;

/// <summary>
/// Helper class for building <c>OpTab</c>s with a variety of chain-able methods.
/// </summary>
/// <remarks>To return the modified <c>OpTab</c> object, use <see cref="Build()"/>.</remarks>
internal class OptionBuilder
{
    private Vector2 vector2 = new(100f, 400f);
    private readonly OpTab opTab;

    public OptionBuilder(OptionInterface owner, string tabName, params Color[] colors)
    {
        opTab = new OpTab(owner, tabName)
        {
            colorButton = GetDefaultedColor(colors, 0)
        };

        opTab.AddItems(
            [
                new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), ControlLibMain.PLUGIN_NAME, bigText: true),
                new OpLabel(new Vector2(245f, 510f), new Vector2(200f, 15f), $"[v{ControlLibMain.PLUGIN_VERSION}]")
                {
                    color = Color.gray
                }
            ]
        );
    }

    /// <summary>
    /// Returns the generated <c>OpTab</c> object with the applied options of previous methods.
    /// </summary>
    /// <returns>The builder's <c>OpTab</c> instance.</returns>
    public OpTab Build() => opTab;

    /// <summary>
    /// Adds a new <c>OpCheckBox</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> after it.
    /// </summary>
    /// <param name="text">The check box's label. Will be displayed right after the box itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this check box will be bound to.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable, params Color[] colors)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                alignment = FLabelAlignment.Left,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetDefaultedColor(colors, 0)
            },
            new OpCheckBox(configurable, vector2)
            {
                description = configurable.info.description,
                colorEdge = GetDefaultedColor(colors, 1),
                colorFill = GetDefaultedColor(colors, 2, MenuColorEffect.rgbBlack)
            }
        ];

        vector2.y -= 32f;

        opTab.AddItems(UIarrayOptions);

        return this;
    }

    /// <summary>
    /// Adds a new <c>OpComboBox</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> after it.
    /// </summary>
    /// <param name="text">The combo box's label. Will be displayed right after the box itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this combo box will be bound to.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddComboBoxOption(string text, Configurable<string> configurable, float width = 200, params Color[] colors)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                alignment = FLabelAlignment.Left,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetDefaultedColor(colors, 0)
            },
            new OpComboBox(configurable, vector2 + new Vector2(180f, 00f), width)
            {
                description = configurable.info.description,
                colorEdge = GetDefaultedColor(colors, 1),
                colorFill = GetDefaultedColor(colors, 2, MenuColorEffect.rgbBlack)
            }
        ];

        vector2.y -= 32f;

        opTab.AddItems(UIarrayOptions);

        return this;
    }

    /// <summary>
    /// Adds a new <c>OpSlider</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> before it.
    /// </summary>
    /// <param name="text">The slider's label. Will be displayed right before the slider itself.</param>
    /// <param name="configurable">The <c>Configurable</c> this slider will be bound to.</param>
    /// <param name="multi">A multiplier for the slider's size.</param>
    /// <param name="vertical">If this slider should be vertical.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, float multi = 1f, bool vertical = false, params Color[] colors)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetDefaultedColor(colors, 0)
            },
            new OpSlider(configurable, vector2 + new Vector2(200f, 0f), multi, vertical)
            {
                description = configurable.info.description,
                colorEdge = GetDefaultedColor(colors, 1),
                colorFill = GetDefaultedColor(colors, 2, MenuColorEffect.rgbBlack),
                colorLine = GetDefaultedColor(colors, 3, MenuColorEffect.rgbVeryDarkGrey)
            }
        ];

        vector2.y -= 32f;

        opTab.AddItems(UIarrayOptions);

        return this;
    }

    /// <summary>
    /// Adds extra space before the next object added.
    /// </summary>
    /// <param name="padding">The amount of padding to be added.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddPadding(Vector2 padding)
    {
        vector2 -= padding;

        return this;
    }

    /// <summary>
    /// Adds a new <c>OpLabel</c> to the <c>OpTab</c> instance.
    /// </summary>
    /// <param name="text">The text to be rendered.</param>
    /// <param name="size">The size of the label element.</param>
    /// <param name="bigText">If this text should be rendered larger than usual.</param>
    /// <param name="color">The color of the text.</param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddText(string text, Vector2 size, bool bigText = false, params Color[] colors)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(140f, 0f), size, text, FLabelAlignment.Center, bigText)
            {
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetDefaultedColor(colors, 0)
            }
        ];

        opTab.AddItems(UIarrayOptions);

        vector2.y -= size.y;

        return this;
    }

    private Color GetDefaultedColor(Color[] colors, int index, Color fallback = default)
    {
        Color color = colors.ElementAtOrDefault(index);

        if (color == default)
        {
            color = fallback != default
                ? fallback
                : MenuColorEffect.rgbMediumGrey;
        }

        return color;
    }
}