using System.Collections.Generic;
using ModLib.Options;
using MoreSlugcats;
using RWCustom;

namespace AscendedSaint.Features;

public static class RevivalFeature
{
    public static void ReviveCreature(Creature target, float targetHealth = 1f)
    {
        if (!target.dead || targetHealth <= 0f) return;

        AbstractCreature abstractCreature = target.abstractCreature;

        if (abstractCreature.state is not HealthState healthState)
            healthState = new HealthState(abstractCreature);

        healthState.alive = true;
        healthState.health = target is Player ? 1f : targetHealth;

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

            if (player == player.room.game.FirstRealizedPlayer
                && player.room.game.cameras?[0].hud?.textPrompt is not null)
            {
                player.room.game.cameras[0].hud.textPrompt.gameOverMode = false;
            }
        }
        else
        {
            RemoveFromRespawnsList(target);
        }

        target.Stun(target is Player ? 40 : 100);

        ModLib.Logger.LogInfo($"{target} was revived!");
    }

    /// <summary>
    /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
    /// </summary>
    /// <param name="oracle">The iterator to de-ascend.</param>
    /// <remarks>But why would you?</remarks>
    public static void ReviveOracle(Oracle oracle)
    {
        if (!CanReviveOracle(oracle)) return;

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

        oracle.health = 1f;

        ModLib.Logger.LogInfo($"{GetOracleName(oracle.ID)} was revived!");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Not as readable as a conditional expression")]
    public static bool CanReviveOracle(Oracle oracle)
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
    private static void RemoveFromRespawnsList(Creature creature)
    {
        CreatureState state = creature.abstractCreature.state;
        EntityID ID = creature.abstractCreature.ID;

        if (state.alive && ID.spawner >= 0 && creature.room?.game.session is StoryGameSession storySession)
        {
            storySession.saveState.respawnCreatures.Remove(ID.spawner);
        }

        ModLib.Logger.LogDebug($"Removed {creature} ({ID.spawner}) from the respawns list.");
    }
}