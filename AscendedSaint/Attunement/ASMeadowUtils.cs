using RainMeadow;
using System;
using static AscendedSaint.Attunement.ASUtils;
using static AscendedSaint.AscendedSaintMain.Utils;

namespace AscendedSaint.Attunement
{
    /// <summary>
    /// A set of Meadow-compatible utility functions to ensure proper sync across all clients.
    /// </summary>
    public static class ASMeadowUtils
    {
        private static ASOptions.ClientOptions ClientOptions => GetClientOptions();

        /// <summary>
        /// Applies Meadow-specific hooks to the game. In particular, this is used to trigger settings sync upon joining a lobby.
        /// </summary>
        public static void ApplyMeadowHooks()
        {
            On.GameSession.ctor += GameSessionHook;
        }

        /// <summary>
        /// Removes all Meadow-specific hooks from the game.
        /// </summary>
        public static void RemoveMeadowHooks()
        {
            On.GameSession.ctor -= GameSessionHook;
        }

        /// <summary>
        /// Initializes or updates the client's settings as a new <c>SharedOptions</c> object.
        /// </summary>
        /// <remarks>If the client has joined an online lobby, a request is instead sent to the host to send its own settings to the player.</remarks>
        private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            if (OnlineManager.lobby == null)
            {
                ASLogger.LogDebug("Game session is not an online game, ignoring.");

                return;
            }

            if (OnlineManager.lobby.isOwner)
            {
                ASLogger.LogDebug("Player is host, creating new SharedOptions object with REMIX settings.");

                SetClientOptions(new SharedOptions());
            }
            else
            {
                OnlinePlayer hostPlayer = OnlineManager.lobby.owner;

                ASLogger.LogDebug($"Requesting host player ({hostPlayer.GetUniqueID()}) for new SharedOptions object...");

                hostPlayer.InvokeRPC(typeof(ASRPCs).GetMethod("RequestRemixSync").CreateDelegate(typeof(Action<RPCEvent, OnlinePlayer>)), OnlineManager.mePlayer);
            }
        }

        /// <summary>
        /// Obtains the <c>WorldSession</c> instance where the given <c>PhysicalObject</c> is found at.
        /// </summary>
        /// <param name="physicalObject">The physical object to be searched.</param>
        /// <returns>The object's <c>WorldSession</c> instance, or <c>null</c> if none is found.</returns>
        internal static WorldSession GetWorldSession(PhysicalObject physicalObject)
        {
            WorldSession worldSession = null;

            foreach (WorldSession session in OnlineManager.lobby.worldSessions.Values)
            {
                if (session.world == physicalObject.room.world)
                {
                    worldSession = session;
                    break;
                }
            }

            if (worldSession == null)
            {
                ASLogger.LogWarning($"Failed to obtain the WorldSession instance of {physicalObject}!");
            }

            return worldSession;
        }

        /// <summary>
        /// Attempts to revive a given creature in a Meadow-compatible context.
        /// </summary>
        /// <param name="physicalObject">The creature to be revived.</param>
        /// <param name="revivalMethod">The fallback revival method to be called. If the client owns the revived entity or the game session is not an online lobby, this is called instead.</param>
        public static void TryReviveCreature(PhysicalObject physicalObject, Action revivalMethod)
        {
            if (OnlineManager.lobby != null)
            {
                WorldSession worldSession = GetWorldSession(physicalObject);

                foreach (OnlineEntity entity in worldSession.activeEntities)
                {
                    if (!(entity is OnlinePhysicalObject onlineObject)
                        || onlineObject.apo.realizedObject != physicalObject) continue;

                    if (entity.owner == OnlineManager.mePlayer)
                    {
                        ASLogger.LogDebug($"Player owns {entity.id}, calling revival method.");

                        revivalMethod.Invoke();
                    }
                    else
                    {
                        ASLogger.LogDebug($"Requesting owner of {entity.id} to run revival method.");

                        entity.owner.InvokeRPC(typeof(ASRPCs).GetMethod("SyncCreatureRevival").CreateDelegate(typeof(Action<RPCEvent, OnlinePhysicalObject>)), onlineObject);
                    }

                    break;
                }
            }
            else revivalMethod.Invoke();
        }

        /// <summary>
        /// Attempts to request the lobby owner to remove the given creature from the world's respawn list.
        /// </summary>
        /// <param name="creature">The creature to be removed.</param>
        /// <returns><c>true</c> if the request was successfully sent, <c>false</c> otherwise.</returns>
        public static bool TryRemoveCreatureRespawn(Creature creature)
        {
            if (OnlineManager.lobby == null || OnlineManager.lobby.isOwner) return false;

            OnlineManager.lobby.owner.InvokeRPC(typeof(ASRPCs).GetMethod("SyncRemoveFromRespawnsList").CreateDelegate(typeof(Action<RPCEvent, Creature>)), creature);

            return true;
        }

        /// <summary>
        /// Custom events sent to or received in order to properly sync settings and the mod's behavior.
        /// </summary>
        public class ASRPCs
        {
            /// <summary>
            /// Sets the client's settings to those received from the host.
            /// </summary>
            /// <param name="options">The options object sent by the host player.</param>
            /// <remarks>This event is sent from the host player to the client upon joining a lobby.</remarks>
            [RPCMethod]
            public static void SyncRemixSettings(SharedOptions options)
            {
                if (OnlineManager.lobby.isOwner) return;

                ClientOptions.allowRevival = options.allowRevival;
                ClientOptions.allowSelfAscension = options.allowSelfAscension;
                ClientOptions.requireKarmaFlower = options.requireKarmaFlower;
                ClientOptions.revivalHealthFactor = options.revivalHealthFactor;

                ASLogger.LogInfo("Synced REMIX settings with client!");
            }

            /// <summary>
            /// Requests the host player to sync its settings with the client.
            /// </summary>
            /// <param name="callingPlayer">The player who requested the sync.</param>
            /// <remarks>This event is sent from the client player to the host upon joining a lobby.</remarks>
            [RPCMethod]
            public static void RequestRemixSync(OnlinePlayer callingPlayer)
            {
                if (!OnlineManager.lobby.isOwner) return;

                ASLogger.LogInfo($"Received request for REMIX settings sync! Sending data to player {callingPlayer.GetUniqueID()}...");

                callingPlayer.InvokeRPC(typeof(ASRPCs).GetMethod("SyncRemixSettings").CreateDelegate(typeof(Action<RPCEvent, SharedOptions>)), ClientOptions);
            }

            /// <summary>
            /// Updates a revived creature from its owner to all subscribed players.
            /// </summary>
            /// <param name="revivedCreature">The creature who was revived.</param>
            /// <remarks>While creature revival is usually synced on its own, iterators and especially players need special handling for proper sync.</remarks>
            [RPCMethod]
            public static void SyncCreatureRevival(OnlinePhysicalObject revivedCreature)
            {
                PhysicalObject physicalObject = revivedCreature.apo.realizedObject;

                if (physicalObject is Creature creature)
                {
                    ASLogger.LogInfo($"{creature.Template.name} was revived!");

                    ReviveCreature(creature, ClientOptions.revivalHealthFactor);
                }
                else if (physicalObject is Oracle oracle)
                {
                    ASLogger.LogInfo($"{GetOracleName(oracle.ID)} was revived!");

                    ReviveOracle(oracle);
                }
                else
                {
                    ASLogger.LogWarning($"Expected creature or iterator revived, got: {physicalObject}");
                }
            }

            /// <summary>
            /// Removes a creature from the world's respawn list. A variant of <c>RemoveFromRespawnsList</c> which can be sent as a RPC event.
            /// </summary>
            /// <param name="creature"></param>
            /// <seealso cref="RemoveFromRespawnsList(Creature)"/>
            [RPCMethod]
            public static void SyncRemoveFromRespawnsList(OnlineCreature creature)
            {
                CreatureState state = creature.abstractCreature.state;
                EntityID ID = creature.abstractCreature.ID;

                GameSession gameSession = creature.realizedCreature.room.game.session;

                if (state.alive && ID.spawner >= 0 && gameSession is StoryGameSession storySession)
                {
                    storySession.saveState.respawnCreatures.Remove(ID.spawner);
                }

                ASLogger.LogInfo($"{creature} has been removed from the respawns list!");
            }

            /// <summary>
            /// Obtains an iterator's full name based on its ID.
            /// </summary>
            /// <param name="oracleID">The oracle ID to be tested.</param>
            /// <returns>The iterator's name (e.g. <c>Five Pebbles</c>), or the string <c>Unknown Iterator (<paramref name="oracleID"/>)</c> if a specific name couldn't be determined.</returns>
            /// <remarks>Custom iterators should only be added here if they are accessible and ascendable in Saint's campaign.</remarks>
            private static string GetOracleName(Oracle.OracleID oracleID)
            {
                if (oracleID == Oracle.OracleID.SL)
                {
                    return "Looks to the Moon";
                }
                else if (oracleID == Oracle.OracleID.SS)
                {
                    return "Five Pebbles";
                }
                else return $"Unknown Iterator ({oracleID})";
            }
        }

        /// <summary>
        /// A serializable variant of <c>ClientOptions</c>, for usage in an online context.
        /// </summary>
        public class SharedOptions : ASOptions.ClientOptions, Serializer.ICustomSerializable
        {
            public SharedOptions()
            {
                RefreshOptions();
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref ClientOptions.allowRevival);
                serializer.Serialize(ref ClientOptions.allowSelfAscension);
                serializer.Serialize(ref ClientOptions.requireKarmaFlower);
                serializer.Serialize(ref ClientOptions.revivalHealthFactor);
            }
        }
    }
}