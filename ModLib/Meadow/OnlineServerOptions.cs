using System.Collections.Generic;
using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
/// An online variant of <see cref="ServerOptions"/> which can be serialized by Rain Meadow.
/// </summary>
public class OnlineServerOptions : ServerOptions, Serializer.ICustomSerializable
{
    public OnlineServerOptions()
    {
    }

    public void CustomSerialize(Serializer serializer)
    {
        Logger.LogDebug($"Serializing {this} : Reading? {serializer.IsReading} | Writing? {serializer.IsWriting} (IsDelta? {serializer.IsDelta})");

        Dictionary<string, int> data = [];

        if (serializer.IsWriting)
        {
            data = MyOptions;
        }

        serializer.Serialize(ref data);

        MyOptions = data;

        Logger.LogDebug($"Resulting data: {this}");
    }
}