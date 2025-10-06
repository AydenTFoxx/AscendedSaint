using AscendedSaint.Attunement;
using ModLib.Options;
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
    public static void SyncAscensionEffects(OnlinePhysicalObject onlineObject)
    {
        if (onlineObject is null || onlineObject.apo.realizedObject is not (Creature or Oracle))
        {
            ModLib.Logger.LogWarning($"Received a request to sync an invalid ascension! Target: {onlineObject}");
            return;
        }

        bool isRevival = onlineObject.apo.realizedObject is Creature creature
            ? !creature.dead
            : onlineObject.apo.realizedObject is Oracle { health: > 0f };

        AscensionHandler.SpawnAscensionEffects(onlineObject.apo.realizedObject, isRevival);
    }

    /// <summary>
    /// Updates a revived creature from its owner to all subscribed players.
    /// </summary>
    /// <param name="revivedCreature">The creature who was revived.</param>
    /// <remarks>While creature revival is usually synced on its own, iterators and especially players need special handling for proper sync.</remarks>
    [SoftRPCMethod]
    public static void SyncCreatureRevival(OnlinePhysicalObject revivedCreature)
    {
        PhysicalObject physicalObject = revivedCreature.apo.realizedObject;

        if (physicalObject is Creature creature)
        {
            ModLib.Logger.LogInfo($"{creature.Template.name} was revived!");

            AscensionHandler.ReviveCreature(creature, OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR) * 0.01f);
        }
        else if (physicalObject is Oracle oracle)
        {
            ModLib.Logger.LogInfo($"{AscensionHandler.GetOracleName(oracle.ID)} was revived!");

            AscensionHandler.ReviveOracle(oracle);
        }
        else
        {
            ModLib.Logger.LogWarning($"Expected creature or iterator revived, got: {physicalObject}");
        }
    }

    /// <summary>
    /// Removes a creature from the world's respawn list. A variant of <c>RemoveFromRespawnsList</c> which can be sent as a RPC event.
    /// </summary>
    /// <param name="onlineCreature">The creature to be removed.</param>
    /// <seealso cref="RemoveFromRespawnsList(Creature)"/>
    [SoftRPCMethod]
    public static void SyncRemoveFromRespawnsList(OnlineCreature creature)
    {
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