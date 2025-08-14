using RainMeadow;
using System;
using static AscendedSaint.Attunement.ASUtils;
using static AscendedSaint.AscendedSaintMain;

namespace AscendedSaint.Attunement
{
    /// <summary>
    /// A set of Meadow-compatible utility functions to ensure proper sync across all clients.
    /// </summary>
    public static class ASMeadowUtils
    {
        /// <summary>
        /// Applies Meadow-specific hooks to the game. In particular, this is used to trigger settings sync upon joining a lobby.
        /// </summary>
        public static void ApplyMeadowHooks()
        {
            On.GameSession.ctor += MeadowGameSessionHook;
        }

        /// <summary>
        /// Removes all Meadow-specific hooks from the game.
        /// </summary>
        public static void RemoveMeadowHooks()
        {
            On.GameSession.ctor -= MeadowGameSessionHook;
        }

        /// <summary>
        /// Initializes or updates the client's settings as a new <c>SharedOptions</c> object.
        /// </summary>
        /// <remarks>If the client has joined an online lobby, a request is instead sent to the host to send its own settings to the player.</remarks>
        private static void MeadowGameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            if (OnlineManager.lobby is null)
            {
                ASLogger.LogDebug("Game session is not an online game, redirecting to vanilla hook.");

                VanillaGameSessionHook(orig, self, game);

                return;
            }

            orig.Invoke(self, game);

            if (OnlineManager.lobby.isOwner)
            {
                ASLogger.LogDebug("Player is host, creating new SharedOptions object with REMIX settings.");

                ClientOptions = new SharedOptions();

                ASLogger.LogDebug($"Shared options are: {ClientOptions}");
            }
            else
            {
                OnlinePlayer hostPlayer = OnlineManager.lobby.owner;

                ASLogger.LogDebug($"Requesting host player for new SharedOptions object...");

                hostPlayer.InvokeOnceRPC(typeof(ASRPCs).GetMethod("RequestRemixSync").CreateDelegate(typeof(Action<OnlinePlayer>)), OnlineManager.mePlayer);
            }
        }

        /// <summary>
        /// Logs a message to Rain Meadow's chat (as the system) and to this mod's log file.
        /// </summary>
        /// <param name="message">The message to be sent to all players.</param>
        public static void LogSystemMessage(string message)
        {
            ChatLogManager.LogSystemMessage(message);

            ASLogger.LogMessage($"-> {message}");
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
                OnlinePhysicalObject onlineObject = ASMeadowQueries.GetOnlinePhysicalObject(physicalObject);

                if (onlineObject is null)
                {
                    ASLogger.LogWarning($"Failed to retrieve the OnlineEntity version of {physicalObject}. No operation will be performed.");
                }
                else if (onlineObject.owner == OnlineManager.mePlayer)
                {
                    ASLogger.LogDebug($"Player owns {physicalObject} ({onlineObject.id}), calling revival method.");

                    revivalMethod.Invoke();
                }
                else
                {
                    ASLogger.LogDebug($"Requesting owner of {physicalObject} ({onlineObject.id}) to run revival method.");

                    onlineObject.owner.InvokeOnceRPC(typeof(ASRPCs).GetMethod("SyncCreatureRevival").CreateDelegate(typeof(Action<OnlinePhysicalObject>)), onlineObject);
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
            if (OnlineManager.lobby is null || OnlineManager.lobby.isOwner) return false;

            OnlineManager.lobby.owner.InvokeOnceRPC(typeof(ASRPCs).GetMethod("SyncRemoveFromRespawnsList").CreateDelegate(typeof(Action<Creature>)), creature);

            return true;
        }

        /// <summary>
        /// Requests all online players to sync Saint's ascension ability effects.
        /// </summary>
        /// <param name="physicalObject">The physical object which was ascended or revived.</param>
        /// <remarks>If the player is not in an online lobby, this has the same effects as calling <see cref="SpawnAscensionEffects(PhysicalObject, bool)"/></remarks>
        public static void RequestAscensionEffectsSync(PhysicalObject physicalObject, bool isRevival = true)
        {
            if (OnlineManager.lobby != null)
            {
                foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
                {
                    if (onlinePlayer.isMe) continue;

                    onlinePlayer.InvokeOnceRPC(typeof(ASRPCs).GetMethod("SyncAscensionEffects").CreateDelegate(typeof(Action<OnlinePhysicalObject>)), ASMeadowQueries.GetOnlinePhysicalObject(physicalObject));
                }
            }

            SpawnAscensionEffects(physicalObject, isRevival);
        }
    }
}