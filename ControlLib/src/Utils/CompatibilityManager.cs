using System.Linq;

namespace ControlLib.Utils;

public static class CompatibilityManager
{
    public static bool IsModEnabled(string modID) => ModManager.ActiveMods.Any(mod => mod.id == modID);

    public static bool IsIICEnabled() => IsModEnabled("improved-input-config");
    public static bool IsRainMeadowEnabled() => IsModEnabled("henpemaz_rainmeadow");
}