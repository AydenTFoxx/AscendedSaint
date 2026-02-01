using System;
using System.Reflection;
using BepInEx;
using LogUtils;
using LogUtils.Enums;

namespace ModLib;

/// <summary>
///     A BaseUnityPlugin skeleton for quick prototyping and development.
/// </summary>
public abstract class ModPlugin : BaseUnityPlugin
{
    private readonly OptionInterface? options;

    /// <summary>
    ///     Determines if this mod has successfuly been enabled.
    /// </summary>
    protected bool IsModEnabled { get; set; }

    /// <summary>
    ///     The custom logger instance for this mod.
    /// </summary>
    protected new Logger Logger { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    ///     Creates a new ModPlugin instance with no REMIX option interface.
    /// </summary>
    public ModPlugin()
    {
        Initialize(Assembly.GetCallingAssembly(), null, null);
    }

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    public ModPlugin(OptionInterface? options)
    {
        this.options = options;

        Initialize(Assembly.GetCallingAssembly(), options?.GetType(), null);
    }

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface and logger instance.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    /// <param name="logger">The logger instance to be used. If null, a new one is created and assigned to this mod.</param>
    public ModPlugin(OptionInterface? options, Logger? logger)
    {
        this.options = options;

        Initialize(Assembly.GetCallingAssembly(), options?.GetType(), logger);
    }

    private void Initialize(Assembly caller, Type? optionHolder, Logger? logger)
    {
        if (logger is null)
        {
            LogID myLogID = Registry.ModEntry.CreateLogID(Info.Metadata);

            logger = new Logger(myLogID, LogID.Unity, LogID.BepInEx)
            {
                LogSource = base.Logger
            };
        }

        Registry.RegisterAssembly(caller, Info.Metadata, optionHolder, logger);

        Logger = logger;
    }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    ///     Applies hooks to the game, then marks the mod as enabled.
    ///     Override this to add behavior which should only occur once, while your mod is being loaded by the game.
    /// </summary>
    public virtual void OnEnable()
    {
        if (IsModEnabled) return;
        IsModEnabled = true;

        Extras.WrapAction(() =>
        {
            ApplyHooks();

            Logger.LogDebug("Successfully registered hooks to the game.");
        }, Logger);

        Logger.LogInfo($"Enabled {Info.Metadata.Name} successfully.");
    }

    /// <summary>
    ///     Removes hooks from the game, then marks the mod as disabled.
    ///     Override this to run behavior which should occur when your mod is disabled/reloaded by the game.
    /// </summary>
    /// <remarks>
    ///     This is most useful for Rain Reloader compatibility, but also seems to be called by the base game on exit.
    /// </remarks>
    public virtual void OnDisable()
    {
        if (!IsModEnabled) return;
        IsModEnabled = false;

        Extras.WrapAction(() =>
        {
            RemoveHooks();

            Logger.LogDebug("Removed all hooks successfully.");
        }, Logger);

        Logger.LogInfo($"Disabled {Info.Metadata.Name} successfully.");
    }

    /// <summary>
    ///     Load any resources, such as sprites or sounds. This also registers the mod's REMIX interface to the game.
    /// </summary>
    protected virtual void LoadResources()
    {
        if (options is not null)
        {
            MachineConnector.SetRegisteredOI(Info.Metadata.GUID, options);
        }
    }

    /// <summary>
    ///     Applies this mod's hooks to the game.
    /// </summary>
    protected virtual void ApplyHooks() => On.RainWorld.OnModsInit += OnModsInitHook;

    /// <summary>
    ///     Removes this mod's hooks from the game.
    /// </summary>
    protected virtual void RemoveHooks() => On.RainWorld.OnModsInit -= OnModsInitHook;

    /// <summary>
    ///     Loads this mod's resources to the game.
    ///     Override this to add any extra behavior which must be run once all mods have been loaded into the game.
    /// </summary>
    protected virtual void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        Extras.WrapAction(LoadResources);
    }
}