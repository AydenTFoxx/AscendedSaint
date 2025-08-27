using System.Linq;
using System.Reflection;
using System.Text;
using ControlLib.Utils;
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
        public string selectionMode = SELECTION_MODE!.Value;
        public bool invertControls = INVERT_CONTROLS!.Value;
        public bool meadowSlowdown = MEADOW_SLOWDOWN!.Value;
        public bool infinitePossession = INFINITE_POSSESSION!.Value;
        public bool possessAncestors = POSSESS_ANCESTORS!.Value;
        public bool forceMultitargetPossession = FORCE_MULTITARGET_POSSESSION!.Value;
        public bool worldwideMindControl = WORLDWIDE_MIND_CONTROL!.Value;

        public ClientOptions()
        {
            CLLogger.LogDebug($"Client options are: {this}");
        }

        /// <summary>
        /// Sets sync-requiring options of the client to those from the given instance.
        /// </summary>
        /// <param name="options">The <c>ClientOptions</c> instance whose values will be copied.</param>
        public void SetSyncedOptions(ClientOptions options)
        {
            meadowSlowdown = options.meadowSlowdown;
            infinitePossession = options.infinitePossession;
            possessAncestors = options.possessAncestors;
            forceMultitargetPossession = options.forceMultitargetPossession;
            worldwideMindControl = options.worldwideMindControl;
        }

        public override string ToString() => $"{nameof(ClientOptions)} => [{FormatOptions()}]";

        protected string FormatOptions()
        {
            StringBuilder stringBuilder = new();

            foreach (FieldInfo? field in typeof(ClientOptions).GetFields())
            {
                if (field is null) continue;

                stringBuilder.Append($"{GetOptionAcronym(field.Name)}: {field.GetValue(this)}; ");
            }

            return stringBuilder.ToString().Trim();
        }

        private string GetOptionAcronym(string optionName)
        {
            StringBuilder stringBuilder = new();

            stringBuilder.Append(optionName.First());

            foreach (char c in optionName.Where(c => char.IsUpper(c)))
            {
                stringBuilder.Append(c);
            }

            return stringBuilder.ToString().ToUpperInvariant();
        }
    }

    public static Configurable<string>? SELECTION_MODE;
    public static Configurable<bool>? INVERT_CONTROLS;
    public static Configurable<bool>? MEADOW_SLOWDOWN;
    public static Configurable<bool>? INFINITE_POSSESSION;
    public static Configurable<bool>? POSSESS_ANCESTORS;
    public static Configurable<bool>? FORCE_MULTITARGET_POSSESSION;
    public static Configurable<bool>? WORLDWIDE_MIND_CONTROL;

    public CLOptions()
    {
        SELECTION_MODE = config.Bind(
            "selection_mode",
            "classic",
            new ConfigurableInfo(
                "Which mode to use for selecting creatures to possess. Classic is list-based; Ascension is akin to Saint's ascension ability.",
                new ConfigAcceptableList<string>("classic", "ascension")
            )
        );
        INVERT_CONTROLS = config.Bind(
            "invert_controls",
            false,
            new ConfigurableInfo(
                "(Classic Mode only) Inverts the controls used for selecting creatures in the Possession ability."
            )
        );
        MEADOW_SLOWDOWN = config.Bind(
            "meadow_slowdown",
            false,
            new ConfigurableInfo(
                "(Requires Rain Meadow) Whether or not using the Possession ability will slow down time."
            )
        );
        INFINITE_POSSESSION = config.Bind(
            "infinite_possession",
            false,
            new ConfigurableInfo(
                "Allows indefinite possession of creatures. Also prevents Inv from exploding."
            )
        );
        POSSESS_ANCESTORS = config.Bind(
            "possess_ancestors",
            false,
            new ConfigurableInfo(
                "If enabled, multi-target possessions will also target anscestors, e.g. \"White Lizard\" will target all lizard types."
            )
        );
        FORCE_MULTITARGET_POSSESSION = config.Bind(
            "force_multitarget_possession",
            false,
            new ConfigurableInfo(
                "If enabled, possessions will by default target all creatures of that same type; Saint's Ascended Possession will only target one creature at a time instead."
            )
        );
        WORLDWIDE_MIND_CONTROL = config.Bind(
            "worldwide_mind_control",
            false,
            new ConfigurableInfo(
                "The Hive Mind must consume all things, living or otherwise."
            )
        );
    }

    public override void Initialize()
    {
        CLLogger.LogInfo($"{nameof(CLOptions)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[3];

        Tabs[0] = new OptionBuilder(this, "Main Options")
            .AddComboBoxOption("Selection Mode", SELECTION_MODE!, width: 120)
            .AddPadding(Vector2.up * 10)
            .AddCheckBoxOption("Invert Controls", INVERT_CONTROLS!)
            .Build();

        Tabs[1] = new OptionBuilder(this, "Compatibility")
            .AddPadding(Vector2.down * 10)
            .AddText("These options are only applied when their respective mods are enabled.", new Vector2(120f, 24f))
            .AddPadding(Vector2.up * 30)
            .AddCheckBoxOption("Meadow Slowdown", MEADOW_SLOWDOWN!)
            .Build();

        Tabs[2] = new OptionBuilder(this, "Cheats", MenuColorEffect.rgbDarkRed)
            .AddPadding(Vector2.down * 10)
            .AddText("These options are for testing purposes only. Use at your own risk.", new Vector2(100f, 24f))
            .AddPadding(Vector2.up * 30)
            .AddCheckBoxOption("Infinite Possession", INFINITE_POSSESSION!)
            .AddCheckBoxOption("Possess Anscestors", POSSESS_ANCESTORS!)
            .AddCheckBoxOption("Force Multi-Target Possession", FORCE_MULTITARGET_POSSESSION!)
            .AddPadding(Vector2.up * 20)
            .AddCheckBoxOption("Worldwide Mind Control", WORLDWIDE_MIND_CONTROL!, default, MenuColorEffect.rgbDarkRed)
            .Build();
    }

    public override void Update() => base.Update();
}