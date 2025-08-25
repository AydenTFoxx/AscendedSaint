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
    public const string PLUGIN_VERSION = "0.2.0";

    public static CLOptions.ClientOptions? ClientOptions { get; private set; }

    private bool isInitialized;
    private readonly CLOptions options;

    public ControlLibMain()
        : base()
    {
        CLLogger.CleanLogFile();

        options = new();
        ClientOptions = new();
    }

    public void OnEnable()
    {
        if (isInitialized) return;
        isInitialized = true;

        ApplyCLHooks();

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
            PossessionHooks.ApplyHooks();

            On.GameSession.ctor += GameSessionHook;
            On.RainWorld.OnModsInit += OnModsInitHook;
            On.RainWorld.PostModsInit += PostModsInitHook;
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
            PossessionHooks.RemoveHooks();

            On.GameSession.ctor -= GameSessionHook;
            On.RainWorld.OnModsInit -= OnModsInitHook;
            On.RainWorld.PostModsInit -= PostModsInitHook;
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to remove hooks!", ex);
        }
    }

    private void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        ClientOptions?.RefreshOptions();
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

    private void PostModsInitHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        InputHandler.Keys.InitKeybinds();
    }
}