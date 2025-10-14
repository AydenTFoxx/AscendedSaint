using System.Reflection;
using BepInEx;

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
    protected new LogUtils.Logger Logger { get; set; }

    /// <summary>
    ///     Creates a new ModPlugin instance with no REMIX option interface.
    /// </summary>
    public ModPlugin() : this(null)
    {
    }

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    public ModPlugin(OptionInterface? options)
    {
        this.options = options;

        Registry.RegisterMod(this, options?.GetType());

        Logger = Registry.GetModLogger(Assembly.GetCallingAssembly());
    }

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