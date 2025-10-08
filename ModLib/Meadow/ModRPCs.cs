using RainMeadow;
using static ModLib.Options.OptionUtils;

namespace ModLib.Meadow;

/// <summary>
/// Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
public static class ModRPCs
{
    [SoftRPCMethod]
    public static void RequestRemixOptionsSync(RPCEvent rpcEvent, OnlinePlayer onlinePlayer)
    {
        if (!MeadowUtils.IsHost)
        {
            Logger.LogWarning("Player is not host; Cannot sync options with other players!");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        if (SharedOptions.MyOptions.Count < 1)
        {
            SharedOptions.RefreshOptions(true);
        }

        Logger.LogInfo($"Syncing REMIX options with player {onlinePlayer}...");

        onlinePlayer.SendRPCEvent(SyncRemixOptions, new OnlineServerOptions() { MyOptions = SharedOptions.MyOptions });
    }

    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, OnlineServerOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            Logger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        SharedOptions.SetOptions(options);

        Logger.LogInfo($"Synced REMIX options! New values are: {SharedOptions}");
    }
}