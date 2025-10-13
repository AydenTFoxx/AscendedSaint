using System.Reflection;
using BepInEx;
using ModLib.Meadow;

namespace ModLib;

/// <summary>
///     A BaseUnityPlugin with extra features out of the box.
/// </summary>
/// <remarks>
///     For the best experience, it is highly recommended to extend this class for your mod's entrypoint class.
/// </remarks>
public class ModPlugin : BaseUnityPlugin
{
    private readonly OptionInterface? options;

    /// <summary>
    ///     Determines if this mod has successfuly been enabled.
    /// </summary>
    protected bool IsModEnabled { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    internal static Assembly Assembly { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    /// <summary>
    ///     Creates a new ModPlugin instance with the provided REMIX option interface.
    /// </summary>
    /// <param name="options">The mod's REMIX option interface class, if any.</param>
    public ModPlugin(OptionInterface options)
    {
        this.options = options;

        Assembly = options.GetType().Assembly;
    }

    /// <summary>
    ///     Initializes compatibility checks and applies hooks to the game.
    ///     Override this to add behavior which should only occur once, while your mod is being loaded by the game.
    /// </summary>
    public virtual void OnEnable()
    {
        if (IsModEnabled) return;
        IsModEnabled = true;

        Extras.WrapAction(() =>
        {
            ApplyHooks();

            ModLib.Logger.LogDebug("Successfully registered hooks to the game.");
        });

        Logger.LogInfo($"Enabled {Assembly.GetModName()} successfully.");
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

            ModLib.Logger.LogDebug("Removed all hooks successfully.");
        });

        Logger.LogInfo($"Disabled {Assembly.GetModName()} successfully.");
    }

    /// <summary>
    ///     Load any resources, such as sprites or sounds. This also registers the mod's REMIX interface to the game.
    /// </summary>
    protected virtual void LoadResources()
    {
        if (options is not null)
        {
            MachineConnector.SetRegisteredOI(Assembly.GetModId(), options);
        }
    }

    /// <summary>
    ///     Applies ModLib's base hooks to the game. Override this to add your own hooks as well.
    /// </summary>
    protected virtual void ApplyHooks()
    {
        On.RainWorld.OnModsInit += OnModsInitHook;

        On.RainWorldGame.Update += GameUpdateHook;

        On.GameSession.ctor += Extras.GameSessionHook;

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }
    }

    /// <summary>
    ///     Removes ModLib's base hooks from the game. Override this to remove your own hooks as well.
    /// </summary>
    protected virtual void RemoveHooks()
    {
        On.RainWorld.OnModsInit -= OnModsInitHook;

        On.RainWorldGame.Update -= GameUpdateHook;

        On.GameSession.ctor -= Extras.GameSessionHook;

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.RemoveHooks();
        }
    }

    /// <summary>
    ///     Loads this mod's resources to the game.
    ///     Override this to add any extra behavior which must be run once all mods have been loaded into the game.
    /// </summary>
    protected virtual void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        Extras.WrapAction(LoadResources);
    }

    /// <summary>
    ///     Updates mod classes which must be regularly ticked.
    ///     Override this to add behavior which runs on every game tick.
    /// </summary>
    protected virtual void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        if (Extras.IsOnlineSession)
        {
            ModRPCManager.UpdateRPCs();
        }
    }
}