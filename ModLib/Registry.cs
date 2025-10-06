using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;

namespace ModLib;

public static class Registry
{
    private static readonly Dictionary<Assembly, ModMetadata> RegisteredMods = [];

    /// <summary>
    /// Registers the current mod assembly to ModLib. This should be done sometime during the mod-loading process,
    /// typically from the <c>Main</c>/<c>Plugin</c> class constructor, <c>Awake()</c> or <c>OnEnable()</c> methods.
    /// </summary>
    /// <param name="plugin">The <c>Plugin</c> class from which this mod is being registered.</param>
    /// <param name="optionHolder">
    ///     A class with <c>public static</c> fields of type <see cref="Configurable{T}"/>,
    ///     which are retrieved via reflection to determine the mod's REMIX options.
    /// </param>
    public static void RegisterMod(BaseUnityPlugin plugin, Type? optionHolder) =>
        RegisterMod(plugin.Info.Metadata, optionHolder);

    /// <inheritdoc cref="RegisterMod"/>
    /// <param name="metadata">The metadata of the Plugin class for registry.</param>
    public static void RegisterMod(BepInPlugin metadata, Type? optionHolder) =>
        RegisteredMods.Add(Assembly.GetCallingAssembly(), new ModMetadata(metadata, optionHolder));

    /// <summary>
    /// Removes the current mod assembly from ModLib's registry.
    /// </summary>
    /// <returns><c>true</c> if the mod was successfully unregistered, <c>false</c> otherwise (e.g. if it was not registered at all).</returns>
    public static bool UnregisterMod() => RegisteredMods.Remove(Assembly.GetCallingAssembly());

    internal static string GetModId(this Assembly assembly) =>
        RegisteredMods.TryGetValue(assembly, out ModMetadata metadata)
            ? metadata.ModId
            : "unknown";

    internal static string GetModName(this Assembly assembly) =>
        RegisteredMods.TryGetValue(assembly, out ModMetadata metadata)
            ? metadata.ModName
            : "Unknown Mod";

    internal static Version GetModVersion(this Assembly assembly) =>
        RegisteredMods.TryGetValue(assembly, out ModMetadata metadata)
            ? metadata.ModVersion
            : new Version(0, 0);

    internal static Type? GetOptionHolder(this Assembly assembly) =>
        RegisteredMods.TryGetValue(assembly, out ModMetadata metadata)
            ? metadata.OptionHolder
            : null;

    private sealed record ModMetadata
    {
        public string ModId { get; }
        public string ModName { get; }
        public Version ModVersion { get; }

        public Type? OptionHolder { get; }
        public LogUtils.Logger? Logger { get; }

        public ModMetadata(BaseUnityPlugin plugin, Type? optionHolder, bool createLogger = true)
            : this(plugin.Info.Metadata, optionHolder, createLogger)
        {
        }

        public ModMetadata(BepInPlugin metadata, Type? optionHolder, bool createLogger = true)
        {
            ModId = metadata.GUID;
            ModName = metadata.Name;
            ModVersion = metadata.Version;

            OptionHolder = optionHolder;

            if (createLogger)
            {
                Logger = new();
            }
        }
    }
}