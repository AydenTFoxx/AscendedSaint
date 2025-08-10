using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace AscendedSaint.Ascension
{
    /// <summary>
    /// A collection of utility functions for Saint's new Ascension-related abilities.
    /// </summary>
    public class ASUtils
    {
        /// <summary>
        /// Attempts to cast a given object to a type from another mod, without breaking this one.
        /// </summary>
        /// <typeparam name="T">The external type to be returned.</typeparam>
        /// <param name="obj">The object to be casted to <paramref name="obj"/>.</param>
        /// <returns>Either <paramref name="obj"/> cast to <typeparamref name="T"/>, or <typeparamref name="T"/>'s default value if the conversion fails.</returns>
        public static T CastToModdedType<T>(object obj)
        {
            T result = default;

            try
            {
                result = (T)obj;
            }
            catch (System.InvalidCastException ex)
            {
                Debug.LogErrorFormat($"{nameof(obj)} should be a boxed {nameof(T)} object.", ex);
            }

            return result;
        }

        /// <summary>
        /// Ascends or returns a creature back from life, depending on whether it was dead beforehand.
        /// </summary>
        /// <param name="creature">The creature to be ascended or revived.</param>
        /// <returns><c>true</c> if the creature was successfully modified, <c>false</c> otherwise.</returns>
        public static bool AscendCreature(Creature creature)
        {
            Room room = creature.room;
            Vector2 pos = creature.mainBodyChunk.pos;

            float revivalHealthFactor = ASOptions.REVIVAL_HEALTH_FACTOR.Value * 0.01f;

            bool didPerformAscension = false;

            if (creature.dead)
            {
                Debug.Log("Return! " + creature.Template.name);

                room.AddObject(new ShockWave(pos, 200f, 0.5f, 30));
                room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

                room.PlaySound(SoundID.Firecracker_Bang, creature.mainBodyChunk, loop: false, 1f, 0.5f + Random.value);
                room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, creature.mainBodyChunk, loop: false, 1f, 1.25f + Random.value * 1.25f);

                if (AscendedSaintMain.IsMeadowEnabled())
                {
                    didPerformAscension = true;
                }
                else
                {
                    ReviveCreature(creature, revivalHealthFactor);
                }

                creature.Stun(120);

                if (!(creature is Player)) RemoveFromRespawnsList(creature);
            }
            else
            {
                Debug.Log("Ascend! " + creature.Template.name);

                room.AddObject(new ShockWave(pos, 150f, 0.25f, 20));

                creature.Die();

                didPerformAscension = true;
            }

            return didPerformAscension;
        }

        /// <summary>
        /// Attempts to revive a given physical object if it is an <c>Oracle</c>. Otherwise, attempts to ascend or revive this same object if it is a <c>Creature</c> instead.
        /// </summary>
        /// <param name="physicalObject">The object instance to be revived.</param>
        /// <returns><c>true</c> if the object was successfully modified, <c>false</c> otherwise.</returns>
        public static bool AscendCreature(PhysicalObject physicalObject)
        {
            Room room = physicalObject.room;
            BodyChunk mainBodyChunk = physicalObject.bodyChunks[0];

            bool didPerformAscension = false;

            if (physicalObject is Oracle oracle)
            {
                Debug.Log("Return, Iterator! " + oracle.ID);

                room.AddObject(new ShockWave(mainBodyChunk.pos, 350f, 0.75f, 24));
                room.AddObject(new Explosion.ExplosionLight(mainBodyChunk.pos, 320f, 1f, 5, Color.white));

                room.PlaySound(SoundID.Firecracker_Bang, mainBodyChunk, loop: false, 1f, 1.5f + Random.value);
                room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, mainBodyChunk, loop: false, 1f, 0.5f + Random.value * 0.5f);

                ReviveOracle(oracle);

                didPerformAscension = true;
            }
            else if (physicalObject is Creature)
            {
                didPerformAscension = AscendCreature(physicalObject as Creature);
            }
            else
            {
                Debug.LogWarning("Cannot ascend or revive this! " + physicalObject.ToString());
            }

            return didPerformAscension;
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
                    return storyGame.saveState.deathPersistentSaveData.ripMoon;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
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
        protected static void RemoveFromRespawnsList(Creature creature)
        {
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
        /// <param name="creature">The  creature to be revived.</param>
        /// <param name="health">The health to be restored for the newly revived creature. Slugcats ignore this setting and are always restored to full health instead.</param>
        protected static void ReviveCreature(Creature creature, float health = 1f)
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
        }

        /// <summary>
        /// Revives a given iterator, rebinding them to the shackles of the Great Cycle once more. Unfinished business I'd say.
        /// </summary>
        /// <param name="oracle">The iterator to de-ascend.</param>
        /// <remarks>But why would you?</remarks>
        protected static void ReviveOracle(Oracle oracle)
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

                List<OracleSwarmer> myNewSwarmers = new List<OracleSwarmer> { };

                for (int i = 0; i < 7; i++)
                {
                    SLOracleSwarmer swarmer = CreateSLOracleSwarmer(oracle);

                    if (swarmer == null) continue;

                    myNewSwarmers.Add(swarmer);
                }

                if (myNewSwarmers.Count == 0) return;

                oracle.mySwarmers.AddRange(myNewSwarmers);

                storyGame.saveState.deathPersistentSaveData.ripMoon = false;
            }
            else
            {
                Debug.LogWarning("Unknown Oracle has been revived: " + oracle.ID);
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
        protected static SLOracleSwarmer CreateSLOracleSwarmer(Oracle oracle)
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
                Debug.LogWarning("Failed to create Moon's Neuron Fly!");

                return null;
            }

            return abstractSwarmer.realizedObject as SLOracleSwarmer;
        }

        /// <summary>
        /// Base class for implementing hooks to Saint's <c>ClassMechanicsSaint</c> method.
        /// </summary>
        public abstract class SaintMechanicsHook
        {
            // Lowering this value makes it harder to target creatures (especially lizards) in-game.
            protected float karmicBurstRadius = 40f;

            public abstract void ClassMechanicsSaintHook(On.Player.orig_ClassMechanicsSaint orig, Player self);
        }
    }
}