using UnityEngine;
using RainMeadow;
using System;
using static AscendedSaint.Attunement.ASUtils;

namespace AscendedSaint.Attunement
{
    public static class ASMeadowUtils
    {
        private static ASOptions.SharedOptions clientOptions = AscendedSaintMain.instance.clientOptions;

        public static void ApplyMeadowHooks()
        {
            On.GameSession.ctor += delegate (On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
            {
                if (OnlineManager.lobby == null || OnlineManager.lobby.isOwner)
                {
                    clientOptions = new ASOptions.SharedOptions();

                    return;
                }

                OnlineManager.lobby.owner.InvokeRPC(typeof(ASRPCs).GetMethod("RequestRemixSync").CreateDelegate(typeof(Action<RPCEvent, OnlinePlayer>)), OnlineManager.mePlayer);
            };
        }

        internal class ASRPCs
        {
            [RPCMethod]
            public static void SyncRemixSettings(RPCEvent _, ASOptions.SharedOptions options)
            {
                clientOptions.allowRevival = options.allowRevival;
                clientOptions.allowSelfAscension = options.allowSelfAscension;
                clientOptions.requireKarmaFlower = options.requireKarmaFlower;
                clientOptions.revivalHealthFactor = options.revivalHealthFactor;

                Debug.Log("[AS+M] Synced REMIX settings with client!");
            }

            [RPCMethod]
            public static void RequestRemixSync(RPCEvent _, OnlinePlayer callingPlayer)
            {
                if (!OnlineManager.lobby.isOwner) return;

                Debug.Log($"[AS+M] Received request for REMIX settings sync! Sending data to player {callingPlayer.GetUniqueID()}...");

                callingPlayer.InvokeRPC(typeof(ASRPCs).GetMethod("SyncRemixSettings").CreateDelegate(typeof(Action<RPCEvent, ASOptions.SharedOptions>)), clientOptions);
            }

            [RPCMethod]
            public static void UpdateRevivedCreature(RPCEvent _, PhysicalObject physicalObject)
            {
                if (physicalObject is Creature creature)
                {
                    Debug.Log($"[AS+M] {creature.Template.name} was revived!");

                    ReviveCreature(creature, clientOptions.revivalHealthFactor);
                }
                else if (physicalObject is Oracle oracle)
                {
                    Debug.Log($"[AS+M] {GetOracleName(oracle.ID)} was revived!");

                    ReviveOracle(oracle);
                }
                else
                {
                    Debug.LogWarning($"[AS+M] Expected creature or iterator revived, got: {physicalObject}");
                }
            }

            private static string GetOracleName(Oracle.OracleID oracleID)
            {
                if (oracleID == Oracle.OracleID.SL)
                {
                    return "Looks to The Moon";
                }
                else if (oracleID == Oracle.OracleID.SS)
                {
                    return "Five Pebbles";
                }
                else
                    return $"Unknown Iterator ({oracleID})";
            }
        }
    }
}