using System.Collections.Generic;
using System.Linq;

namespace ControlLib.Utils;

/// <summary>
/// Simple helper for determining the presence of other mods and ensure mod compatibility.
/// </summary>
public static class CompatibilityManager
{
    private static string[] SupportedModIDs { get; } = ["henpemaz_rainmeadow", "improved-input-config"];
    private static Dictionary<string, bool> ManagedMods { get; } = [];

    /// <summary>
    /// Enables the manager's hooks for ensuring mod compatibility.
    /// </summary>
    public static void ApplyHooks() => On.RainWorld.PreModsInit += PreModsInitHook;

    /// <summary>
    /// Disables the manager's hooks for mod compatibility.
    /// </summary>
    public static void RemoveHooks() => On.RainWorld.PreModsInit -= PreModsInitHook;

    /// <summary>
    /// Determines if a given mod is currently enabled.
    /// </summary>
    /// <param name="modID">The ID of the mod to check for.</param>
    /// <returns><c>true</c> if the given mod was found to be enabled, <c>false</c> otherwise.</returns>
    public static bool IsModEnabled(string modID) =>
        ManagedMods.TryGetValue(modID, out bool value)
            ? value
            : ModManager.ActiveMods.Any(mod => mod.id == modID);

    /// <summary>
    /// Determines if either Improved Input Config or Improved Input Config: Extended are enabled.
    /// </summary>
    /// <returns><c>true</c> if one of these mods is enabled, <c>false</c> otherwise.</returns>
    public static bool IsIICEnabled() => IsModEnabled("improved-input-config");

    /// <summary>
    /// Determines if the Rain Meadow mod is enabled.
    /// </summary>
    /// <returns><c>true</c> if the mod is enabled, <c>false</c> otherwise.</returns>
    public static bool IsRainMeadowEnabled() => IsModEnabled("henpemaz_rainmeadow");

    /// <summary>
    /// Overrides the configured compatibility features for a given mod.
    /// </summary>
    /// <param name="modID">The identifier of the mod.</param>
    /// <param name="value">The value to be set.</param>
    /// <remarks>Warning: Once disabled, a compatibility layer will not be re-enabled until a restart. Use with care.</remarks>
    public static void SetModCompatibility(string modID, bool value = true) => ManagedMods[modID] = value;

    /// <summary>
    /// Queries the client's list of enabled mods for toggling compatibility features.
    /// </summary>
    private static void PreModsInitHook(On.RainWorld.orig_PreModsInit orig, RainWorld self)
    {
        orig.Invoke(self);

        CLLogger.LogDebug("Checking compatibility mods...");

        foreach (string modID in SupportedModIDs)
        {
            if (self.options.enabledMods.Contains(modID))
            {
                CLLogger.LogInfo($"Adding compatibility layer for {modID}.");

                ManagedMods.Add(modID, true);
            }
        }
    }
}