using System;
using System.IO;
using System.Reflection;
using BepInEx;
using LogUtils.Enums;
using ModLib.Meadow;
using UnityEngine;

namespace ModLib;

internal static class Core
{
    public const string MOD_GUID = "ynhzrfxn.modlib";
    public const string MOD_NAME = "ModLib";
    public const string MOD_VERSION = "1.0.0.0";

    public static readonly BepInPlugin PluginData = new(MOD_GUID, MOD_NAME, MOD_VERSION);

    public static readonly LogID MyLogID = new(Path.Combine("Logs", "ModLib.log"), LogAccess.FullAccess, true);
    public static readonly LogUtils.Logger Logger = new ModLogger(PluginData, MyLogID);

    private static bool _initialized;
    private static bool _loaderIsUpToDate;

    private static readonly Version _latestLoaderVersion = new("1.0.0.4");

    public static void Initialize()
    {
        if (_initialized) return;

        _initialized = true;

        CompatibilityManager.CheckModCompats();

        Extras.IsMeadowEnabled = CompatibilityManager.IsRainMeadowEnabled();
        Extras.IsIICEnabled = CompatibilityManager.IsIICEnabled();

        On.GameSession.ctor += Extras.GameSessionHook;

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.AddHooks();
        }

        if (!_loaderIsUpToDate)
        {
            DeployVersionLoader();
        }
    }

    public static void Dispose()
    {
        if (!_initialized) return;
        _initialized = false;

        CompatibilityManager.Clear();

        On.GameSession.ctor -= Extras.GameSessionHook;

        if (Extras.IsMeadowEnabled)
        {
            MeadowHooks.RemoveHooks();
        }
    }

    private static void DeployVersionLoader()
    {
        string targetPath = Path.Combine(Paths.PatcherPluginPath, "ModLib.Loader.dll");

        Version? localVersion = null;

        if (File.Exists(targetPath))
        {
            localVersion = AssemblyName.GetAssemblyName(targetPath).Version;

            if (localVersion >= _latestLoaderVersion)
            {
                Logger.LogDebug($"Local ModLib patcher is up to date, skipping deploy action. ({localVersion} vs {_latestLoaderVersion})");

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
            Logger.LogInfo($"Updating local ModLib.Loader patcher from {localVersion} to {_latestLoaderVersion}.");
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