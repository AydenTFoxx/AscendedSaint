using System.Linq;
using BepInEx;
using BepInEx.Logging;
using LogUtils.Enums;

namespace ModLib;

/// <summary>
///     A mod-specific logger instance, which uses LogUtils' API to log to a variety of relevant sources.
/// </summary>
public class ModLogger : LogUtils.Logger
{
    /// <summary>
    ///     The LogID of the primary log file for this logger instance.
    /// </summary>
    public LogID MyLogID { get; protected set; }

    /// <inheritdoc cref="ModLogger(BepInPlugin, LogID)"/> />
    public ModLogger(BaseUnityPlugin plugin, LogID? logID = null)
        : this(plugin.Info.Metadata, logID)
    {
    }

    /// <summary>
    ///     Creates a new logger instance for the given mod.
    /// </summary>
    /// <param name="plugin">The mod itself.</param>
    /// <param name="logID">
    ///     The LogID to use for this mod.
    ///     If <c>null</c>, a new LogID will be registered and assigned to this loader.
    /// </param>
    public ModLogger(BepInPlugin plugin, LogID? logID = null)
        : base(GetLogSource(plugin))
    {
        MyLogID = logID ?? new LogID(plugin.Name.Replace(' ', '\0'), LogAccess.FullAccess, true);

        Targets.LogIDs.Add(MyLogID);
    }

    private static ILogSource GetLogSource(BepInPlugin plugin) =>
        BepInEx.Logging.Logger.Sources.First(s => s.SourceName == plugin.Name);
}