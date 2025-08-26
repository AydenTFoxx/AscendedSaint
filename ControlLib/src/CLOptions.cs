using ControlLib.Utils;
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

        /// <summary>
        /// Sets all of the client's settings to those from the REMIX options menu.
        /// </summary>
        public void RefreshOptions()
        {
            selectionMode = SELECTION_MODE!.Value;
            invertControls = INVERT_CONTROLS!.Value;
            meadowSlowdown = MEADOW_SLOWDOWN!.Value;
        }

        /// <summary>
        /// Sets sync-requiring options of the client to those from the given instance.
        /// </summary>
        /// <param name="options">The <c>ClientOptions</c> instance whose values will be copied.</param>
        public void SetSyncedOptions(ClientOptions options) => meadowSlowdown = options.meadowSlowdown;

        public string FormatOptions() => $"SM: {selectionMode}; IC: {invertControls} | MS: {meadowSlowdown}";

        public override string ToString() => $"{nameof(ClientOptions)} => [{FormatOptions()}]";
    }

    public static Configurable<string>? SELECTION_MODE;
    public static Configurable<bool>? INVERT_CONTROLS;
    public static Configurable<bool>? MEADOW_SLOWDOWN;

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
                "If Rain Meadow mod is present, whether or not using the Possession ability will slow down time."
            )
        );
    }

    public override void Initialize()
    {
        CLLogger.LogInfo($"{nameof(CLOptions)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[1];

        Tabs[0] = new OptionBuilder(this, "Possession")
            .AddComboBoxOption("Selection Mode", SELECTION_MODE!, width: 120)
            .AddPadding(new Vector2(0f, 10f))
            .AddCheckBoxOption("Invert Controls", INVERT_CONTROLS!)
            .AddCheckBoxOption("Meadow Slowdown", MEADOW_SLOWDOWN!)
            .Build();
    }

    public override void Update() => base.Update();
}