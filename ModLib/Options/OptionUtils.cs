namespace ModLib.Options;

/// <summary>
///     Utility methods for retrieving the mod's REMIX options.
/// </summary>
/// <remarks>This also allows for overriding the player's local options without touching their REMIX values.</remarks>
public static class OptionUtils
{
    /// <summary>
    ///     The client's local <see cref="ServerOptions"/> instance, overriden when joining an online lobby.
    /// </summary>
    public static ServerOptions SharedOptions { get; } = new();

    /// <summary>
    ///     Directly requests for the client's REMIX options, then retrieves its value.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <returns>The configured value for the given option.</returns>
    public static T? GetClientOptionValue<T>(Configurable<T>? option) => option is null ? default : option.Value;

    /// <summary>
    ///     Determines if a given option is enabled in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <returns>The local value for the given option.</returns>
    public static int GetOptionValue<T>(Configurable<T>? option) =>
        Extras.IsHostPlayer
            ? option is not null
                ? ServerOptions.CastOptionValue(option.Value)
                : 0
            : GetOptionValue(option?.key ?? "none");

    /// <summary>
    ///     Directly requests for the client's REMIX options, then determines whether it is enabled or not.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    public static bool IsClientOptionEnabled(Configurable<bool>? option) => option?.Value ?? false;

    /// <summary>
    ///     Directly requests for the client's REMIX options, then compares its values to the provided argument.
    /// </summary>
    /// <remarks>
    ///     This should only be used for options which are not synced by <c>Options.ServerOptions</c>
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    public static bool IsClientOptionValue<T>(Configurable<T>? option, T value) => option?.Value?.Equals(value) ?? false;

    /// <summary>
    ///     Determines if a given option is enabled in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <param name="option">The option to be queried. Must be of <c>bool</c> type.</param>
    /// <returns>The configured value for the given option.</returns>
    /// <seealso cref="IsClientOptionEnabled(Configurable{bool}?)"/>
    public static bool IsOptionEnabled(Configurable<bool>? option) =>
        Extras.IsHostPlayer
            ? option?.Value ?? false
            : IsOptionEnabled(option?.key ?? "none");

    /// <summary>
    ///     Determines if a given option has the provided value in the client's REMIX options, or the host's if in an online lobby.
    /// </summary>
    /// <remarks>
    ///     If the client is not in an online lobby, this has the same effect as directly checking the configurable itself.
    /// </remarks>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    /// <seealso cref="IsClientOptionValue{T}(Configurable{T}?, T)"/>
    public static bool IsOptionValue<T>(Configurable<T>? option, T value) =>
        Extras.IsHostPlayer
            ? option?.Value?.Equals(value) ?? false
            : IsOptionValue(option?.key ?? "none", value);

    /// <summary>
    ///     Retrieves the value of the given option from the local <c>SharedOptions</c> property.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns>The value stored in the local <c>SharedOptions</c> property.</returns>
    private static int GetOptionValue(string option) => SharedOptions.MyOptions.TryGetValue(option, out int value) ? value : default;

    /// <summary>
    ///     Determines if the local <c>SharedOptions</c> property has the given option enabled.
    /// </summary>
    /// <param name="option">The name of the option to be queried.</param>
    /// <returns><c>true</c> if the given option is enabled, <c>false</c> otherwise.</returns>
    private static bool IsOptionEnabled(string option) => SharedOptions.MyOptions.TryGetValue(option, out int value) && value != default;

    /// <summary>
    ///     Determines if the local <c>SharedOptions</c> property has the given option set to the provided value.
    /// </summary>
    /// <typeparam name="T">The type of the configurable itself.</typeparam>
    /// <param name="option">The name of the option to be queried.</param>
    /// <param name="value">The expected value to be checked.</param>
    /// <returns><c>true</c> if the option's value matches the given argument, <c>false</c> otherwise.</returns>
    private static bool IsOptionValue<T>(string option, T value) => SharedOptions.MyOptions.TryGetValue(option, out int v) && v.Equals(value);
}