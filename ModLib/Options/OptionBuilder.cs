using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ModLib.Options;

/// <summary>
/// Helper class for building <c>OpTab</c>s with a variety of chain-able methods.
/// </summary>
/// <remarks>To return the modified <c>OpTab</c> object, use <see cref="Build()"/>.</remarks>
public class OptionBuilder
{
    private Vector2 vector2 = new(100f, 400f);
    private readonly OpTab opTab;

    /// <summary>
    /// Initializes a new <c>OptionBuilder</c> instance for creating option tabs.
    /// </summary>
    /// <param name="owner">The <c>OptionInterface</c> who will own the resulting <c>OpTab</c> instance.</param>
    /// <param name="tabName">The name of the tab itself, displayed on the left side of the menu; Only visible with two or more tabs.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpTab</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    public OptionBuilder(OptionInterface owner, string tabName, params Color[] colors)
    {
        opTab = new OpTab(owner, tabName)
        {
            colorButton = GetColorOrDefault(colors, 0),
            colorCanvas = GetColorOrDefault(colors, 1)
        };

        opTab.AddItems(
            [
                new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), ModPlugin.Assembly.GetModName(), bigText: true),
                new OpLabel(new Vector2(245f, 510f), new Vector2(200f, 15f), $"[v{ModPlugin.Assembly.GetModVersion()}]")
                {
                    color = GetColorOrDefault(colors, 2, Color.gray)
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
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpCheckBox</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
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
                color = GetColorOrDefault(colors, 0)
            },
            new OpCheckBox(configurable, vector2)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack)
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
    /// <param name="width">The width of the combo box element.</param>
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpComboBox</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
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
                color = GetColorOrDefault(colors, 0)
            },
            new OpComboBox(configurable, vector2 + new Vector2(180f, 00f), width)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack)
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
    /// <param name="colors">
    ///     The color values to be used by the <c>OpLabel</c> and <c>OpSlider</c> instance.
    ///     Colors are retrieved by index and applied to relevant fields in alphabetical order.
    /// </param>
    /// <returns>The <c>OptionBuilder</c> object.</returns>
    public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, float multi = 1f, bool vertical = false, params Color[] colors)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
            {
                description = configurable.info.description,
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = GetColorOrDefault(colors, 0)
            },
            new OpSlider(configurable, vector2 + new Vector2(200f, 0f), multi, vertical)
            {
                description = configurable.info.description,
                colorEdge = GetColorOrDefault(colors, 1),
                colorFill = GetColorOrDefault(colors, 2, MenuColorEffect.rgbBlack),
                colorLine = GetColorOrDefault(colors, 3, MenuColorEffect.rgbVeryDarkGrey)
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
    public OptionBuilder AddText(string text, Vector2 size, bool bigText = false, Color? color = default)
    {
        UIelement[] UIarrayOptions =
        [
            new OpLabel(vector2 + new Vector2(100f + size.x, 10f), size, text, FLabelAlignment.Center, bigText)
            {
                verticalAlignment = OpLabel.LabelVAlignment.Center,
                color = color ?? MenuColorEffect.rgbMediumGrey
            }
        ];

        opTab.AddItems(UIarrayOptions);

        vector2.y -= size.y + 30f;

        return this;
    }

    /// <summary>
    /// Retrieves a color from the given array, a fallback if provided, or the default Rain World color for menu elements (rgbMediumGrey) if neither are provided.
    /// </summary>
    /// <param name="colors">The color array to search for a given color.</param>
    /// <param name="index">The index of the color to be retrieved.</param>
    /// <param name="fallback">A fallback color to use if the given index does not have a value.</param>
    /// <returns>A <c>Color</c> instance for usage by menu elements.</returns>
    private Color GetColorOrDefault(Color[] colors, int index, Color fallback = default)
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