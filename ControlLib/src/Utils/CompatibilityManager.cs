using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ControlLib.Utils;

/// <summary>
/// Simple helper for determining the presence of other mods and ensure mod compatibility.
/// </summary>
public static class CompatibilityManager
{
    private static string[][] SupportedModIDs { get; } = [
        ["henpemaz_rainmeadow", "3388224007"],  // Rain Meadow
        ["improved-input-config", "3458119961"] // Improved Input Config: Extended
    ];
    private static Dictionary<string, bool> ManagedMods { get; } = [];

    /// <summary>
    /// Queries the client's list of enabled mods for toggling compatibility features.
    /// </summary>
    public static void CheckModCompats()
    {
        CLLogger.LogDebug("Checking compatibility mods...");

        try
        {
            StreamReader reader = new(Path.Combine(Application.streamingAssetsPath, "enabledMods.txt"));

            while (!reader.EndOfStream)
            {
                string modID = reader.ReadLine();

                if (modID.StartsWith("[WORKSHOP]"))
                    modID = modID.Split('\\').Last();

                foreach (string[] supportedIDs in SupportedModIDs)
                {
                    if (supportedIDs.Contains(modID))
                    {
                        modID = supportedIDs[0];

                        CLLogger.LogInfo($"Adding compatibility layer for: {modID}");

                        ManagedMods.Add(modID, true);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            CLLogger.LogError("Failed to read enabled mods file!", ex);
        }
    }

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
}