using System;
using RainMeadow;

namespace ModLib.Meadow;

public static class MeadowUtils
{
    public static bool IsOnline => OnlineManager.lobby is not null;
    public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

    /// <summary>
    /// Logs a message to Rain Meadow's chat (as the system) and to this mod's log file.
    /// </summary>
    /// <param name="message">The message to be sent to all players.</param>
    public static void LogSystemMessage(string message)
    {
        ChatLogManager.LogSystemMessage(message);

        Logger.LogMessage($"-> {message}");
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

    public static bool IsMine(PhysicalObject physicalObject) => physicalObject.IsLocal();

    public static void RequestOptionsSync()
    {
        if (!IsOnline || IsHost) return;

        OnlineManager.lobby.owner.SendRPCEvent(ModRPCs.RequestRemixOptionsSync, OnlineManager.mePlayer);
    }

    public static bool RequestOwnership(PhysicalObject physicalObject) =>
        RequestOwnership(physicalObject.abstractPhysicalObject.GetOnlineObject());

    public static bool RequestOwnership(OnlinePhysicalObject? onlineObject)
    {
        if (onlineObject is null)
        {
            Logger.LogWarning($"Cannot request the ownership of a null object; Aborting operation.");
            return false;
        }

        try
        {
            Logger.LogDebug($"Requesting ownership of {onlineObject}...");

            onlineObject.Request();

            Logger.LogDebug($"New owner is: {onlineObject.owner}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to request ownership of {onlineObject}!", ex);
        }

        return onlineObject.isMine;
    }

    public enum MeadowGameModes
    {
        Meadow,
        Story,
        Arena,
        Custom = -1
    }
}