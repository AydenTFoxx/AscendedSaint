using System.Reflection;
using BepInEx;
using ModLib.Meadow;

namespace ModLib;

public class ModPlugin(OptionInterface options) : BaseUnityPlugin
{
    public bool IsModEnabled { get; protected set; }

    protected bool IsMeadowEnabled { get; set; }

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

        Logger.LogInfo($"Enabled {Assembly.GetCallingAssembly().GetModName()} successfully.");
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

        Logger.LogInfo($"Disabled {Assembly.GetCallingAssembly().GetModName()} successfully.");
    }


    // Load any resources, such as sprites or sounds
    protected virtual void LoadResources() =>
        MachineConnector.SetRegisteredOI(Assembly.GetCallingAssembly().GetModId(), options);


    protected virtual void ApplyHooks()
    {
        On.RainWorld.OnModsInit += OnModsInitHook;

        On.RainWorld.Update += UpdateHook;

        On.GameSession.ctor += Extras.GameSessionHook;
        On.GameSession.AddPlayer += Extras.AddPlayerHook;
    }

    protected virtual void RemoveHooks()
    {
        On.RainWorld.OnModsInit -= OnModsInitHook;

        On.RainWorld.Update -= UpdateHook;

        On.GameSession.ctor -= Extras.GameSessionHook;
        On.GameSession.AddPlayer -= Extras.AddPlayerHook;
    }

    protected virtual void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        Extras.WrapAction(LoadResources);
    }

    protected virtual void UpdateHook(On.RainWorld.orig_Update orig, RainWorld self)
    {
        orig.Invoke(self);

        if (IsMeadowEnabled)
        {
            ModRPCManager.UpdateRPCs();
        }
    }
}