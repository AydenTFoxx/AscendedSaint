using System;
using AscendedSaint.Meadow;
using ModLib.Meadow;
using RainMeadow;

namespace AscendedSaint.Attunement;

/// <summary>
/// A set of Meadow-compatible utility functions to ensure proper sync across all clients.
/// </summary>
public static class MeadowHelper
{
    /// <summary>
    /// Attempts to revive a given creature in a Meadow-compatible context.
    /// </summary>
    /// <param name="physicalObject">The creature to be revived.</param>
    /// <param name="revivalMethod">The fallback revival method to be called. If the client owns the revived entity or the game session is not an online lobby, this is called instead.</param>
    public static void TryReviveCreature(PhysicalObject physicalObject, Action revivalMethod)
    {
        if (OnlineManager.lobby is null)
        {
            revivalMethod.Invoke();
        }
        else
        {
            OnlinePhysicalObject? onlineObject = physicalObject.abstractPhysicalObject.GetOnlineObject();

            if (onlineObject is null)
            {
                ModLib.Logger.LogWarning($"Failed to retrieve the OnlinePhysicalObject of {physicalObject}. No operation will be performed.");
                return;
            }

            if (!onlineObject.isMine)
                MeadowUtils.RequestOwnership(physicalObject);

            if (onlineObject.owner == OnlineManager.mePlayer)
            {
                ModLib.Logger.LogDebug($"Player owns {physicalObject} ({onlineObject.id}), calling revival method.");

                revivalMethod.Invoke();
            }
            else
            {
                ModLib.Logger.LogDebug($"Requesting owner of {physicalObject} ({onlineObject.id}) to run revival method.");

                ModRPCManager.SendRPCEvent(onlineObject.owner, MyRPCs.SyncCreatureRevival, onlineObject);
            }
        }
    }

    /// <summary>
    /// Attempts to request the lobby owner to remove the given creature from the world's respawn list.
    /// </summary>
    /// <param name="creature">The creature to be removed.</param>
    /// <returns><c>true</c> if the request was successfully sent, <c>false</c> otherwise.</returns>
    public static bool TryRemoveCreatureRespawn(Creature creature)
    {
        if (OnlineManager.lobby is null || OnlineManager.lobby.isOwner) return false;

        OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature();

        if (onlineCreature is null) return false;

        OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.SyncRemoveFromRespawnsList, onlineCreature);

        return true;
    }

    /// <summary>
    /// Requests all online players to sync Saint's ascension ability effects.
    /// </summary>
    /// <param name="physicalObject">The physical object which was ascended or revived.</param>
    /// <remarks>If the player is not in an online lobby, this has the same effects as calling <see cref="SpawnAscensionEffects(PhysicalObject, bool)"/></remarks>
    public static void RequestAscensionEffectsSync(PhysicalObject physicalObject, bool isRevival = true)
    {
        if (OnlineManager.lobby is not null)
        {
            OnlinePhysicalObject? onlineObject = physicalObject.abstractPhysicalObject.GetOnlineObject();

            if (onlineObject is null) return;

            foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
            {
                if (onlinePlayer.isMe) continue;

                onlinePlayer.SendRPCEvent(MyRPCs.SyncAscensionEffects, onlineObject);
            }
        }

        AscensionHandler.SpawnAscensionEffects(physicalObject, isRevival);
    }
}