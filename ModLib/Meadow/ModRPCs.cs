using RainMeadow;
using static ModLib.Options.OptionUtils;

namespace ModLib.Meadow;

/// <summary>
///     Events sent to and received by clients, used for syncing data in an online context.
/// </summary>
public static class ModRPCs
{
    /// <summary>
    ///     Writes the provided system message to the player's chat.
    /// </summary>
    /// <param name="message">The message to be displayed</param>
    [SoftRPCMethod]
    public static void LogSystemMessage(string message)
    {
        ChatLogManager.LogSystemMessage(message);

        Core.Logger.LogMessage($"-> {message}");
    }

    /// <summary>
    ///     Requests the owner of the current lobby to sync their REMIX options with this client.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="onlinePlayer">The player who called this event.</param>
    [SoftRPCMethod]
    public static void RequestSyncRemixOptions(RPCEvent rpcEvent, OnlinePlayer onlinePlayer)
    {
        if (!MeadowUtils.IsHost)
        {
            Core.Logger.LogWarning("Player is not host; Cannot sync REMIX options!");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        Core.Logger.LogDebug($"Syncing local REMIX options with client {onlinePlayer}...");

        onlinePlayer.SendRPCEvent(SyncRemixOptions, (OnlineServerOptions)SharedOptions);
    }

    /// <summary>
    ///     Overrides the player's local <see cref="SharedOptions"/> instance with the host's own REMIX options.
    /// </summary>
    /// <param name="rpcEvent">The RPC event itself.</param>
    /// <param name="options">The serialized <see cref="Options.ServerOptions"/> value.</param>
    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, OnlineServerOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            Core.Logger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        Core.Logger.LogDebug($"Received data: {options}");

        SharedOptions.SetOptions(options);

        Core.Logger.LogInfo($"Synced REMIX options! New values are: {SharedOptions}");
    }
}