using System;
using System.Collections.Generic;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Utilities for retrieving and evaluating data exclusive to the Rain Meadow mod.
/// </summary>
/// <remarks>
///     <para>
///         Warning: Always ensure Rain Meadow is enabled before using this class!
///     </para>
///     <para>
///         Properties and methods like <see cref="Extras.IsMeadowEnabled"/>, <see cref="Extras.IsOnlineSession"/>, and <see cref="CompatibilityManager.IsRainMeadowEnabled()"/>
///         can all be used/queried before accessing any of this class's members. Otherwise, a <see cref="TypeLoadException"/> will be thrown, even if the given member does not have any Meadow-specific code.
///     </para>
/// </remarks>
public static class MeadowUtils
{
    /// <summary>
    ///     Determines if the current game session is an online lobby.
    /// </summary>
    public static bool IsOnline => OnlineManager.lobby is not null;

    /// <summary>
    ///     Determines if this player is the host of an online session. On singleplayer, this is always true.
    /// </summary>
    public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

    /// <summary>
    ///     Determines if the given physical object belongs to the client.
    /// </summary>
    /// <remarks>
    ///     If the current game session is not online, this always returns <c>true</c>.
    /// </remarks>
    /// <param name="physicalObject">The object itself.</param>
    /// <returns><c>true</c> if the physical object belongs to this client (or the current game session is not online), <c>false</c> otherwise.</returns>
    public static bool IsMine(PhysicalObject physicalObject) => physicalObject.IsLocal();

    /// <summary>
    ///     Obtains the online name of the given player.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>null</c> if none is found.</returns>
    public static string? GetOnlineName(this Player self)
    {
        if (!IsOnline) return null;

        OnlineCreature? onlineCreature = self.abstractCreature.GetOnlineCreature();

        if (onlineCreature is null or { isAvatar: false })
        {
            Core.Logger.LogWarning($"Cannot retrieve the online name of invalid target: {onlineCreature}");
            return null;
        }

        OnlineEntity.EntityId targetId = onlineCreature.id;
        foreach (KeyValuePair<OnlinePlayer, OnlineEntity.EntityId> playerAvatar in OnlineManager.lobby.playerAvatars)
        {
            if (playerAvatar.Value == targetId)
                return playerAvatar.Key.id.GetPersonaName();
        }

        Core.Logger.LogWarning($"Failed to retrieve the online name of {onlineCreature}!");

        return null;
    }

    /// <summary>
    ///     Logs a message to Rain Meadow's chat (as the system) for all online players.
    /// </summary>
    /// <param name="message">The contents of the message to be sent.</param>
    public static void LogSystemMessage(string message)
    {
        if (!IsOnline) return;

        foreach (OnlinePlayer onlinePlayer in OnlineManager.lobby.participants)
        {
            if (onlinePlayer.isMe) continue;

            onlinePlayer.SendRPCEvent(ModRPCs.LogSystemMessage, message);
        }

        ModRPCs.LogSystemMessage(message); // Run the RPC method anyway; No need to repeat code.
    }

    /// <summary>
    ///     Determines if the current online game session is of the given game mode type. If not online, this is always false.
    /// </summary>
    /// <param name="gameMode">The gamemode to be tested for.</param>
    /// <returns><c>true</c> if the current game session is both online and of the given game mode type; <c>false</c> otherwise.</returns>
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

    /// <summary>
    ///     Requests the owner of a given online object for its ownership, then optionally runs a callback method after resolving the request.
    /// </summary>
    /// <param name="onlineObject">The online object whose ownership will be requested.</param>
    /// <param name="callback">The optional callback method to be executed after resolving the request.</param>
    public static void RequestOwnership(OnlinePhysicalObject onlineObject, Action<GenericResult>? callback = null)
    {
        if (!ValidateOwnershipRequest(onlineObject))
        {
            callback?.Invoke(new GenericResult.Fail());
            return;
        }

        try
        {
            Core.Logger.LogDebug($"Requesting ownership of {onlineObject}...");

            onlineObject.Request();

            (onlineObject.pendingRequest as RPCEvent)?.Then(callback ?? DefaultCallback);
        }
        catch (Exception ex)
        {
            Core.Logger.LogError($"Failed to request ownership of {onlineObject}!", ex);

            callback?.Invoke(new GenericResult.Error());
        }

        void DefaultCallback(GenericResult result)
        {
            Core.Logger.LogDebug($"Request result: {result} | New ownership: {onlineObject.owner}");
        }
    }

    private static bool ValidateOwnershipRequest(OnlinePhysicalObject onlineObject)
    {
        if (onlineObject is null)
        {
            Core.Logger.LogWarning($"Cannot request the ownership of a null object; Aborting operation.");
        }
        else if (onlineObject.isMine)
        {
            Core.Logger.LogWarning($"{onlineObject} is already mine!");
        }
        else if (!onlineObject.isTransferable)
        {
            Core.Logger.LogWarning($"{onlineObject} cannot be transfered.");
        }
        else if (onlineObject.isPending)
        {
            Core.Logger.LogWarning($"{onlineObject} has a pending request.");
        }
        else if (!onlineObject.currentlyJoinedResource.isAvailable)
        {
            Core.Logger.LogWarning($"{onlineObject} is in an unavailable resource.");
        }
        else
        {
            return true;
        }

        return false;
    }
}