using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static AscendedSaint.AscendedSaintMain.Utils;

namespace AscendedSaint.Attunement
{
    /// <summary>
    /// A collection of utility functions for Saint's new Ascension-related abilities.
    /// </summary>
    public static class ASUtils
    {
        /// <summary>
        /// The performed action on the given creature after applying the Ascension ability.
        /// </summary>
        public enum AscensionResult
        {
            /// <summary>
            /// The creature was ascended. This is only used for when the player ascends themselves.
            /// </summary>
            Ascended,
            /// <summary>
            /// The creature was revived. Used if the creature was previously dead and the player has now revived it.
            /// </summary>
            Revived,
            /// <summary>
            /// The creature was ignored; No operation was performed.
            /// </summary>
            Ignored
        }

        /// <summary>
        /// Ascends or returns a creature back from life, depending on whether it was dead beforehand.
        /// </summary>
        /// <param name="creature">The creature to be ascended or revived.</param>
        /// <returns><c>true</c> if the creature was successfully modified, <c>false</c> otherwise.</returns>
        public static AscensionResult AscendCreature(Creature creature)
        {
            AscensionResult ascensionResult;

            Vector2 pos = creature.mainBodyChunk.pos;
            Room room = creature.room;

            float revivalHealthFactor = ASOptions.REVIVAL_HEALTH_FACTOR.Value * 0.01f;

            if (creature.dead)
            {
                Debug.Log("[AS] Return! " + creature.Template.name);

                room.AddObject(new ShockWave(pos, 200f, 0.5f, 30));
                room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

                room.PlaySound(SoundID.Firecracker_Bang, creature.mainBodyChunk, loop: false, 1f, 0.5f + Random.value);
                room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, creature.mainBodyChunk, loop: false, 1f, 1.25f + Random.value * 1.25f);

                ReviveCreature(creature, revivalHealthFactor);

                if (IsOnlineMultiplayerSession())
                {
                    foreach (RainMeadow.OnlinePlayer onlinePlayer in RainMeadow.OnlineManager.players)
                    {
                        if (onlinePlayer.isMe) continue;

                        onlinePlayer.InvokeRPC(typeof(ASMeadowUtils.ASRPCs).GetMethod("UpdateRevivedCreature").CreateDelegate(typeof(System.Action<RainMeadow.RPCEvent, PhysicalObject>)), creature);
                    }
                }

                creature.Stun(120);

                if (!(creature is Player)) RemoveFromRespawnsList(creature);

                ascensionResult = AscensionResult.Revived;
            }
            else
            {
                Debug.Log("[AS] Ascend! " + creature.Template.name);

                room.AddObject(new ShockWave(pos, 150f, 0.25f, 20));

                creature.Die();

                ascensionResult = AscensionResult.Ascended;
            }

            return ascensionResult;
        }

        /// <summary>
        /// Attempts to revive a given physical object if it is an <c>Oracle</c>. Otherwise, attempts to ascend or revive this same object if it is a <c>Creature</c> instead.
        /// </summary>
        /// <param name="physicalObject">The object instance to be revived.</param>
        /// <returns><c>true</c> if the object was successfully modified, <c>false</c> otherwise.</returns>
        public static AscensionResult AscendCreature(PhysicalObject physicalObject)
        {
            AscensionResult ascensionResult = AscensionResult.Ignored;

            Room room = physicalObject.room;
            BodyChunk mainBodyChunk = physicalObject.bodyChunks[0];

            if (physicalObject is Oracle oracle)
            {
                Debug.Log("[AS] Return, Iterator! " + oracle.ID);

                room.AddObject(new ShockWave(mainBodyChunk.pos, 350f, 0.75f, 24));
                room.AddObject(new Explosion.ExplosionLight(mainBodyChunk.pos, 320f, 1f, 5, Color.white));

                room.PlaySound(SoundID.Firecracker_Bang, mainBodyChunk, loop: false, 1f, 1.5f + Random.value);
                room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, mainBodyChunk, loop: false, 1f, 0.5f + Random.value * 0.5f);

                ReviveOracle(oracle);

                if (IsOnlineMultiplayerSession())
                {
                    foreach (RainMeadow.OnlinePlayer onlinePlayer in RainMeadow.OnlineManager.players)
                    {
                        if (onlinePlayer.isMe) continue;

                        onlinePlayer.InvokeRPC(typeof(ASMeadowUtils.ASRPCs).GetMethod("UpdateRevivedCreature").CreateDelegate(typeof(System.Action<RainMeadow.RPCEvent, PhysicalObject>)), oracle);
                    }
                }

                ascensionResult = AscensionResult.Revived;
            }
            else if (physicalObject is Creature)
            {
                ascensionResult = AscendCreature(physicalObject as Creature);
            }
            else
            {
                Debug.LogWarning("[AS] Cannot ascend or revive this! " + physicalObject.ToString());
            }

            return ascensionResult;
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
        internal static void RemoveFromRespawnsList(Creature creature)
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
                Debug.LogWarning("Failed to create Moon's Neuron Fly!");

                return null;
            }

            return abstractSwarmer.realizedObject as SLOracleSwarmer;
        }
    }
}