using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static AscendedSaint.AscendedSaintMain;

namespace AscendedSaint.Attunement
{
    /// <summary>
    /// A collection of utility functions for Saint's new Ascension-related abilities.
    /// </summary>
    public static class ASUtils
    {
        private static readonly ASOptions.ClientOptions ClientOptions = AscendedSaintMain.ClientOptions;

        /// <summary>
        /// Ascends or returns a creature back from life, depending on whether it was dead beforehand.
        /// </summary>
        /// <param name="creature">The creature to be ascended or revived.</param>
        /// <returns><c>true</c> if the creature was successfully modified, <c>false</c> otherwise.</returns>
        public static void AscendCreature(Creature creature)
        {
            Vector2 pos = creature.mainBodyChunk.pos;
            Room room = creature.room;

            if (creature.dead)
            {
                ASLogger.LogInfo("Return! " + creature.Template.name);

                if (Utils.IsMeadowEnabled())
                {
                    ASMeadowUtils.TryReviveCreature(creature, () => ReviveCreature(creature, ClientOptions.revivalHealthFactor));
                    ASMeadowUtils.RequestAscensionEffectsSync(creature);

                    ASMeadowUtils.LogSystemMessage($"{creature.Template.name} was revived by {ASMeadowUtils.PlayerName}.");
                }
                else
                {
                    ReviveCreature(creature, ClientOptions.revivalHealthFactor);

                    SpawnAscensionEffects(creature);
                }

                creature.Stun(120);
            }
            else
            {
                ASLogger.LogInfo("Ascend! " + creature.Template.name);

                creature.Die();

                if (Utils.IsMeadowEnabled())
                {
                    ASMeadowUtils.RequestAscensionEffectsSync(creature);

                    ASMeadowUtils.LogSystemMessage($"{ASMeadowUtils.PlayerName} self-ascended.");
                }
                else
                {
                    SpawnAscensionEffects(creature, isRevival: false);
                }
            }
        }

        /// <summary>
        /// Attempts to revive a given physical object if it is an <c>Oracle</c>. Otherwise, attempts to ascend or revive this same object if it is a <c>Creature</c> instead.
        /// </summary>
        /// <param name="physicalObject">The object instance to be revived.</param>
        /// <returns><c>true</c> if the object was successfully modified, <c>false</c> otherwise.</returns>
        public static void AscendCreature(PhysicalObject physicalObject)
        {
            Room room = physicalObject.room;
            BodyChunk mainBodyChunk = physicalObject.bodyChunks[0];

            if (physicalObject is Oracle oracle)
            {
                ASLogger.LogInfo("Return, Iterator! " + oracle.ID);

                if (Utils.IsMeadowEnabled())
                {
                    ASMeadowUtils.TryReviveCreature(physicalObject, () => ReviveOracle(oracle));
                    ASMeadowUtils.RequestAscensionEffectsSync(oracle);

                    ASMeadowUtils.LogSystemMessage($"{Utils.GetOracleName(oracle.ID)} was revived by {ASMeadowUtils.PlayerName}.");
                }
                else
                {
                    ReviveOracle(oracle);

                    SpawnAscensionEffects(oracle);
                }
            }
            else if (physicalObject is Creature)
            {
                AscendCreature(physicalObject as Creature);
            }
            else
            {
                ASLogger.LogWarning("Cannot ascend or revive this! " + physicalObject.ToString());
            }
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

            ASLogger.LogDebug($"Spawned revival effects at {pos} for {physicalObject}.");
        }

        /// <summary>
        /// Determines whether a creature or iterator can be revived.
        /// </summary>
        /// <param name="physicalObject">The creature or iterator to be tested.</param>
        /// <returns><c>true</c> if the target creature can be revived, <c>false</c> otherwise.</returns>
        public static bool CanReviveCreature(PhysicalObject physicalObject)
        {
            if (physicalObject is Creature creature)
            {
                return creature.dead;
            }
            else if (physicalObject is Oracle oracle)
            {
                if (!(oracle.room.game.session is StoryGameSession storyGame)) return false;

                if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
                {
                    return storyGame.saveState.deathPersistentSaveData.ripPebbles;
                }
                else if (oracle.ID == Oracle.OracleID.SL)
                {
                    return storyGame.saveState.deathPersistentSaveData.ripMoon
                        || (oracle.oracleBehavior as SLOracleBehavior).State.neuronsLeft == 0;
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to obtain the Karma Flower held by a given creature.
        /// </summary>
        /// <param name="creature">The creature to be tested.</param>
        /// <returns>A Karma Flower held by the creature, or <c>null</c> if none is found.</returns>
        public static PhysicalObject GetHeldKarmaFlower(Creature creature)
        {
            foreach (Creature.Grasp grasp in creature.grasps)
            {
                if (grasp is null) continue;

                if (grasp.grabbed is KarmaFlower) return grasp.grabbed;
            }

            return null;
        }

        /// <summary>
        /// Removes a given creature from the world's <c>respawnCreatures</c> list.
        /// </summary>
        /// <param name="creature">The creature to be removed.</param>
        internal static void RemoveFromRespawnsList(Creature creature)
        {
            if (Utils.IsMeadowEnabled() && ASMeadowUtils.TryRemoveCreatureRespawn(creature)) return;

            CreatureState state = creature.abstractCreature.state;
            EntityID ID = creature.abstractCreature.ID;

            if (state.alive && ID.spawner >= 0 && creature.room.game.session is StoryGameSession storySession)
            {
                storySession.saveState.respawnCreatures.Remove(ID.spawner);
            }
        }

        /// <summary>
        /// Restores a creature's health and sets its state as "alive" once again.
        /// </summary>
        /// <param name="creature">The creature to be revived.</param>
        /// <param name="health">The health to be restored for the newly revived creature. Slugcats ignore this setting and are always restored to full health instead.</param>
        internal static void ReviveCreature(Creature creature, float health = 1f)
        {
            AbstractCreature abstractCreature = creature.abstractCreature;

            if (!(abstractCreature.state is HealthState healthState))
                healthState = new HealthState(abstractCreature);

            creature.dead = false;
            creature.killTag = null;
            creature.killTagCounter = 0;

            healthState.alive = true;
            healthState.health = creature is Player ? 1f : health;

            abstractCreature.abstractAI?.SetDestination(abstractCreature.pos);

            if (creature is Player player)
            {
                player.playerState.alive = true;
                player.playerState.permaDead = false;

                player.airInLungs = 0.1f;
                player.exhausted = true;
                player.aerobicLevel = 1f;
            }
            else
            {
                RemoveFromRespawnsList(creature);
            }
        }

        /// <summary>
        /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
        /// </summary>
        /// <param name="oracle">The iterator to de-ascend.</param>
        /// <remarks>But why would you?</remarks>
        internal static void ReviveOracle(Oracle oracle)
        {
            Room room = oracle.room;

            if (!(room.game.session is StoryGameSession storyGame)) return;

            if (oracle.ID == MoreSlugcatsEnums.OracleID.CL)
            {
                Custom.Log("De-Ascend saint pebbles");

                storyGame.saveState.deathPersistentSaveData.ripPebbles = false;
            }
            else if (oracle.ID == Oracle.OracleID.SL)
            {
                Custom.Log("De-Ascend saint moon");

                List<OracleSwarmer> myNewSwarmers = new List<OracleSwarmer>();

                for (int i = 0; i < 7; i++)
                {
                    myNewSwarmers.Add(CreateSLOracleSwarmer(oracle));
                }

                oracle.mySwarmers.AddRange(myNewSwarmers);

                (oracle.oracleBehavior as SLOracleBehavior).State.neuronsLeft = 7;

                storyGame.saveState.deathPersistentSaveData.ripMoon = false;
            }
            else
            {
                ASLogger.LogWarning("Unknown Oracle has been revived: " + oracle.ID);
            }

            Vector2 pos = oracle.bodyChunks[0].pos;
            room.AddObject(new ShockWave(pos, 500f, 0.75f, 18));
            room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

            oracle.health = 1f;
        }

        /// <summary>
        /// Creates a new Neuron Fly object for Looks to the Moon.
        /// </summary>
        /// <param name="oracle">The iterator this neuron is being created for.</param>
        /// <returns>The new realized <c>SLOracleSwarmer</c> object.</returns>
        private static SLOracleSwarmer CreateSLOracleSwarmer(Oracle oracle)
        {
            World world = oracle.room.world;

            AbstractPhysicalObject abstractSwarmer = new AbstractPhysicalObject(
                world,
                AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer,
                null,
                oracle.abstractPhysicalObject.pos,
                world.game.GetNewID()
            );

            abstractSwarmer.RealizeInRoom();

            if (!(abstractSwarmer.realizedObject is SLOracleSwarmer))
            {
                ASLogger.LogWarning("Failed to create Moon's Neuron Fly!");

                return null;
            }

            return abstractSwarmer.realizedObject as SLOracleSwarmer;
        }
    }
}