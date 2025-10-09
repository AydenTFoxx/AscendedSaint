using System.Reflection;
using BepInEx;
using ModLib.Meadow;

namespace ModLib;

public class ModPlugin : BaseUnityPlugin
{
    private readonly OptionInterface options;

    protected bool IsModEnabled { get; set; }
    protected bool IsMeadowEnabled { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    internal static Assembly Assembly { get; set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public ModPlugin(OptionInterface options)
    {
        this.options = options;

        Assembly = options.GetType().Assembly;
    }

    public virtual void OnEnable()
    {
        if (IsModEnabled) return;
        IsModEnabled = true;

        CompatibilityManager.CheckModCompats();

        IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();

        Extras.WrapAction(() =>
        {
            ApplyHooks();

            ModLib.Logger.LogDebug("Successfully registered hooks to the game.");
        });

        Logger.LogInfo($"Enabled {Assembly.GetModName()} successfully.");
    }

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


    // Load any resources, such as sprites or sounds
    protected virtual void LoadResources() =>
        MachineConnector.SetRegisteredOI(Assembly.GetModId(), options);


    protected virtual void ApplyHooks()
    {
        On.RainWorld.OnModsInit += OnModsInitHook;

        On.RainWorldGame.Update += GameUpdateHook;

        On.GameSession.ctor += Extras.GameSessionHook;
        On.GameSession.AddPlayer += Extras.AddPlayerHook;
    }

    protected virtual void RemoveHooks()
    {
        On.RainWorld.OnModsInit -= OnModsInitHook;

        On.RainWorldGame.Update -= GameUpdateHook;

        On.GameSession.ctor -= Extras.GameSessionHook;
        On.GameSession.AddPlayer -= Extras.AddPlayerHook;
    }

    protected virtual void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        Extras.WrapAction(LoadResources);
    }

    protected virtual void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        if (IsMeadowEnabled && MeadowUtils.IsOnline)
        {
            ModRPCManager.UpdateRPCs();
        }
    }
}