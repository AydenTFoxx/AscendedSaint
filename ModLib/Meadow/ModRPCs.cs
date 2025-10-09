using RainMeadow;
using static ModLib.Options.OptionUtils;

namespace ModLib.Meadow;

/// <summary>
/// Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
public static class ModRPCs
{
    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, OnlineServerOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            Logger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        Logger.LogDebug($"Received data: {options}");

        SharedOptions.SetOptions(options);

        Logger.LogInfo($"Synced REMIX options! New values are: {SharedOptions}");
    }
}