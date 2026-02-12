using System.Linq;
using ModLib.Options;
using MoreSlugcats;
using RWCustom;

namespace AscendedSaint.Utils;

public static class RevivalHelper
{
    private static float RevivalHealth => OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR) * 0.01f;
    private static int RevivalStun => OptionUtils.GetOptionValue(Options.REVIVAL_STUN_DURATION);

    /// <summary>
    /// Creates and grants a new Neuron Fly object for the given iterator (usually Looks to the Moon).
    /// </summary>
    /// <param name="oracle">The iterator this neuron is being created for.</param>
    public static void AddOracleSwarmer(Oracle oracle)
    {
        World world = oracle.room.world;
        AbstractPhysicalObject.AbstractObjectType swarmerType = oracle.ID == Oracle.OracleID.SL
            ? AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer
            : AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer;

        AbstractPhysicalObject abstractSwarmer = new(
            world,
            swarmerType,
            null,
            oracle.abstractPhysicalObject.pos,
            world.game.GetNewID()
        );

        abstractSwarmer.RealizeInRoom();

        if (abstractSwarmer.realizedObject is not OracleSwarmer realizedSwarmer)
        {
            Main.Logger.LogWarning($"Failed to realize OracleSwarmer for {oracle}! Destroying created abstract object.");

            abstractSwarmer.Destroy();
            abstractSwarmer.realizedObject?.Destroy();
            return;
        }

        oracle.mySwarmers.Add(realizedSwarmer);

        if (oracle.ID == Oracle.OracleID.SL)
            oracle.glowers++;

        if (oracle.oracleBehavior is SLOracleBehavior oracleBehavior)
            oracleBehavior.State.neuronsLeft++;
    }

    /// <summary>
    /// Obtains an iterator's full name based on its ID.
    /// </summary>
    /// <param name="oracleID">The oracle ID to be tested.</param>
    /// <returns>The iterator's name (e.g. <c>Five Pebbles</c>), or the string <c>Unknown Iterator (<paramref name="oracleID"/>)</c> if a specific name couldn't be determined.</returns>
    /// <remarks>Custom iterators should only be added here if they are accessible and ascendable in Saint's campaign.</remarks>
    public static string GetOracleName(Oracle.OracleID oracleID) =>
        oracleID == Oracle.OracleID.SL
        ? "Looks to the Moon"
        : oracleID == MoreSlugcatsEnums.OracleID.CL // Oracle.OracleID.SS is not checked since FP is not ascendable outside Saint's campaign
            ? "Five Pebbles"
            : oracleID == MoreSlugcatsEnums.OracleID.ST // Should not occur under normal circumstances, but just for the funnies
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
    public static bool ReviveCreature(Creature? target)
    {
        if (target is null) return false;

        if (!target.dead)
        {
            Main.Logger.LogWarning($"{target} cannot be revived, as it's already alive!");
            return false;
        }

        AbstractCreature abstractCreature = target.abstractCreature;

        if (abstractCreature.state is HealthState healthState)
        {
            healthState.alive = true;
            healthState.health = target is Player ? 1f : RevivalHealth;
        }

        target.dead = false;
        target.killTag = null;
        target.killTagCounter = 0;

        abstractCreature.abstractAI?.SetDestination(abstractCreature.pos);

        if (target is Player player)
        {
            if (player.playerState is not null)
            {
                player.playerState.alive = true;
                player.playerState.permaDead = false;
            }

            player.airInLungs = 0.1f;
            player.exhausted = true;
            player.aerobicLevel = 1f;

            player.room?.game.cameras[0].hud?.textPrompt?.gameOverMode = false;
        }

        target.Stun(target is Player ? (int)(RevivalStun * 0.5f) : RevivalStun);

        Main.Logger.LogInfo($"{target} was revived!");

        return true;
    }

    /// <summary>
    /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
    /// </summary>
    /// <param name="oracle">The iterator to de-ascend.</param>
    /// <remarks>But why would you?</remarks>
    public static bool ReviveOracle(Oracle? oracle)
    {
        if (oracle is null || !CanReviveOracle(oracle)) return false;

        StoryGameSession storySession = oracle.room.game.GetStorySession;

        if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
        {
            Custom.Log("De-Ascend saint pebbles");

            if (oracle.oracleBehavior is CLOracleBehavior pebblesBehavior)
            {
                pebblesBehavior.Pain();

                pebblesBehavior.halcyon = oracle.room.physicalObjects.SelectMany(static objs => objs.Where(static obj => obj is HalcyonPearl)).FirstOrDefault() as HalcyonPearl;

                Main.Logger.LogDebug($"Halcyon porl: {pebblesBehavior.halcyon}");
            }

            storySession.saveState.miscWorldSaveData.halcyonStolen = false;

            storySession.saveState.deathPersistentSaveData.ripPebbles = false;
        }
        else if (oracle.ID == Oracle.OracleID.SL)
        {
            Custom.Log("De-Ascend saint moon");

            for (int i = 0; i < 7; i++)
            {
                AddOracleSwarmer(oracle);
            }

            if (oracle.oracleBehavior is SLOracleBehavior moonBehavior)
            {
                moonBehavior.State.neuronsLeft = 7;

                moonBehavior.Pain();

                moonBehavior.wasScaredBySingularity = true;
                moonBehavior.SingularityProtest();
            }

            storySession.saveState.deathPersistentSaveData.ripMoon = false;
        }
        else
        {
            Main.Logger.LogWarning($"Reviving unknown oracle: {oracle.ID}; Custom Oracle revival is not supported and may result in undefined behavior!");
        }

        oracle.stun = (int)(RevivalStun * 0.5f);

        oracle.health = 1f;

        Main.Logger.LogInfo($"{GetOracleName(oracle.ID)} was revived!");

        return true;
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

            Main.Logger.LogDebug($"Removed {creature} ({ID.spawner}) from the respawns list? {removed}");
        }
    }

    private static bool CanReviveOracle(Oracle oracle)
    {
        return oracle.room?.game.session is StoryGameSession storyGame
            && (oracle.ID == MoreSlugcatsEnums.OracleID.CL || oracle.ID == Oracle.OracleID.SS
                ? storyGame.saveState.deathPersistentSaveData.ripPebbles
                : oracle.ID == Oracle.OracleID.SL
                    ? storyGame.saveState.deathPersistentSaveData.ripMoon
                        || oracle.oracleBehavior is SLOracleBehavior { State.neuronsLeft: 0 }
                    : OptionUtils.IsOptionEnabled(Options.CUSTOM_ORACLE_REVIVAL) && !oracle.Alive);
    }
}