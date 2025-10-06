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
        foreach (FieldInfo field in Assembly.GetCallingAssembly().GetOptionHolder()?.GetFields(BindingFlags.Public | BindingFlags.Static) ?? [])
        {
            if (field.GetValue(null) is ConfigurableBase configurable
                && Attribute.GetCustomAttribute(field, typeof(ClientOptionAttribute)) is null)
            {
                MyOptions[configurable.key] = (int)configurable.BoxedValue;
            }
        }

        Logger.LogDebug($"{(isOnline ? "Online " : "")}REMIX options are: {this}");
    }

    public void SetOptions(ServerOptions? options)
    {
        Dictionary<string, int> newOptions = options?.MyOptions ?? [];

        foreach (string key in MyOptions.Keys)
        {
            int newValue = newOptions.TryGetValue(key, out int value) ? value : new();

            Logger.LogDebug($"Setting key {MyOptions[key]} to {newValue}.");

            MyOptions[key] = newValue;
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
}

/// <summary>
/// Determines a given REMIX option is not to be synced in an online context (e.g. in a Rain Meadow lobby).
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ClientOptionAttribute : Attribute
{
}