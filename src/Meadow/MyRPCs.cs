using AscendedSaint.Attunement;
using AscendedSaint.Features;
using ModLib.Meadow;
using RainMeadow;

namespace AscendedSaint.Meadow;

/// <summary>
///     Custom events sent to or received in order to properly sync settings and the mod's behavior.
/// </summary>
public static class MyRPCs
{
    /// <summary>
    ///     Syncs the ascension effects of a given creature to the player.
    /// </summary>
    /// <param name="onlineObject">The creature who was ascended or revived.</param>
    [SoftRPCMethod]
    public static void SyncAscensionEffects(RPCEvent rpcEvent, OnlinePhysicalObject onlineObject)
    {
        if (onlineObject is null || onlineObject.apo.realizedObject is not (Creature or Oracle))
        {
            ModLib.Logger.LogWarning($"Received a request to sync an invalid ascension! Target: {onlineObject}");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        bool isRevival = onlineObject.apo.realizedObject is Creature creature
            ? !creature.dead
            : onlineObject.apo.realizedObject is Oracle { health: > 0f };

        AscensionHandler.SpawnAscensionEffects(onlineObject.apo.realizedObject, isRevival);
    }

    /// <summary>
    ///     Requests the owner of a non-transferrable object to revive it in place of the calling player.
    /// </summary>
    /// <param name="onlineObject">The online object to be revived.</param>
    [SoftRPCMethod]
    public static void SyncObjectRevival(RPCEvent rpcEvent, OnlinePhysicalObject onlineObject)
    {
        if (!onlineObject.isMine)
        {
            ModLib.Logger.LogWarning($"Cannot revive an object I don't own: {onlineObject}");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        if (onlineObject.apo.realizedObject is Creature creature)
        {
            RevivalFeature.ReviveCreature(creature);
        }
        else if (onlineObject.apo.realizedObject is Oracle oracle)
        {
            RevivalFeature.ReviveOracle(oracle);
        }
        else
        {
            ModLib.Logger.LogWarning($"Failed to revive invalid target: {onlineObject}");

            rpcEvent.Resolve(new GenericResult.Fail());
        }
    }

    /// <summary>
    ///     Removes a creature from the world's respawn list. A variant of <c>RemoveFromRespawnsList</c> which can be sent as a RPC event.
    /// </summary>
    /// <param name="onlineCreature">The creature to be removed.</param>
    /// <seealso cref="RemoveFromRespawnsList(Creature)"/>
    [SoftRPCMethod]
    public static void SyncRemoveFromRespawnsList(RPCEvent rpcEvent, OnlineCreature? onlineCreature)
    {
        if (!MeadowUtils.IsHost)
        {
            ModLib.Logger.LogWarning("Player is not host; Ignoring removal request.");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        Creature? creature = onlineCreature?.abstractCreature.realizedCreature;

        if (creature is null)
        {
            ModLib.Logger.LogWarning($"Cannot remove null creature from respawns list!");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        RevivalFeature.RemoveFromRespawnsList(creature);
    }
}