using ModLib.Meadow;

namespace ModLib.Options;

/// <summary>
/// Utility methods for retrieving the mod's REMIX options.
/// </summary>
/// <remarks>This also allows for overriding the player's local options without touching their REMIX values.</remarks>
public static class OptionUtils
{
    public static ServerOptions SharedOptions { get; } = new();

    public static T? GetClientOptionValue<T>(Configurable<T>? option) => option is null ? default : option.Value;

    public static int GetOptionValue<T>(Configurable<T>? option) =>
        CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost
            ? GetOptionValue(option?.key ?? "none")
            : int.TryParse(option?.Value?.ToString(), out int value)
                ? value
                : default;

    /// <summary>
    /// Directly requests for the client's REMIX options, then retrieves its value.
    /// </summary>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns>The configured value for the given option.</returns>
    /// <remarks>This should only be used for options which are not synced by <c>Options.ServerOptions</c></remarks>
    public static bool IsClientOptionEnabled(Configurable<bool>? option) => option?.Value ?? false;

    /// <summary>
    /// Directly requests for the client's REMIX options, then compares its values to the provided argument.
    /// </summary>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    /// <remarks>This should only be used for options which are not synced by <c>Options.ServerOptions</c></remarks>
    public static bool IsClientOptionValue<T>(Configurable<T>? option, T value) => option?.Value?.Equals(value) ?? false;

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
    /// Determines if a given option has the provided value in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    /// <remarks>If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.</remarks>
    /// <seealso cref="IsClientOptionValue{T}(Configurable{T}?, T)"/>
    public static bool IsOptionValue<T>(Configurable<T>? option, T value) =>
        CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost
            ? IsOptionValue(option?.key ?? "none", value)
            : option?.Value?.Equals(value) ?? false;

    private static int GetOptionValue(string option) => SharedOptions.MyOptions.TryGetValue(option, out int value) ? value : default;

    /// <summary>
    /// Determines if the local <c>SharedOptions</c> property has the given option enabled.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    private static bool IsOptionEnabled(string option) => SharedOptions.MyOptions.TryGetValue(option, out int value) && value != default;

    /// <summary>
    /// Determines if the local <c>SharedOptions</c> property has the given option set to the provided value.
    /// </summary>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The name of the option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    private static bool IsOptionValue<T>(string option, T value) => SharedOptions.MyOptions.TryGetValue(option, out int v) && v.Equals(value);
}