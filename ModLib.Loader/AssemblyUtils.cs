using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AssemblyCandidate = (System.Version Version, string Path);

// Credits to LogUtils by Fluffball (@TheVileOne) for original LogUtils.VersionLoader code

namespace ModLib.Loader;

internal static class AssemblyUtils
{
    private static readonly string[] RootPaths =
    [
        Path.Combine("RainWorld_Data", "StreamingAssets", "mods"), // RainWorld_Data\StreamingAssets\mods\
        Path.Combine("steamapps", "workshop", "content", "312520"), // steamapps\workshop\content\312520\
        Path.Combine("newest", "plugins"), // newest\plugins\
        "plugins" // plugins\
    ];

    public static AssemblyCandidate LastFoundAssembly;

    /// <summary>
    ///     Searches the specified directory path (and any subdirectories) for an assembly, and returns the first match
    /// </summary>
    public static string FindAssembly(string searchPath, string assemblyName) =>
        Directory.EnumerateFiles(searchPath, assemblyName, SearchOption.TopDirectoryOnly).FirstOrDefault();

    public static AssemblyCandidate FindLatestAssembly(IEnumerable<string> searchTargets, string assemblyName)
    {
        AssemblyCandidate target = default;
        foreach (string searchPath in searchTargets)
        {
            try
            {
                string targetPath = FindAssembly(searchPath, assemblyName);

                if (targetPath != null)
                {
                    Version version = AssemblyName.GetAssemblyName(targetPath).Version;

                    LastFoundAssembly = new AssemblyCandidate(version, targetPath);

                    Patcher.Logger.LogInfo($"Found candidate: {FormatCandidate(LastFoundAssembly)}");

                    if (target.Path == null || target.Version < version)
                        target = LastFoundAssembly;
                }
            }
            catch (IOException ex)
            {
                Patcher.Logger.LogError($"Error trying to access {searchPath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Patcher.Logger.LogError($"Error reading version from {searchPath}: {ex.Message}");
            }
        }
        return target;
    }

    public static bool HasPath(this AssemblyCandidate candidate, string path) =>
        candidate.Path != null && candidate.Path.StartsWith(path);

    public static string FormatCandidate(AssemblyCandidate candidate, bool includePathToAssembly = false) =>
        $"v{candidate.Version} from {GetModName(candidate.Path, includePathToAssembly)}";

    private static string? GetModName(string path, bool includePathToAssembly)
    {
        string? result = path.Split(includePathToAssembly ? ["mods", "312520"] : RootPaths, StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);

        return string.IsNullOrWhiteSpace(result)
                ? string.Empty
                : includePathToAssembly
                    ? result.Remove(0, 1)
                    : result.Replace(Path.DirectorySeparatorChar, ' ').Trim();
    }
}