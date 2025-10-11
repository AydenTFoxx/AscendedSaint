using System.Collections.Generic;
using ModLib;
using ModLib.Options;
using MoreSlugcats;
using RWCustom;

namespace AscendedSaint.Features;

public static class RevivalFeature
{
    private static float TargetHealth => OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR) * 0.01f;
    private static int RevivalStun => OptionUtils.GetOptionValue(Options.REVIVAL_STUN_DURATION);

    /// <summary>
    /// Obtains an iterator's full name based on its ID.
    /// </summary>
    /// <param name="oracleID">The oracle ID to be tested.</param>
    /// <returns>The iterator's name (e.g. <c>Five Pebbles</c>), or the string <c>Unknown Iterator (<paramref name="oracleID"/>)</c> if a specific name couldn't be determined.</returns>
    /// <remarks>Custom iterators should only be added here if they are accessible and ascendable in Saint's campaign.</remarks>
    public static string GetOracleName(Oracle.OracleID oracleID) =>
        oracleID == Oracle.OracleID.SL
        ? "Looks to the Moon"
        : oracleID == MoreSlugcatsEnums.OracleID.CL
            ? "Five Pebbles"
            : oracleID == MoreSlugcatsEnums.OracleID.ST
                ? "Sliver of Straw" // Would you de-ascend Sliver of Straw?
                : $"Unknown Iterator ({oracleID})";

    /// <summary>
    /// Restores a creature's health and sets its state as "alive" once again.
    /// </summary>
    /// <param name="creature">The creature to be revived.</param>
    /// <param name="health">
    ///     The health to be restored for the newly revived creature.
    ///     For slugcats/slugpups, this is always <c>1f</c> (100%).
    /// </param>
    public static bool ReviveCreature(Creature target)
    {
        if (!target.dead)
        {
            Logger.LogWarning($"{target} cannot be revived, as it's already alive!");
            return false;
        }

        AbstractCreature abstractCreature = target.abstractCreature;

        if (abstractCreature.state is HealthState healthState)
        {
            healthState.alive = true;
            healthState.health = target is Player ? 1f : TargetHealth;
        }

        target.dead = false;
        target.killTag = null;
        target.killTagCounter = 0;

        abstractCreature.abstractAI?.SetDestination(abstractCreature.pos);

        if (target is Player player)
        {
            player.playerState.alive = true;
            player.playerState.permaDead = false;

            player.airInLungs = 0.1f;
            player.exhausted = true;
            player.aerobicLevel = 1f;

            player.room?.game.cameras[0].hud?.textPrompt?.gameOverMode = false;
        }

        target.Stun(target is Player ? (int)(RevivalStun * 0.5f) : RevivalStun);

        Logger.LogInfo($"{target} was revived!");

        return true;
    }

    /// <summary>
    /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
    /// </summary>
    /// <param name="oracle">The iterator to de-ascend.</param>
    /// <remarks>But why would you?</remarks>
    public static bool ReviveOracle(Oracle oracle)
    {
        if (!CanReviveOracle(oracle))
        {
            Logger.LogWarning($"Cannot revive oracle {GetOracleName(oracle.ID)}!");
            return false;
        }

        StoryGameSession storySession = oracle.room.game.GetStorySession;

        if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
        {
            Custom.Log("De-Ascend saint pebbles");

            storySession.saveState.deathPersistentSaveData.ripPebbles = false;
        }
        else if (oracle.ID == Oracle.OracleID.SL)
        {
            Custom.Log("De-Ascend saint moon");

            List<OracleSwarmer> myNewSwarmers = [];

            for (int i = 0; i < 7; i++)
            {
                SLOracleSwarmer? swarmer = CreateSLOracleSwarmer(oracle);

                if (swarmer is null) continue;

                myNewSwarmers.Add(swarmer);
            }

            oracle.mySwarmers.AddRange(myNewSwarmers);

            (oracle.oracleBehavior as SLOracleBehavior)?.State.neuronsLeft = 7;

            storySession.saveState.deathPersistentSaveData.ripMoon = false;
        }

        oracle.stun = (int)(RevivalStun * 0.5f);

        oracle.health = 1f;

        Logger.LogInfo($"{GetOracleName(oracle.ID)} was revived!");

        return true;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Not as readable as a conditional expression")]
    private static bool CanReviveOracle(Oracle oracle)
    {
        if (oracle.room?.game.session is not StoryGameSession storyGame) return false;

        if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
            return storyGame.saveState.deathPersistentSaveData.ripPebbles;

        if (oracle.ID == Oracle.OracleID.SL)
            return storyGame.saveState.deathPersistentSaveData.ripMoon
            || oracle.oracleBehavior is SLOracleBehavior { State.neuronsLeft: 0 };

        return OptionUtils.IsOptionEnabled(Options.CUSTOM_ORACLE_REVIVAL) && !oracle.Consious;
    }

    /// <summary>
    /// Creates a new Neuron Fly object for Looks to the Moon.
    /// </summary>
    /// <param name="oracle">The iterator this neuron is being created for.</param>
    /// <returns>The new realized <c>SLOracleSwarmer</c> object.</returns>
    private static SLOracleSwarmer? CreateSLOracleSwarmer(Oracle oracle)
    {
        World world = oracle.room.world;

        AbstractPhysicalObject abstractSwarmer = new(
            world,
            AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer,
            null,
            oracle.abstractPhysicalObject.pos,
            world.game.GetNewID()
        );

        abstractSwarmer.RealizeInRoom();

        return abstractSwarmer?.realizedObject as SLOracleSwarmer;
    }

    /// <summary>
    /// Removes a given creature from the world's <c>respawnCreatures</c> list.
    /// </summary>
    /// <param name="creature">The creature to be removed.</param>
    public static void RemoveFromRespawnsList(Creature creature)
    {
        if (creature is Player) return;

        CreatureState state = creature.abstractCreature.state;
        EntityID ID = creature.abstractCreature.ID;

        if (state.alive && ID.spawner >= 0 && creature.room?.game.session is StoryGameSession storySession)
        {
            bool removed = storySession.saveState.respawnCreatures.Remove(ID.spawner);

            Logger.LogDebug($"Removed {creature} ({ID.spawner}) from the respawns list? {removed}");
        }
    }
}