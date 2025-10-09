using AscendedSaint.Attunement;
using ModLib.Meadow;
using RainMeadow;

namespace AscendedSaint.Meadow;

/// <summary>
/// Custom events sent to or received in order to properly sync settings and the mod's behavior.
/// </summary>
public static class MyRPCs
{
    /// <summary>
    /// Syncs the ascension effects of a given creature to the player.
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
    /// Removes a creature from the world's respawn list. A variant of <c>RemoveFromRespawnsList</c> which can be sent as a RPC event.
    /// </summary>
    /// <param name="onlineCreature">The creature to be removed.</param>
    /// <seealso cref="RemoveFromRespawnsList(Creature)"/>
    [SoftRPCMethod]
    public static void SyncRemoveFromRespawnsList(RPCEvent rpcEvent, OnlineCreature creature)
    {
        if (!MeadowUtils.IsHost)
        {
            ModLib.Logger.LogWarning("Player is not host; Ignoring removal request.");

            rpcEvent.Resolve(new GenericResult.Fail());
            return;
        }

        CreatureState state = creature.abstractCreature.state;
        EntityID ID = creature.abstractCreature.ID;

        GameSession? gameSession = creature.realizedCreature.room?.game.session;

        if (state.alive && ID.spawner >= 0 && gameSession is StoryGameSession storySession)
        {
            storySession.saveState.respawnCreatures.Remove(ID.spawner);
        }

        ModLib.Logger.LogInfo($"{creature} ({ID.spawner}) has been removed from the respawn list!");
    }
}