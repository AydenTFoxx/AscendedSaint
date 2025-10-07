using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MoreSlugcats;
using UnityEngine;
using static ModLib.CompatibilityManager;
using static ModLib.Options.OptionUtils;

namespace AscendedSaint.Attunement;

public static partial class AscensionHandler
{
    public static bool CanReviveObject(PhysicalObject physicalObject)
    {
        return physicalObject is Creature creature
            ? creature.dead
            : physicalObject is Oracle oracle && CanReviveOracle(oracle);
    }

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "unreadable mess")]
    public static bool CanReviveOracle(Oracle oracle)
    {
        if (oracle.room?.game.session is not StoryGameSession storyGame) return false;

        if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
            return storyGame.saveState.deathPersistentSaveData.ripPebbles;

        if (oracle.ID == Oracle.OracleID.SL)
            return storyGame.saveState.deathPersistentSaveData.ripMoon
            || oracle.oracleBehavior is SLOracleBehavior { State.neuronsLeft: 0 };

        return IsOptionEnabled(Options.CUSTOM_ORACLE_REVIVAL) && !oracle.Consious;
    }

    /// <summary>
    /// Attempts to obtain the Karma Flower held by the player.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns>A Karma Flower held by the player, or <c>null</c> if none is found.</returns>
    public static PhysicalObject? GetHeldKarmaFlower(Player player) =>
        player.grasps.FirstOrDefault(grasp => grasp?.grabbed is KarmaFlower)?.grabbed;

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

        if (abstractSwarmer.realizedObject is null)
        {
            ModLib.Logger.LogWarning("Failed to create Moon's Neuron Fly!");
        }

        return abstractSwarmer?.realizedObject as SLOracleSwarmer;
    }

    /// <summary>
    /// Removes a given creature from the world's <c>respawnCreatures</c> list.
    /// </summary>
    /// <param name="creature">The creature to be removed.</param>
    private static void RemoveFromRespawnsList(Creature creature)
    {
        if (IsRainMeadowEnabled() && MeadowHelper.TryRemoveCreatureRespawn(creature)) return;

        CreatureState state = creature.abstractCreature.state;
        EntityID ID = creature.abstractCreature.ID;

        if (state.alive && ID.spawner >= 0 && creature.room?.game.session is StoryGameSession storySession)
        {
            storySession.saveState.respawnCreatures.Remove(ID.spawner);
        }

        ModLib.Logger.LogDebug($"Removed {creature} from the respawns list.");
    }

    /// <summary>
    /// Spawns the special effects of Saint's new abilities.
    /// </summary>
    /// <param name="physicalObject">The object which was the target of this ability.</param>
    /// <param name="isRevival">If the performed ability was a revival.</param>
    public static void SpawnAscensionEffects(PhysicalObject physicalObject, bool isRevival = true)
    {
        Room room = physicalObject.room;
        Vector2 pos = physicalObject is Creature creature ? creature.mainBodyChunk.pos : physicalObject.bodyChunks[0].pos;

        if (isRevival)
        {
            BodyChunk bodyChunk = physicalObject is Creature creature1 ? creature1.mainBodyChunk : physicalObject.bodyChunks[0];
            bool isOracle = physicalObject is Oracle;

            float shockWaveSize = isOracle ? 350f : 200;
            float shockWaveIntensity = isOracle ? 0.75f : 0.5f;
            int shockWaveLifetime = isOracle ? 24 : 30;
            float firecrackerPitch = isOracle ? 1.5f : 0.5f;
            float markPitch = isOracle ? 0.5f : 1.25f;

            room.AddObject(new ShockWave(pos, shockWaveSize, shockWaveIntensity, shockWaveLifetime));
            room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

            room.PlaySound(SoundID.Firecracker_Bang, bodyChunk, loop: false, 1f, firecrackerPitch + Random.value);
            room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, bodyChunk, loop: false, 1f, markPitch + (Random.value * markPitch));
        }
        else
        {
            room.AddObject(new ShockWave(pos, 150f, 0.25f, 20));
        }

        ModLib.Logger.LogDebug($"Spawned {(isRevival ? "revival" : "ascension")} effects at {pos} for {physicalObject}.");
    }
}