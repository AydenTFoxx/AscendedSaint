using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.MultiFolderLoader;
using Mono.Cecil;
using AssemblyCandidate = (System.Version Version, string Path);

// Credits to LogUtils by Fluffball (@TheVileOne) for original LogUtils.VersionLoader code

namespace ModLib.Loader;

public static class Patcher

{
    internal static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("ModLib.Loader");

    public static IEnumerable<string> TargetDLLs => GetDLLs();

    public static IEnumerable<string> GetDLLs()
    {
        yield return "BepInEx.MultiFolderLoader.dll";

        Logger.LogMessage($"Looking for ModLib assemblies...");

        AssemblyCandidate target = AssemblyUtils.FindLatestAssembly(GetSearchPaths(), "ModLib.dll");

        if (target.Path != null)
        {
            Logger.LogMessage($"Loading latest ModLib DLL: {FormatPath(target.Path)}");
            Assembly.LoadFrom(target.Path);
        }
        else
        {
            Logger.LogInfo("No ModLib assembly found.");
        }

        static string FormatPath(string path)
        {
            string? result = path.Split(["mods", "312520"], StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

            return string.IsNullOrWhiteSpace(result)
                ? string.Empty
                : result.Remove(0, 1);
        }
    }

    private static IEnumerable<string> GetSearchPaths()
    {
        foreach (Mod mod in ModManager.Mods)
        {
            yield return mod.PluginsPath;

            //Check the mod's root directory only if we did not find results in the current plugin directory
            if (!AssemblyUtils.LastFoundAssembly.HasPath(mod.PluginsPath))
                yield return mod.ModDir;
        }
    }

    public static void Patch(AssemblyDefinition _)
    {
    }
}