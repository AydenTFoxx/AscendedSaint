using ControlLib.Utils;
using Menu.Remix.MixedUI;

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

        /// <summary>
        /// Sets all of the client's settings to those from the REMIX options menu.
        /// </summary>
        public void RefreshOptions()
        {
            selectionMode = SELECTION_MODE!.Value;
            invertControls = INVERT_CONTROLS!.Value;
        }

        /// <summary>
        /// Sets the client's options to those from the given <c>ClientOptions</c> instance.
        /// </summary>
        /// <param name="options">The <c>ClientOptions</c> instance whose values will be copied.</param>
        public void SetOptions(ClientOptions options)
        {
            selectionMode = options.selectionMode;
            invertControls = options.invertControls;
        }
    }

    public static Configurable<string>? SELECTION_MODE;
    public static Configurable<bool>? INVERT_CONTROLS;

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
        INVERT_CONTROLS = config.Bind(
            "invert_controls",
            false,
            new ConfigurableInfo(
                "(Classic Mode only) Inverts the controls used for selecting creatures in the Possession ability."
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
            .AddCheckBoxOption("Invert Controls", INVERT_CONTROLS!)
            .Build();
    }

    public override void Update() => base.Update();
}