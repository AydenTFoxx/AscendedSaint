using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ControlLib;

/// <summary>
/// The mod's REMIX options.
/// </summary>
public class CLOptions : OptionInterface
{
    /// <summary>
    /// Holds all of the player's client settings.
    /// </summary>
    public record class ClientOptions
    {
        public string? selectionMode;

        /// <summary>
        /// Creates a new <c>ClientOptions</c> instance with the mod's REMIX options' values.
        /// </summary>
        public ClientOptions()
        {
            RefreshOptions();
        }

        /// <summary>
        /// Sets all of the client's settings to those from the REMIX options menu.
        /// </summary>
        public void RefreshOptions() => selectionMode = SELECTION_MODE!.Value;

        /// <summary>
        /// Sets the client's options to those from the given <c>ClientOptions</c> instance.
        /// </summary>
        /// <param name="options">The <c>ClientOptions</c> instance whose values will be copied.</param>
        public void SetOptions(ClientOptions options) => selectionMode = options.selectionMode;
    }

    public static Configurable<string>? SELECTION_MODE;

    public CLOptions()
    {
        SELECTION_MODE = config.Bind(
            "selection_mode",
            "classic",
            new ConfigurableInfo(
                "Which mode to use for selecting creatures to possess.<LINE>Classic is list-based; Ascension is akin to Saint's ascension ability.",
                new ConfigAcceptableList<string>("classic", "ascension")
            )
        );
    }

    public override void Initialize()
    {
        CLLogger.LogInfo($"{nameof(CLOptions)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[1];

        Tabs[0] = new OptionBuilder(this, "Possession")
            .AddComboBoxOption("Selection Mode", SELECTION_MODE!)
            .Build();
    }

    public override void Update() => base.Update();

    /// <summary>
    /// Helper class for building <c>OpTab</c>s with a variety of chain-able methods.
    /// </summary>
    /// <remarks>To return the modified <c>OpTab</c> object, use <see cref="Build()"/>.</remarks>
    internal class OptionBuilder
    {
        private Vector2 vector2 = new(100f, 400f);
        private readonly OpTab opTab;

        public OptionBuilder(OptionInterface owner, string tabName, Color colorButton = default)
        {
            opTab = new OpTab(owner, tabName)
            {
                colorButton = colorButton != default ? colorButton : MenuColorEffect.rgbMediumGrey
            };

            opTab.AddItems(
                [
                    new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), ControlLibMain.PLUGIN_NAME, bigText: true),
                    new OpLabel(new Vector2(200f, 505f), new Vector2(200f, 15f), "v" + ControlLibMain.PLUGIN_VERSION)
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
        public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable)
        {
            UIelement[] UIarrayOptions =
            [
                new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
                {
                    description = configurable.info.description,
                    alignment = FLabelAlignment.Left,
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpCheckBox(configurable, vector2)
                {
                    description = configurable.info.description
                }
            ];

            vector2.y -= 32f;

            opTab.AddItems(UIarrayOptions);

            return this;
        }

        public OptionBuilder AddComboBoxOption(string text, Configurable<string> configurable, float width = 200)
        {
            UIelement[] UIarrayOptions =
            [
                new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
                {
                    description = configurable.info.description,
                    alignment = FLabelAlignment.Left,
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpComboBox(configurable, vector2, width)
                {
                    description = configurable.info.description
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
        public OptionBuilder AddSliderOption(string text, Configurable<int> configurable, float multi = 1f, bool vertical = false)
        {
            UIelement[] UIarrayOptions =
            [
                new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
                {
                    description = configurable.info.description,
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
                new OpSlider(configurable, vector2 + new Vector2(200f, 0f), multi, vertical)
                {
                    description = configurable.info.description
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
            vector2 += padding;

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
        public OptionBuilder AddText(string text, Vector2 size, bool bigText = false, Color color = default)
        {
            UIelement[] UIarrayOptions =
            [
                new OpLabel(vector2 + new Vector2(180f, 0f), size, text, FLabelAlignment.Center, bigText)
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center,
                    color = color == default ? MenuColorEffect.rgbMediumGrey : color
                }
            ];

            opTab.AddItems(UIarrayOptions);

            vector2.y -= size.y;

            return this;
        }
    }
}