using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModLib.Options;

/// <summary>
///     Holds the client's current REMIX options; Provides the ability to override these options, as well as serialize them in an online context.
/// </summary>
public class ServerOptions
{
    /// <summary>
    ///     The local holder of REMIX options' values.
    /// </summary>
    public Dictionary<string, int> MyOptions = [];

    /// <summary>
    ///     Sets the local holder's values to those from the REMIX option interface.
    /// </summary>
    public void RefreshOptions()
    {
        foreach (FieldInfo field in ModPlugin.Assembly.GetOptionHolder()?.GetFields(BindingFlags.Public | BindingFlags.Static) ?? [])
        {
            if (field.GetValue(null) is ConfigurableBase configurable
                && Attribute.GetCustomAttribute(field, typeof(ClientOptionAttribute)) is null)
            {
                MyOptions[configurable.key] = CastOptionValue(configurable.BoxedValue);
            }
        }

        Logger.LogDebug($"{(Extras.IsOnlineSession ? "Online " : "")}REMIX options are: {this}");
    }

    /// <summary>
    ///     Sets the local holder's values to those from the given source.
    /// </summary>
    /// <param name="source">The source whose values will be copied.</param>
    public void SetOptions(ServerOptions source) => SetOptions(source.MyOptions);

    /// <summary>
    ///     Sets the local holder's values to those from the provided dictionary.
    /// </summary>
    /// <param name="options">The dictionary whose values will be copied.</param>
    public void SetOptions(Dictionary<string, int> options)
    {
        foreach (KeyValuePair<string, int> pair in options)
        {
            if (!MyOptions.TryGetValue(pair.Key, out _))
            {
                Logger.LogWarning($"{nameof(MyOptions)} does not have option \"{pair.Key}\", will not be synced.");
                continue;
            }

            Logger.LogDebug($"Setting key {pair.Key} to {pair.Value}.");

            MyOptions[pair.Key] = pair.Value;
        }
    }

    /// <summary>
    ///     Returns a string containing the <see cref="ServerOptions"/>' local values.
    /// </summary>
    /// <returns>A string containing the <see cref="ServerOptions"/>' local values.</returns>
    public override string ToString() => $"[{FormatOptions()}]";

    private string GetOptionAcronym(string optionName) =>
        string.Concat(optionName.Split('_').Select(s => s.First())).ToUpperInvariant();

    private string FormatOptions()
    {
        StringBuilder stringBuilder = new();

        foreach (KeyValuePair<string, int> kvp in MyOptions)
        {
            stringBuilder.Append($"{GetOptionAcronym(kvp.Key)}: {kvp.Value}; ");
        }

        return stringBuilder.ToString().Trim();
    }

    /// <summary>
    ///     Casts the provided object to an equivalent integer value.
    /// </summary>
    /// <param name="value">The value to be cast.</param>
    /// <returns>The integer equivalent of the provided object.</returns>
    public static int CastOptionValue(object? value)
    {
        try
        {
            return value is IConvertible convertible
                ? convertible.ToInt32(CultureInfo.InvariantCulture)
                : int.TryParse(value?.ToString(), out int result)
                    ? result
                    : default;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to cast option value: {value}", ex);

            return 0;
        }
    }
}

/// <summary>
/// Determines a given REMIX option is not to be synced in an online context (e.g. in a Rain Meadow lobby).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}