using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
/// An online variant of <see cref="ServerOptions"/> which can be serialized by Rain Meadow.
/// </summary>
public class OnlineServerOptions : ServerOptions, Serializer.ICustomSerializable
{
    public OnlineServerOptions(ServerOptions source)
    {
        SetOptions(source);
    }

    public OnlineServerOptions()
    {
    }

    public void CustomSerialize(Serializer serializer) => serializer.Serialize(ref MyOptions);

    public override string ToString() => $"{nameof(OnlineServerOptions)} => {FormatOptions()}";
}