using System;
using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

public static class MeadowUtils
{
    public static bool IsOnline => OnlineManager.lobby is not null;
    public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

    /// <summary>
    /// Obtains the online name of the given player.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>null</c> if none is found.</returns>
    public static string? GetOnlineName(this Player self)
    {
        OnlinePhysicalObject? playerOPO = self.abstractPhysicalObject.GetOnlineObject();

        if (playerOPO is null)
        {
            Logger.LogWarning("Failed to retrieve the OnlinePhysicalObject representation of the player, aborting.");
            return null;
        }

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (playerOPO.owner == onlinePlayer)
                return onlinePlayer.id.GetPersonaName();
        }

        Logger.LogWarning($"Failed to retrieve the OnlinePlayer instance of {self}!");

        return null;
    }

    /// <summary>
    /// Logs a message to Rain Meadow's chat (as the system) and to this mod's log file.
    /// </summary>
    /// <param name="message">The message to be sent to all players.</param>
    public static void LogSystemMessage(string message)
    {
        ChatLogManager.LogSystemMessage(message);

        Logger.LogMessage($"-> {message}");
    }

    public static void InitOptionsSync()
    {
        if (!IsOnline || !IsHost) return;

        foreach (OnlinePlayer onlinePlayer in OnlineManager.lobby.participants)
        {
            if (onlinePlayer.isMe) continue;

            onlinePlayer.SendRPCEvent(ModRPCs.SyncRemixOptions, new OnlineServerOptions() { MyOptions = OptionUtils.SharedOptions.MyOptions });
        }
    }

    public static bool IsGameMode(MeadowGameModes gameMode)
    {
        if (!IsOnline) return false;

        int gamemode = OnlineManager.lobby.gameMode switch
        {
            MeadowGameMode => 0,
            StoryGameMode => 1,
            ArenaOnlineGameMode => 2,
            _ => -1
        };

        return gamemode == (int)gameMode;
    }

    public static void RequestOwnership(PhysicalObject physicalObject) =>
        RequestOwnership(physicalObject.abstractPhysicalObject.GetOnlineObject(), null);

    public static void RequestOwnership(OnlinePhysicalObject? onlineObject, Action<GenericResult>? callback = null)
    {
        if (onlineObject is null)
        {
            Logger.LogWarning($"Cannot request the ownership of a null object; Aborting operation.");
            return;
        }

        try
        {
            Logger.LogDebug($"Requesting ownership of {onlineObject}...");

            onlineObject.Request();

            (onlineObject.pendingRequest as RPCEvent)?.Then(callback ?? DefaultCallback);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to request ownership of {onlineObject}!", ex);
        }

        void DefaultCallback(GenericResult result) => Logger.LogDebug($"Request successful? {result is GenericResult.Ok} | Do I own the object? {onlineObject.isMine}");
    }

    public enum MeadowGameModes
    {
        Meadow,
        Story,
        Arena,
        Custom = -1
    }
}