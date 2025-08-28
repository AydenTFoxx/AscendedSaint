using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ControlLib.Utils.Options;

public class ServerOptions
{
    public Dictionary<string, bool> MyOptions { get; } = [];

    public void RefreshOptions(bool isOnline = false)
    {
        foreach (FieldInfo? field in typeof(CLOptions).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field?.GetValue(null) is not Configurable<bool> configurable
                || Attribute.GetCustomAttribute(field, typeof(ClientOptionAttribute)) is not null)
            {
                continue;
            }
            MyOptions[configurable.key] = configurable.Value;
        }

        CLLogger.LogDebug($"{(isOnline ? "Online " : "")}REMIX options are: {this}");
    }

    public void SetOptions(ServerOptions? options)
    {
        Dictionary<string, bool> opts = options?.MyOptions ?? MyOptions;

        foreach (string key in opts.Keys)
        {
            bool value = options is not null && opts[key];

            CLLogger.LogDebug($"Setting key {MyOptions[key]} to {value}.");

            MyOptions[key] = value;
        }
    }

    public override string ToString() => $"{nameof(ServerOptions)} => [{FormatOptions()}]";

    protected string GetOptionAcronym(string optionName) =>
        string.Concat(optionName.Split('_').Select(s => s.First())).ToUpperInvariant();

    protected string FormatOptions()
    {
        StringBuilder stringBuilder = new();

        foreach (KeyValuePair<string, bool> kvp in MyOptions)
        {
            stringBuilder.Append($"{GetOptionAcronym(kvp.Key)}: {kvp.Value}; ");
        }

        return stringBuilder.ToString().Trim();
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ClientOptionAttribute : Attribute
{
}