using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace ModLib;

internal static class Core
{
    public const string MOD_NAME = "ModLib";
    public const string MOD_VERSION = "1.0.0.0";

    public static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModLib");

    private static bool _initialized;
    private static bool _loaderIsUpToDate;

    public static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        CompatibilityManager.CheckModCompats();

        Extras.IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        Extras.IsIICEnabled = CompatibilityManager.IsIICEnabled();

        if (!_loaderIsUpToDate)
        {
            DeployVersionLoader();
        }
    }

    private static void DeployVersionLoader()
    {
        string targetPath = Path.Combine(Paths.PatcherPluginPath, "ModLib.Loader.dll");

        Version currentVersion = typeof(Core).Assembly.GetName().Version; // For simplicity's sake, the Loader should always be the same version as ModLib itself
        Version? localVersion = null;

        if (File.Exists(targetPath))
        {
            localVersion = AssemblyName.GetAssemblyName(targetPath).Version;

            if (localVersion >= currentVersion)
            {
                Logger.LogDebug($"Local ModLib patcher is up to date, skipping deploy action. ({localVersion} vs {currentVersion})");

                _loaderIsUpToDate = true;
                return;
            }
        }

        using Stream stream = typeof(Registry).Assembly.GetManifestResourceStream("ModLib.ModLib.Loader.dll");

        byte[] block = new byte[stream.Length];
        stream.Read(block, 0, block.Length);

        if (!File.Exists(targetPath))
        {
            Logger.LogInfo("Deploying new ModLib.Loader assembly to the game.");
        }
        else
        {
            Logger.LogInfo($"Updating local ModLib.Loader patcher from {localVersion} to {currentVersion}.");
        }

        WriteAssemblyFile(targetPath, block);

        WhitelistPatcher();

        _loaderIsUpToDate = true;
    }

    private static void WhitelistPatcher()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "whitelist.txt");

        using StreamReader reader = File.OpenText(path);

        while (!reader.EndOfStream)
        {
            string entry = reader.ReadLine();

            if (entry == "modlib.loader.dll")
            {
                Logger.LogDebug("ModLib.Loader is already whitelisted, skipping action.");
                return;
            }
        }

        reader.Close();

        try
        {
            File.AppendAllText(path, "modlib.loader.dll");

            Logger.LogDebug("Added ModLib.Loader to the game's whitelist.");
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