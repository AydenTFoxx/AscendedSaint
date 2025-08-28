using System;
using System.Security.Permissions;
using BepInEx;
using ControlLib.Possession;
using ControlLib.Utils;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace ControlLib;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class ControlLibMain : BaseUnityPlugin
{
    public const string PLUGIN_NAME = "ControlLib";
    public const string PLUGIN_GUID = "ynhzrfxn.controllib";
    public const string PLUGIN_VERSION = "0.4.0";

    private bool isInitialized;
    private readonly CLOptions options;

    public ControlLibMain()
        : base()
    {
        CLLogger.CleanLogFile();

        options = new();
    }

    public void OnEnable()
    {
        if (isInitialized) return;
        isInitialized = true;

        ApplyCLHooks();

        InputHandler.Keys.InitKeybinds();

        Logger.LogInfo("Enabled ControlLib successfully.");
    }

    public void OnDisable()
    {
        if (!isInitialized) return;
        isInitialized = false;

        RemoveCLHooks();

        Logger.LogInfo("Disabled ControlLib successfully.");
    }

    private void ApplyCLHooks()
    {
        try
        {
            CompatibilityManager.ApplyHooks();
            PossessionHooks.ApplyHooks();

            On.GameSession.ctor += GameSessionHook;
            On.GameSession.AddPlayer += AddPlayerHook;

            On.RainWorld.OnModsInit += OnModsInitHook;

            CLLogger.LogDebug("Successfully applied all hooks to the game.");
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to apply hooks!", ex);
        }
    }

    private void RemoveCLHooks()
    {
        try
        {
            CompatibilityManager.RemoveHooks();
            PossessionHooks.RemoveHooks();

            On.GameSession.ctor -= GameSessionHook;
            On.GameSession.AddPlayer -= AddPlayerHook;

            On.RainWorld.OnModsInit -= OnModsInitHook;

            CLLogger.LogDebug("Removed all hooks from the game.");
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to remove hooks!", ex);
        }
    }

    private void AddPlayerHook(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
    {
        orig.Invoke(self, player);

        bool isOnlineSession = CompatibilityManager.IsRainMeadowEnabled();

        if (self.game.Players.Count <= 1 && (!isOnlineSession || MeadowUtils.IsHost))
        {
            OptionUtils.SharedOptions.RefreshOptions(isOnlineSession);
        }
        else
        {
            CLLogger.LogDebug($"{self.game.FirstRealizedPlayer} is already realized, ignoring.");
        }
    }

    private void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost)
        {
            OptionUtils.SharedOptions.SetOptions(null);

            MeadowUtils.RequestOptionsSync();
        }
    }

    private void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        try
        {
            MachineConnector.SetRegisteredOI(PLUGIN_GUID, options);
        }
        catch (Exception ex)
        {
            CLLogger.LogError("Failed to apply REMIX settings!", ex);
        }
    }
}