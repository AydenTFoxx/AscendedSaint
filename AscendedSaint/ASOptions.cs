using Menu.Remix.MixedUI;
using UnityEngine;

namespace AscendedSaint
{
    /// <summary>
    /// The mod's REMIX options.
    /// </summary>
    public class ASOptions : OptionInterface
    {
        public class SharedOptions
        {
            public bool allowSelfAscension;
            public bool allowRevival;
            public bool requireKarmaFlower;
            public float revivalHealthFactor;

            public SharedOptions()
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

        private Vector2 vector2 = new Vector2(100f, 400f);

        public ASOptions()
        {
            ALLOW_SELF_ASCENSION = config.Bind("allow_self_ascension", true, new ConfigurableInfo("If enabled, Saint's Ascension ability can affect themselves."));

            ALLOW_REVIVAL = config.Bind("allow_revival", true, new ConfigurableInfo("If enabled, using the Ascension ability on a corpse whilst holding a Karma Flower will revive it."));
            REQUIRE_KARMA_FLOWER = config.Bind("require_karma_flower", true, new ConfigurableInfo("Whether or not reviving a creature will require a Karma Flower."));
            REVIVAL_HEALTH_FACTOR = config.Bind("revival_health_factor", 50, new ConfigurableInfo("How much of a creature's health will be restored upon revival. For Slugcats, this is always 100."));
        }

        public override void Initialize()
        {
            Debug.Log("ASConfig Initialize");
            base.Initialize();

            Tabs = new OpTab[] {
                new OpTab(this, "Main Options"),
            };

            Tabs[0].AddItems(
                new UIelement[] {
                    new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), AscendedSaintMain.PLUGIN_NAME, FLabelAlignment.Center, true),
                    new OpLabel(new Vector2(200f, 505f), new Vector2(200f, 15f), "v" + AscendedSaintMain.PLUGIN_VERSION, FLabelAlignment.Center, false)
                    {
                        color = Color.gray
                    }
                }
            );

            AddCheckBoxOption(Tabs[0], "Allow Self-Ascension", ALLOW_SELF_ASCENSION);
            AddCheckBoxOption(Tabs[0], "Allow Creature Revival", ALLOW_REVIVAL);
            AddCheckBoxOption(Tabs[0], "Require Karma Flower", REQUIRE_KARMA_FLOWER);

            AddSliderOption(Tabs[0], "Revival Health Factor", REVIVAL_HEALTH_FACTOR);
        }

        public override void Update()
        {
            base.Update();
        }

        private void AddCheckBoxOption(OpTab opTab, string text, Configurable<bool> configurable)
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
        }

        private void AddSliderOption(OpTab opTab, string text, Configurable<int> configurable, float multi = 1f, bool vertical = false)
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
        }
    }
}