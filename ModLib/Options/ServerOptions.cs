using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModLib.Options;

public class ServerOptions
{
    public Dictionary<string, int> MyOptions = [];

    public void RefreshOptions(bool isOnline = false)
    {
        foreach (FieldInfo field in ModPlugin.Assembly.GetOptionHolder()?.GetFields(BindingFlags.Public | BindingFlags.Static) ?? [])
        {
            if (field.GetValue(null) is ConfigurableBase configurable
                && Attribute.GetCustomAttribute(field, typeof(ClientOptionAttribute)) is null)
            {
                MyOptions[configurable.key] = CastOptionValue(configurable.BoxedValue);
            }
        }

        Logger.LogDebug($"{(isOnline ? "Online " : "")}REMIX options are: {this}");
    }

    public void SetOptions(ServerOptions source) => SetOptions(source.MyOptions);

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

    public override string ToString() => $"[{FormatOptions()}]";

    protected string GetOptionAcronym(string optionName) =>
        string.Concat(optionName.Split('_').Select(s => s.First())).ToUpperInvariant();

    protected string FormatOptions()
    {
        StringBuilder stringBuilder = new();

        foreach (KeyValuePair<string, int> kvp in MyOptions)
        {
            stringBuilder.Append($"{GetOptionAcronym(kvp.Key)}: {kvp.Value}; ");
        }

        return stringBuilder.ToString().Trim();
    }

    private static int CastOptionValue(object? value)
    {
        return value is int i
            ? i
            : value is bool b
                ? b
                    ? 1
                    : 0
                : int.TryParse(value?.ToString(), out int result)
                    ? result
                    : default;
    }
}

/// <summary>
/// Determines a given REMIX option is not to be synced in an online context (e.g. in a Rain Meadow lobby).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}