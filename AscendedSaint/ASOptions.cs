using Menu.Remix.MixedUI;
using UnityEngine;

namespace AscendedSaint
{
    /// <summary>
    /// The mod's REMIX options.
    /// </summary>
    public class ASOptions : OptionInterface
    {
        /// <summary>
        /// Holds all of the player's client settings.
        /// </summary>
        public class ClientOptions
        {
            public bool allowSelfAscension;
            public bool allowRevival;
            public bool requireKarmaFlower;
            public float revivalHealthFactor;

            public ClientOptions()
            {
                RefreshOptions();
            }

            /// <summary>
            /// Sets all of the client's settings to those from the REMIX options menu.
            /// </summary>
            public void RefreshOptions()
            {
                allowSelfAscension = ALLOW_SELF_ASCENSION.Value;
                allowRevival = ALLOW_REVIVAL.Value;
                requireKarmaFlower = REQUIRE_KARMA_FLOWER.Value;
                revivalHealthFactor = REVIVAL_HEALTH_FACTOR.Value * 0.01f;
            }
        }

        public static Configurable<bool> ALLOW_SELF_ASCENSION;
        public static Configurable<bool> ALLOW_REVIVAL;
        public static Configurable<bool> REQUIRE_KARMA_FLOWER;
        public static Configurable<int> REVIVAL_HEALTH_FACTOR;

        public ASOptions()
        {
            ALLOW_SELF_ASCENSION = config.Bind("allow_self_ascension", true, new ConfigurableInfo("If enabled, Saint's Ascension ability can affect themselves."));

            ALLOW_REVIVAL = config.Bind("allow_revival", true, new ConfigurableInfo("If enabled, using the Ascension ability on a corpse whilst holding a Karma Flower will revive it."));
            REQUIRE_KARMA_FLOWER = config.Bind("require_karma_flower", true, new ConfigurableInfo("Whether or not reviving a creature will require a Karma Flower."));
            REVIVAL_HEALTH_FACTOR = config.Bind("revival_health_factor", 50, new ConfigurableInfo("How much of a creature's health will be restored upon revival. For Slugcats, this is always 100."));
        }

        public override void Initialize()
        {
            ASLogger.LogInfo("ASOptions initialized.");
            base.Initialize();

            Tabs = new OpTab[1];

            Tabs[0] = new OptionBuilder(this, "Main Options")
                .AddCheckBoxOption("Allow Self-Ascension", ALLOW_SELF_ASCENSION)
                .AddCheckBoxOption("Allow Creature Revival", ALLOW_REVIVAL)
                .AddCheckBoxOption("Require Karma Flower", REQUIRE_KARMA_FLOWER)
                .AddSliderOption("Revival Health Factor", REVIVAL_HEALTH_FACTOR)
                .Build();
        }

        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Helper class for building <c>OpTab</c>s with a variety of chain-able methods.
        /// </summary>
        /// <remarks>To return the modified <c>OpTab</c> object, use <see cref="Build()"/>.</remarks>
        internal class OptionBuilder
        {
            private Vector2 vector2 = new Vector2(100f, 400f);
            private readonly OpTab opTab;

            public OptionBuilder(OptionInterface owner, string tabName)
            {
                opTab = new OpTab(owner, tabName);

                opTab.AddItems(
                    new UIelement[] {
                        new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), AscendedSaintMain.PLUGIN_NAME, FLabelAlignment.Center, true),
                        new OpLabel(new Vector2(200f, 505f), new Vector2(200f, 15f), "v" + AscendedSaintMain.PLUGIN_VERSION, FLabelAlignment.Center, false)
                        {
                            color = Color.gray
                        }
                    }
                );
            }

            /// <summary>
            /// Returns the generated <c>OpTab</c> object with the applied options of previous methods.
            /// </summary>
            /// <returns>The builder's <c>OpTab</c> instance.</returns>
            public OpTab Build()
            {
                return opTab;
            }

            /// <summary>
            /// Adds a new <c>OpCheckBox</c> to the <c>OpTab</c> instance, with a descriptive <c>OpLabel</c> after it.
            /// </summary>
            /// <param name="text">The check box's label. Will be displayed right after the box itself.</param>
            /// <param name="configurable">The <c>Configurable</c> this check box will be bound to.</param>
            /// <returns>The <c>OptionBuilder</c> object.</returns>
            public OptionBuilder AddCheckBoxOption(string text, Configurable<bool> configurable)
            {
                UIelement[] UIarrayOptions = new UIelement[]
                {
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
                };

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
                UIelement[] UIarrayOptions = new UIelement[]
                {
                    new OpLabel(vector2 + new Vector2(40f, 0f), new Vector2(100f, 24f), text)
                    {
                        description = configurable.info.description,
                        alignment = FLabelAlignment.Center,
                        verticalAlignment = OpLabel.LabelVAlignment.Center
                    },
                    new OpSlider(configurable, vector2 + new Vector2(200f, 0f), multi, vertical)
                    {
                        description = configurable.info.description
                    }
                };

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
        }
    }
}