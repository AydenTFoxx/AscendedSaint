using System;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using LogUtils.Enums;
using ModLib.Meadow;
using ModLib.Options;
using UnityEngine;

namespace ModLib;

internal static class Core
{
    public const string MOD_GUID = "ynhzrfxn.modlib";
    public const string MOD_NAME = "ModLib";
    public const string MOD_VERSION = "1.0.0.0";

    public static readonly BepInPlugin PluginData = new(MOD_GUID, MOD_NAME, MOD_VERSION);
    public static readonly ManualLogSource LogSource = BepInEx.Logging.Logger.CreateLogSource(MOD_NAME);

    public static readonly string LogsPath = Path.Combine(Application.streamingAssetsPath, "Logs");

    public static readonly LogID MyLogID = new("ModLib.log", LogsPath, LogAccess.FullAccess, true);
    public static readonly LogUtils.Logger Logger = new(MyLogID, LogID.BepInEx) { LogSource = LogSource };

    private static bool _initialized;

    private static readonly WeakReference<GameSession> LastGameSession = new(null!);

    static Core()
    {
        if (!Directory.Exists(LogsPath))
        {
            Directory.CreateDirectory(LogsPath);
        }

        MyLogID.Properties.ReadOnly = false;

        if (!MyLogID.Properties.ReadOnly)
        {
            MyLogID.Properties.AltFilename = new LogUtils.LogFilename("ynhzrfxn.modlib", ".log");

            MyLogID.Properties.ShowCategories.IsEnabled = true;
            MyLogID.Properties.ShowLogTimestamp.IsEnabled = true;

            MyLogID.Properties.IntroMessage = $"# Initialized ModLib successfully.";
            MyLogID.Properties.OutroMessage = $"# Disabled ModLib successfully.";

            MyLogID.Properties.AddTag("ModLib");
        }
    }

    public static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        CompatibilityManager.CheckModCompats();

        Extras.IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        Extras.IsIICEnabled = CompatibilityManager.IsIICEnabled();

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }
        else
        {
            On.GameSession.ctor += GameSessionHook;
        }

        PatchLoader.Initialize();
    }

    public static void Disable()
    {
        if (!_initialized) return;

        _initialized = false;

        CompatibilityManager.Clear();

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.RemoveHooks();
        }
        else
        {
            On.GameSession.ctor -= GameSessionHook;
        }
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (!LastGameSession.TryGetTarget(out _))
        {
            OptionUtils.SharedOptions.RefreshOptions();

            LastGameSession.SetTarget(self);
        }
    }

    private static class PatchLoader
    {
        private const string TARGET_DLL = "ModLib.Loader.dll";

        private static readonly Version _latestLoaderVersion = new("1.0.0.7");

        private static readonly string _targetPath = Path.Combine(Paths.PatcherPluginPath, TARGET_DLL);

        private static bool _loaderIsUpToDate;

        public static void Initialize()
        {
            // Non-whitelisted DLLs are sent to a backup/ folder by MultiFolderLoader
            // When updating ModLib's patcher, we simply un-whitelist it so it's moved elsewhere;
            // ModLib can then deploy the latest version while safely removing the old assembly.
            string backupPath = Path.Combine(Paths.BepInExRootPath, "backup", TARGET_DLL);

            if (File.Exists(backupPath) && File.Exists(_targetPath))
            {
                Version oldVersion = AssemblyName.GetAssemblyName(backupPath).Version;

                Logger.LogDebug($"Patcher update successful. (Previous: {oldVersion}; Current: {_latestLoaderVersion})");

                File.Delete(backupPath);

                _loaderIsUpToDate = true;
            }

            if (!_loaderIsUpToDate)
            {
                DeployVersionLoader();
            }
        }

        private static void DeployVersionLoader()
        {
            Version? localVersion = null;

            if (File.Exists(_targetPath))
            {
                localVersion = AssemblyName.GetAssemblyName(_targetPath).Version;

                if (localVersion >= _latestLoaderVersion)
                {
                    Logger.LogDebug($"Local ModLib patcher is up to date, skipping deploy action. ({localVersion} vs {_latestLoaderVersion})");

                    _loaderIsUpToDate = true;
                    return;
                }
            }

            if (!File.Exists(_targetPath))
            {
                Logger.LogInfo("Deploying new ModLib.Loader assembly to the game.");

                using Stream stream = typeof(Registry).Assembly.GetManifestResourceStream("ModLib.ModLib.Loader.dll");

                byte[] block = new byte[stream.Length];
                stream.Read(block, 0, block.Length);

                WriteAssemblyFile(_targetPath, block);

                WhitelistPatcher();
            }
            else
            {
                Logger.LogInfo($"Removing ModLib.Loader patcher from whitelist for update ({localVersion} -> {_latestLoaderVersion}).");

                RemoveFromWhitelist();
            }

            _loaderIsUpToDate = true;
        }

        public static void RemoveFromWhitelist()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "whitelist.txt");

            StringBuilder whitelistBuilder = new();

            using StreamReader reader = File.OpenText(path);

            while (!reader.EndOfStream)
            {
                string entry = reader.ReadLine().ToLowerInvariant();

                if (entry != "modlib.loader.dll")
                {
                    whitelistBuilder.AppendLine(entry);
                }
            }

            reader.Close();

            File.WriteAllText(path, whitelistBuilder.ToString());
        }

        private static void WhitelistPatcher()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "whitelist.txt");

            using StreamReader reader = File.OpenText(path);

            while (!reader.EndOfStream)
            {
                string entry = reader.ReadLine().ToLowerInvariant();

                if (entry == "modlib.loader.dll")
                {
                    Logger.LogDebug("ModLib.Loader is already whitelisted, skipping action.");
                    return;
                }
            }

            reader.Close();

            try
            {
                File.AppendAllText(path, "modlib.loader.dll" + Environment.NewLine);

                Logger.LogInfo("Added ModLib.Loader to the game's whitelist.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to whitelist patcher assembly! {ex}");
            }
        }

        private static void WriteAssemblyFile(string path, byte[] assemblyData)
        {
            try
            {
                File.WriteAllBytes(path, assemblyData);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to write data to {path}: {ex}");
            }
        }
    }
}