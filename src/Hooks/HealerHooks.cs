using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AscendedSaint.Attunement;
using AscendedSaint.Healing;
using AscendedSaint.Utils;
using ModLib;
using ModLib.Objects;
using ModLib.Meadow;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace AscendedSaint.Hooks;

internal static class HealerHooks
{
    public static void ApplyHooks()
    {
        IL.Player.ClassMechanicsSaint += ReplaceAscensionWithHealingILHook;

        On.MoreSlugcats.HRGuardManager.Update += UnlockBattleRoomsAfterDelayHook;

        On.Player.Die += PreventHealerDeathHook;
    }

    public static void RemoveHooks()
    {
        IL.Player.ClassMechanicsSaint -= ReplaceAscensionWithHealingILHook;

        On.MoreSlugcats.HRGuardManager.Update += UnlockBattleRoomsAfterDelayHook;

        On.Player.Die -= PreventHealerDeathHook;
    }

    private static void PreventHealerDeathHook(On.Player.orig_Die orig, Player self)
    {
        if (OptionUtils.IsOptionEnabled(Options.HEALER_SAINT)
            && OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_REVIVAL)
            && self is { slatedForDeletetion: false, killTag: not null, room: not null, room.game.IsStorySession: true, room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma: true }
            && TryPreventDeath(self))
        {
            return;
        }

        orig.Invoke(self);

        static bool IsOnSafePos(Creature target)
        {
            Room.Tile tile = target.room.GetTile(target.bodyChunks[1].pos);

            return !tile.DeepWater
                && !tile.wormGrass
                && target.IsTileSolid(1, 0, -1)
                && !target.IsTileSolid(1, 0, 0)
                && !target.IsTileSolid(1, 0, 1);
        }

        static bool TryPreventDeath(Player self)
        {
            WorldCoordinate safePos = self.abstractCreature.pos;

            if (!IsOnSafePos(self))
            {
                List<ShortcutData> shortcuts = [.. self.room.shortcuts];

                if (shortcuts.Count <= 0) return false;

                shortcuts.Sort(static (x, y) => (int)(x.StartTile - y.StartTile).ToVector2().sqrMagnitude);

                safePos = shortcuts.First().startCoord;
            }

            self.room.AddObject(new FadingMeltLights());

            ApplyAscensionEffects(self, null);

            self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = false;
            self.room.game.cameras[0].hud?.karmaMeter.reinforceAnimation = 0;

            DeathProtection.CreateInstance(self, DeathProtection.SafeLandingCondition, safePos: safePos);
            return true;
        }
    }

    private static void UnlockBattleRoomsAfterDelayHook(On.MoreSlugcats.HRGuardManager.orig_Update orig, HRGuardManager self, bool eu)
    {
        bool wasTriggered = self.triggered;

        orig.Invoke(self, eu);

        if (!OptionUtils.IsOptionEnabled(Options.HEALER_SAINT) || !self.triggered) return;

        if (!wasTriggered)
        {
            self.playerInRoomTime = 0;

            Main.Logger.LogDebug("Init Healer Saint battle room!");
        }
        else if (self.playerInRoomTime % 720 == 0)
        {
            Main.Logger.LogDebug($"Die! TempleGuard (Hits remaining: {self.hitsToKill - 1})");

            self.myGuard.Die();

            self.room.PlaySound(SoundID.Firecracker_Bang, self.myPlayer.mainBodyChunk, loop: false, 1f, 0.85f + Random.value);
            self.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, self.myPlayer.mainBodyChunk, loop: false, 1f, 0.65f + (Random.value * 0.65f));

            self.room.AddObject(new TemplarCircle(self.myGuard.realizedCreature, self.myGuard.realizedCreature.mainBodyChunk.pos, 48f, 0.25f, 0.125f, 120, false));

            for (int m = 0; m < 20; m++)
            {
                self.room.AddObject(new Spark(self.myGuard.realizedCreature.mainBodyChunk.pos, Custom.RNV() * (Random.value * 40f), new Color(1f, 1f, 1f), null, 30, 120));
            }

            foreach (Creature creature in self.room.physicalObjects.SelectMany(static list => list).OfType<Creature>())
            {
                if (creature is Player or Overseer or TempleGuard || IsPlayerFriend(creature.abstractCreature, self.myPlayer)) continue;

                creature.LoseAllGrasps();

                creature.stun = 100 + Random.Range(-20, 20);
            }

            self.myPlayer.SaintStagger(120);
        }

        if (self is { hitsToKill: <= 0, slatedForDeletetion: true })
        {
            Main.Logger.LogDebug("Cleared battle room! Spawned enemies will be removed on abstraction.");

            self.room.AddObject(new FadingMeltLights());

            DeathPersistentSaveData? saveData = self.room.game.GetStorySession?.saveState.deathPersistentSaveData;

            if (saveData is not null && saveData.karma < saveData.karmaCap)
            {
                saveData.karma++;

                self.room.game.cameras[0].hud?.karmaMeter.reinforceAnimation = 0;
            }

            foreach (Creature creature in self.room.physicalObjects.SelectMany(static list => list).OfType<Creature>())
            {
                if (creature is Player or Overseer or TempleGuard || IsPlayerFriend(creature.abstractCreature, self.myPlayer)) continue;

                creature.Die();

                creature.abstractCreature.destroyOnAbstraction = true;
            }

            foreach (AbstractCreature crit in self.room.abstractRoom.creatures)
            {
                if (crit.realizedCreature is not (Player or Overseer or TempleGuard))
                    crit.Die();
            }
        }
    }

    private static void ReplaceAscensionWithHealingILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(
            static x => x.MatchLdloc(18),
            static x => x.MatchCallvirt(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.bodyChunks)).GetGetMethod()),
            static x => x.MatchStloc(19)
        );

        // Target: foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
        //        ^ HERE (Prepend)

        ILCursor d = new(c);

        ILLabel? endOfLoopTarget = null;
        ILLabel origTarget = c.MarkLabel();

        d.GotoPrev(x => x.MatchBrfalse(out endOfLoopTarget));

        // Target: if (physicalObject != this && (physicalObject.abstractPhysicalObject.rippleLayer == this.abstractPhysicalObject.rippleLayer || physicalObject.abstractPhysicalObject.rippleBothSides || this.abstractPhysicalObject.rippleBothSides)) <-- HERE (No-op; Referenced for later use)

        c.Emit(OpCodes.Ldsfld, typeof(Options).GetField(nameof(Options.HEALER_SAINT)))
         .Emit(OpCodes.Call, typeof(OptionUtils).GetMethod(nameof(OptionUtils.IsOptionEnabled), BindingFlags.Public | BindingFlags.Static, null, [typeof(Configurable<bool>)], null))
         .Emit(OpCodes.Brfalse, origTarget)
         .Emit(OpCodes.Ldarg_0)
         .Emit(OpCodes.Ldloc, 18)
         .Emit(OpCodes.Ldloc, 15)
         .Emit(OpCodes.Call, typeof(HealerHooks).GetMethod(nameof(HealerSaintMechanics), BindingFlags.NonPublic | BindingFlags.Static))
         .Emit(OpCodes.Stloc, 15)
         .Emit(OpCodes.Br, endOfLoopTarget);

        // Result:
        //     if (OptionUtils.IsOptionEnabled(Options.HEALER_SAINT))
        //     {
        //         flag2 = HealerSaintMechanics(this, physicalObject, flag2);
        //     }
        //     else
        //     {
        //         foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
        //         {
        //             ...
        //         }
        //     }
    }

    private static void ApplyAscensionEffects(Player self, KarmaFlower? karmaFlower)
    {
        Extras.GrantFakeAchievement("revival");

        karmaFlower?.Destroy();

        self.SaintStagger(120);

        self.burstX = 0f;
        self.burstY = 0f;

        self.monkAscension = false;

        self.voidSceneTimer = -1; // Prevents the sound effects for actual ascension from playing later
    }

    private static bool HealerSaintMechanics(Player self, PhysicalObject target, bool didAscendCreature)
    {
        if ((Extras.IsOnlineSession && !MeadowUtils.IsMine(self))
            || target is not (Creature or Oracle)
            || target.HasAscensionCooldown())
            return didAscendCreature;

        bool isTargetDead = target is Creature { dead: true } or Oracle { Alive: false };
        bool requireKarmaFlower = OptionUtils.IsOptionEnabled(Options.REQUIRE_KARMA_FLOWER) && target != self && isTargetDead;

        KarmaFlower? karmaFlower = requireKarmaFlower
            ? AscensionHandler.GetHeldKarmaFlower(self)
            : null;

        if (requireKarmaFlower && karmaFlower is null)
        {
            Main.Logger.LogDebug($"Player has no Karma Flower, ignoring revival target: {target}");

            target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown * 0.5f);

            return didAscendCreature;
        }

        Main.Logger.LogDebug($"Attempting to {(isTargetDead ? "revive" : "heal")}: {target}");

        bool result = false;

        switch (target)
        {
            case Creature creature:
                {
                    if (creature.dead)
                    {
                        result = AscensionHandler.TryAscendObject(creature, self);

                        if (result)
                        {
                            didAscendCreature = true;

                            ApplyAscensionEffects(self, requireKarmaFlower ? karmaFlower : null);
                        }
                    }
                    else if (creature.State is HealthState { health: < 1f } or PlayerState { permanentDamageTracking: > 0d }
                            || (ModManager.Watcher && (creature is Player { rippleDeathTime: > 0 } || creature.injectedPoison > 0 || creature.room?.locusts?.SwarmScore(creature) > 0f)))
                    {
                        if (creature.State is HealthState healthState)
                            healthState.health = 1f;

                        if (creature is Player player)
                        {
                            player.playerState?.permanentDamageTracking = 0d;

                            if (ModManager.Watcher)
                            {
                                player.rippleDeathTime = 0;
                                player.rippleDeathIntensity = 0f;
                            }
                        }

                        creature.deaf = 0;
                        creature.blind = 0;

                        creature.stun = 80;

                        if (ModManager.Watcher)
                        {
                            creature.injectedPoison = 0f;
                            creature.repelLocusts = 1800;
                        }

                        result = !creature.dead;

                        self.voidSceneTimer = 1; // Force play the "blast" sound from Saint's karmic blast
                    }

                    if (result)
                        self.abstractCreature.CharmCreature(creature.abstractCreature, Random.Range(180, 240));

                    target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown, isRevival: result);
                }
                break;
            case Oracle oracle:
                {
                    if (!oracle.Alive)
                    {
                        result = AscensionHandler.TryAscendObject(oracle, self);

                        if (result)
                        {
                            didAscendCreature = true;

                            ApplyAscensionEffects(self, requireKarmaFlower ? karmaFlower : null);
                        }
                    }
                    else if (oracle is { glowers: < 5, mySwarmers.Count: < 5 } && oracle.ID == Oracle.OracleID.SL)
                    {
                        RevivalHelper.AddOracleSwarmer(oracle);

                        self.voidSceneTimer = 1; // Force play the "blast" sound from Saint's karmic blast

                        result = oracle.glowers > 0 && oracle.mySwarmers.Count > 0;
                    }

                    target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown, isRevival: result);
                }
                break;
            default:
                target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown * 0.25f);
                break;
        }

        return didAscendCreature;
    }

    private static bool IsPlayerFriend(AbstractCreature creature, Player player)
    {
        ArtificialIntelligence? AI = creature.realizedCreature is Lizard lizor ? lizor.AI : creature.abstractAI?.RealAI;

        return AI is not null && ((AI is FriendTracker.IHaveFriendTracker && AI.friendTracker?.friend == player)
            || (AI is IReactToSocialEvents && AI.tracker is not null && !AI.DynamicRelationship(player.abstractCreature).GoForKill && player.room.game.session.creatureCommunities.LikeOfPlayer(creature.creatureTemplate.communityID, player.room.world?.RegionNumber ?? 0, player.playerState.playerNumber) >= 0.5f));
    }
}