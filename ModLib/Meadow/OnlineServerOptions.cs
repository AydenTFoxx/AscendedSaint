using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
/// An online variant of <see cref="ServerOptions"/> which can be serialized by Rain Meadow.
/// </summary>
public class OnlineServerOptions : ServerOptions, Serializer.ICustomSerializable
{
    /// <summary>
    ///     Serializes and de-serializes this object's <see cref="ServerOptions.MyOptions"/> value.
    /// </summary>
    /// <param name="serializer">The serializer used for operations.</param>
    public void CustomSerialize(Serializer serializer)
    {
        Logger.LogDebug($"Serializing {this} : Reading? {serializer.IsReading} | Writing? {serializer.IsWriting}");

        serializer.Serialize(ref MyOptions);

        Logger.LogDebug($"Resulting data: {this}");
    }
}