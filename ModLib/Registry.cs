using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using ModLib.Options;

namespace ModLib;

/// <summary>
///     The entrypoint for registering mods to ModLib.
/// </summary>
public static class Registry
{
    private static readonly ConditionalWeakTable<Assembly, ModMetadata> RegisteredMods = new();

    static Registry()
    {
        Core.Initialize();

        RegisteredMods.Add(typeof(Registry).Assembly, new ModMetadata(Core.PluginData, null, Core.Logger));
    }

    /// <summary>
    ///     Registers the current mod assembly to ModLib. This should be done sometime during the mod-loading process,
    ///     typically from the <c>Main</c>/<c>Plugin</c> class constructor, <c>Awake()</c> or <c>OnEnable()</c> methods.
    /// </summary>
    /// <param name="plugin">The <c>Plugin</c> class from which this mod is being registered.</param>
    /// <param name="optionHolder">
    ///     A class with <c>public static</c> fields of type <see cref="Configurable{T}"/>,
    ///     which are retrieved via reflection to determine the mod's REMIX options.
    /// </param>
    public static void RegisterMod(BaseUnityPlugin plugin, Type? optionHolder)
    {
        RegisteredMods.Add(Assembly.GetCallingAssembly(), new ModMetadata(plugin, optionHolder));

        if (optionHolder is not null)
        {
            ServerOptions.AddOptionSource(optionHolder);
        }
    }

    /// <summary>
    ///     Removes the current mod assembly from ModLib's registry.
    /// </summary>
    /// <returns><c>true</c> if the mod was successfully unregistered, <c>false</c> otherwise (e.g. if it was not registered at all).</returns>
    public static bool UnregisterMod()
    {
        Assembly caller = Assembly.GetCallingAssembly();

        if (!RegisteredMods.TryGetValue(caller, out ModMetadata metadata)) return false;

        if (metadata.OptionHolder is not null)
        {
            ServerOptions.RemoveOptionSource(metadata.OptionHolder);
        }

        return RegisteredMods.Remove(caller);
    }

    internal static BepInPlugin GetModData(Assembly caller)
    {
        return RegisteredMods.TryGetValue(caller, out ModMetadata metadata)
            ? metadata.Plugin
            : throw new ModNotFoundException($"Could not find mod for assembly: {caller.FullName}");
    }

    internal static LogUtils.Logger GetModLogger(Assembly caller)
    {
        return RegisteredMods.TryGetValue(caller, out ModMetadata metadata)
            ? metadata.Logger
            : throw new ModNotFoundException($"Could not find mod for assembly: {caller.FullName}");
    }

    private sealed record ModMetadata
    {
        public BepInPlugin Plugin { get; }
        public Type? OptionHolder { get; }

        public LogUtils.Logger Logger { get; }

        public ModMetadata(BaseUnityPlugin plugin, Type? optionHolder, LogUtils.Logger? logger = null)
            : this(plugin.Info.Metadata, optionHolder, logger)
        {
        }

        public ModMetadata(BepInPlugin plugin, Type? optionHolder, LogUtils.Logger? logger = null)
        {
            Plugin = plugin;
            OptionHolder = optionHolder;

            Logger = logger ?? new ModLogger(plugin);
        }
    }

    /// <summary>
    ///     The exception that is thrown when a ModLib method is called from an unregistered mod assembly.
    /// </summary>
    public sealed class ModNotFoundException : InvalidOperationException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class.
        /// </summary>
        public ModNotFoundException()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <inheritdoc/>
        public ModNotFoundException(string message)
            : base(message + " (Did you remember to register your mod before calling this method?)")
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModNotFoundException"/> class with a specified error message
        ///     and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <inheritdoc/>
        public ModNotFoundException(string message, Exception innerException)
            : base(message + " (Did you remember to register your mod before calling this method?)", innerException)
        {
        }
    }
}