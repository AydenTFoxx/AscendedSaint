using ControlLib.Possession;

namespace ControlLib.Utils;

/// <summary>
/// Utility methods for retrieving the mod's REMIX options.
/// </summary>
/// <remarks>This also allows for overriding the player's local options without touching their REMIX values.</remarks>
public static class OptionUtils
{
    public static Options.ServerOptions SharedOptions { get; } = new();

    /// <summary>
    /// Directly requests for the client's REMIX options, then retrieves its value.
    /// </summary>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns>The configured value for the given option.</returns>
    /// <remarks>This should only be used for options which are not synced by <c>Options.ServerOptions</c></remarks>
    public static bool IsClientOptionEnabled(Configurable<bool>? option) => option?.Value ?? false;

    /// <summary>
    /// Determines if a given option is enabled in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns>The configured value for the given option.</returns>
    /// <remarks>If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.</remarks>
    /// <seealso cref="IsClientOptionEnabled(Configurable{bool}?)"/>
    public static bool IsOptionEnabled(Configurable<bool>? option) =>
        CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost
            ? IsOptionEnabled(option?.key ?? "none")
            : option?.Value ?? false;

    /// <summary>
    /// Determines if the local <c>SharedOptions</c> property has the given option enabled.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    private static bool IsOptionEnabled(string option) => SharedOptions.MyOptions.TryGetValue(option, out bool value) && value;
}