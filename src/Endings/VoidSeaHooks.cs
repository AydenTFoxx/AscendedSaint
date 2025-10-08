using System;
using System.Runtime.CompilerServices;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using VoidSea;

namespace AscendedSaint.Endings;

/// <summary>
/// Hooks and helper methods for modifying the behaviors of the main Void Worm (Jerry), and by extension, the sequence of events played once the player goes past the mass of "distant" Void Worms.
/// </summary>
public static class VoidSeaHooks
{
    private static bool applyHooks;

    public static void AddHooks() => On.GameSession.ctor += InitHook;

    public static void RemoveHooks() => On.GameSession.ctor -= InitHook;

    private static void AddInternalHooks()
    {
        //IL.VoidSea.PlayerGhosts.Update += UpdatePlayerGhostsILHook;
        //IL.VoidSea.VoidSeaScene.SaintEndUpdate += IgnoreSaintEndUpdateILHook;
        //IL.VoidSea.VoidSeaScene.VoidSeaTreatment += VoidSeaTreatmentILHook;
        IL.VoidSea.VoidWorm.MainWormBehavior.Update += InvertSaintAscensionILHook;
    }

    private static void RemoveInternalHooks()
    {
        //IL.VoidSea.PlayerGhosts.Update -= UpdatePlayerGhostsILHook;
        //IL.VoidSea.VoidSeaScene.SaintEndUpdate -= IgnoreSaintEndUpdateILHook;
        //IL.VoidSea.VoidSeaScene.VoidSeaTreatment -= VoidSeaTreatmentILHook;
        IL.VoidSea.VoidWorm.MainWormBehavior.Update -= InvertSaintAscensionILHook;
    }

    private static void InitHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        bool dynamicEndings = OptionUtils.IsClientOptionEnabled(Options.DYNAMIC_ENDINGS);

        if (dynamicEndings != applyHooks)
        {
            applyHooks = !applyHooks;

            if (applyHooks)
            {
                AddHooks();
            }
            else
            {
                RemoveHooks();
            }
        }
    }

    /// <summary>
    /// Skips the execution of <c>SaintEndUpdate</c> if any of the mod's alternate endings are playing.
    /// </summary>
    private static void IgnoreSaintEndUpdateILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel target = null!;

        c.GotoNext(MoveType.After,
            static x => x.MatchLdloc(4),
            static x => x.MatchCallvirt(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.graphicsModule)).GetGetMethod()),
            x => x.MatchBrfalse(out target)
        );

        // Target:  if (player == null || player.graphicsModule == null) { ... }

        c.Emit(OpCodes.Ldloc, 4).EmitDelegate(VoidUtils.ShouldPlayAlternateEnding);
        c.Emit(OpCodes.Brtrue, target);

        // Result:  if (player == null || player.graphicsModule == null || VoidUtils.ShouldPlayAlternateEnding(player)) { ... }
    }

    /// <summary>
    /// Modifies several values in the main Void Worm's behaviors to allow Saint to ascend upwards, instead of violently crashing against the floor.
    /// </summary>
    private static void InvertSaintAscensionILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(static x => x.MatchCall(typeof(Region).GetMethod(nameof(Region.IsRubiconRegion))));
        c.GotoNext(static x => x.MatchBrfalse(out _)).MoveAfterLabels();

        // Target: if (Region.IsRubiconRegion(base.voidSea.room.world.name)) { ... }
        //             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldloc_0).EmitDelegate(OverrideRubiconCheck);

        // Result: if (OverrideRubiconCheck(Region.IsRubiconRegion(base.voidSea.room.world.name))) { ... }

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 100f, 500f);

        // Result: goalPos = Custom.MoveTowards(goalPos, player.mainBodyChunk.pos + new Vector2(InvertNextValue(100f), InvertNextValue(500f)), 60f);

        c.GotoNext(MoveType.After, x => x.MatchLdcR4(7000f))
         .GotoNext()
         .MoveAfterLabels()
         .Emit(OpCodes.Ldloc_0)
         .EmitDelegate(NegateTargetValue);

        // Result: goalPos = new Vector2(base.voidSea.sceneOrigo.x, NegateTargetValue(base.voidSea.voidWormsAltitude + 7000f));

        c.GotoNext(MoveType.After,
            static x => x.MatchStfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.ridingWorm)))
        ).MoveAfterLabels();

        // Target: base.voidSea.ridingWorm = true; <-- HERE (Append)

        ILCursor d = new(c);

        d.GotoNext(MoveType.After, static x => x.MatchBleUn(out _)).MoveAfterLabels();

        ILLabel target1 = d.MarkLabel();

        // Target: if (worm.chunks[0].pos.y > base.voidSea.voidWormsAltitude + 7000f) { SwitchPhase(Phase.SwimDown); }
        //                                                                             ^ HERE (No-op; Referenced for later use)

        c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
        CustomAscension.OverrideNextConditional(c, target1, ShouldSwimDown);

        c.GotoNext(MoveType.After, x => x.MatchLdcR4(7000f))
         .GotoNext()
         .MoveAfterLabels()
         .Emit(OpCodes.Ldloc_0)
         .EmitDelegate(NegateTargetValue);

        // Result: base.voidSea.ridingWorm = true; if (ShouldSwimDown(player, this) || worm.chunks[0].pos.y > NegateTargetValue(base.voidSea.voidWormsAltitude + 7000f)) { SwitchPhase(Phase.SwimDown); }

        CustomAscension.SkipNextValue(c, 0f);
        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, -100000f);

        // Result: goalPos = worm.chunks[0].pos + new Vector2(0f, InvertNextValue(-100000f));

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, -40f);

        // Result: SuperSwim(InvertNextValue(-40f) * Mathf.InverseLerp(0f, 200f, timeInPhase));

        c.GotoNext(
            static x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer))),
            static x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Inequality")),
            x => x.MatchBrfalse(out _)
        ).GotoPrev(
            static x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.MSC)))
        );

        ILLabel target2 = c.MarkLabel();

        // Target: if (!ModManager.MSC || player.playerState.slugcatCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer) { ... }
        //        ^ HERE (Prepend)

        ILLabel target5 = null!;
        ILCursor d2 = new(c);

        d2.GotoNext(x => x.MatchBr(out target5));

        // Target: else if (worm.chunks[0].pos.y < -11000f) { ... }
        //              ^^ HERE (Referenced for skipping to end of method)

        c.Emit(OpCodes.Ldloc_0).EmitDelegate(VoidUtils.ShouldPlayAlternateEnding);
        c.Emit(OpCodes.Brfalse, target2);

        c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(InvertSwimDownPhase);
        c.Emit(OpCodes.Br, target5);

        // Result: if (ShouldPlayAlternateEnding(player)) { InvertSwimDownPhase(player, this); } else if (!ModManager.MSC || player.playerState.slugcatCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer) { ... }

        c.GotoNext(static x => x.MatchLdfld(typeof(VoidWorm.MainWormBehavior).GetField(nameof(VoidWorm.MainWormBehavior.goalPos))));

        // Target: goalPos = Vector2.Lerp(goalPos, player.mainBodyChunk.pos + new Vector2(-100f, 500f), 0.2f);

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 500f, 500f);

        // Result (x2): goalPos = Vector2.Lerp(goalPos, player.mainBodyChunk.pos + new Vector2(-100f, InvertNextValue(500f)), 0.2f);

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 100000f);

        // Result: goalPos = new Vector2(player.mainBodyChunk.pos.x, worm.chunks[0].pos.y - InvertNextValue(100000f));

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, -150f);

        // Result: SuperSwim(InvertNextValue(-150f) * Mathf.InverseLerp(600f, 650f, timeInPhase));

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 100000f);

        // Result: goalPos = new Vector2(player.mainBodyChunk.pos.x, InvertNextValue(100000f));

        CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 50f);

        // Result: SuperSwim(InvertNextValue(50f));

        c.GotoNext().GotoNext(MoveType.After).MoveAfterLabels();

        ILLabel target3 = c.MarkLabel();

        // Target: if (worm.chunks[0].pos.y > 10000f && worm.chunks[worm.chunks.Length - 1].pos.y > 10000f) { ... }
        //        ^ HERE (Prepend)

        ILCursor d1 = new(c);

        d1.GotoNext(MoveType.Before,
            static x => x.MatchCall(typeof(VoidWorm.VoidWormBehavior).GetProperty(nameof(VoidWorm.VoidWormBehavior.voidSea)).GetGetMethod()),
            static x => x.MatchCallvirt(typeof(VoidSeaScene).GetMethod(nameof(VoidSeaScene.DestroyMainWorm)))
        ).GotoPrev().MoveAfterLabels();

        ILLabel target4 = d1.MarkLabel();

        // Target: base.voidSea.DestroyMainWorm();
        //        ^ HERE (4)        and...        ^ HERE (5) (No-op; Referenced for later use)

        c.Emit(OpCodes.Ldloc_0).EmitDelegate(VoidUtils.ShouldPlayAlternateEnding);
        c.Emit(OpCodes.Brfalse, target3);

        c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
        c.EmitDelegate(ShouldDestroyMainWorm);
        c.Emit(OpCodes.Brtrue, target4);
        c.Emit(OpCodes.Br, target5);

        // Result: if (ShouldPlayAlternateEnding(player)) { if (ShouldDestroyMainWorm(player, this)) { goto IL_1685; // aka next if } } else if (this.worm.chunks[0].pos.y > 10000f && this.worm.chunks[this.worm.chunks.Length - 1].pos.y > 10000f)) { ... }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetTargetDepth(VoidWorm.MainWormBehavior self, float value) => self.voidSea.voidWormsAltitude + value;

        static void InvertSwimDownPhase(Player player, VoidWorm.MainWormBehavior self)
        {
            if (!self.voidSea.secondSpace)
            {
                if (self.worm.chunks[0].pos.y > GetTargetDepth(self, 17000f) && (int)self.voidSea.deepDivePhase < (int)VoidSeaScene.DeepDivePhase.CeilingDestroyed)
                {
                    self.voidSea.DestroyCeiling();
                }
                if (self.worm.chunks[0].pos.y > GetTargetDepth(self, 35000f) && (int)self.voidSea.deepDivePhase < (int)VoidSeaScene.DeepDivePhase.CloseWormsDestroyed)
                {
                    self.voidSea.DestroyAllWormsExceptMainWorm();
                }
                if (self.worm.chunks[0].pos.y > GetTargetDepth(self, 200000f) && (int)self.voidSea.deepDivePhase < (int)VoidSeaScene.DeepDivePhase.DistantWormsDestroyed)
                {
                    self.voidSea.DestroyDistantWorms();
                }
                if (self.worm.chunks[0].pos.y > GetTargetDepth(self, 440000f) && (int)self.voidSea.deepDivePhase < (int)VoidSeaScene.DeepDivePhase.MovedIntoSecondSpace)
                {
                    self.voidSea.MovedToSecondSpace();
                }
            }
            else if (self.worm.chunks[0].pos.y > GetTargetDepth(self, 11000f))
            {
                self.SwitchPhase(VoidWorm.MainWormBehavior.Phase.DepthReached);
            }
        }

        static bool ShouldDestroyMainWorm(Player player, VoidWorm.MainWormBehavior self) =>
            VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y < -10000f && self.worm.chunks[self.worm.chunks.Length - 1].pos.y < -10000f;

        static bool ShouldSwimDown(Player player, VoidWorm.MainWormBehavior self) =>
            VoidUtils.ShouldPlayAlternateEnding(player) && (self.worm.chunks[0].pos.y < self.voidSea.voidWormsAltitude - 14000f || self.timeInPhase > 300);
    }

    /// <summary>
    /// Prevents player ghosts from spawning in Saint's alternate ascension ending.
    /// </summary>
    private static void UpdatePlayerGhostsILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(MoveType.After,
            static x => x.MatchCall(typeof(PlayerGhosts).GetProperty(nameof(PlayerGhosts.IdealGhostCount)).GetGetMethod())
        ).MoveAfterLabels();

        c.Emit(OpCodes.Ldarg_0).Emit(OpCodes.Ldfld, typeof(PlayerGhosts).GetField(nameof(PlayerGhosts.originalPlayer)));
        c.EmitDelegate(OverrideIdealGhostCount);

        static int OverrideIdealGhostCount(int value, Player player) =>
            VoidUtils.ShouldPlayAlternateEnding(player) ? 1 : value;
    }

    /// <summary>
    /// *Maybe* prevents the player from being sent into otherwordly realms when switching to the Egg scenario.
    /// </summary>
    private static void VoidSeaTreatmentILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(static x => x.MatchCall(typeof(Region).GetMethod(nameof(Region.IsRubiconRegion))));
        c.GotoNext(static x => x.MatchBrfalse(out _)).MoveAfterLabels();

        // Target: if (Region.IsRubiconRegion(base.voidSea.room.world.name)) { ... }
        //             ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldarg_1).EmitDelegate(OverrideRubiconCheck);

        // Result: if (OverrideRubiconCheck(Region.IsRubiconRegion(base.voidSea.room.world.name))) { ... }
    }

    private static float NegateTargetValue(float value, Player player) => VoidUtils.ShouldPlayAlternateEnding(player) ? 0f : value;

    private static bool OverrideRubiconCheck(bool value, Player player) => value && !VoidUtils.ShouldPlayAlternateEnding(player);

    /// <summary>
    /// Collection of utilities for overriding behaviors of the Void Sea's ascension sequence.
    /// </summary>
    private static class CustomAscension
    {
        public static void InvertNextValue(ILCursor c, OpCode playerCode, params float[] values)
        {
            try
            {
                foreach (float value in values)
                {
                    c.GotoNext(MoveType.After, x => x.MatchLdcR4(value))
                     .MoveAfterLabels()
                     .Emit(playerCode)
                     .EmitDelegate(InvertTargetValue);
                }
            }
            catch (Exception ex)
            {
                ModLib.Logger.LogError($"Failed to apply sub-hook: {nameof(InvertNextValue)}", ex);

                throw;
            }

            static float InvertTargetValue(float value, Player player) => VoidUtils.ShouldPlayAlternateEnding(player) ? value * -1f : value;
        }

        public static void OverrideNextConditional(ILCursor c, ILLabel target, Delegate @delegate)
        {
            try
            {
                c.EmitDelegate(@delegate);
                c.Emit(OpCodes.Brtrue, target);
            }
            catch (Exception ex)
            {
                ModLib.Logger.LogError($"Failed to apply sub-hook: {nameof(OverrideNextConditional)}", ex);

                throw;
            }
        }

        public static void OverrideNextValue(ILCursor c, OpCode playerCode, float value, float newValue)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(value))
                 .MoveAfterLabels()
                 .Emit(OpCodes.Ldc_R4, newValue)
                 .Emit(playerCode)
                 .EmitDelegate(OverrideTargetValue);
            }
            catch (Exception ex)
            {
                ModLib.Logger.LogError($"Failed to apply sub-hook: {nameof(OverrideNextValue)}", ex);

                throw;
            }

            static float OverrideTargetValue(float value, float newValue, Player player) => VoidUtils.ShouldPlayAlternateEnding(player) ? newValue : value;
        }

        public static void SkipNextValue(ILCursor c, params float[] values)
        {
            try
            {
                foreach (float value in values)
                {
                    c.GotoNext(MoveType.After, x => x.MatchLdcR4(value));
                }
                c.MoveAfterLabels();
            }
            catch (Exception ex)
            {
                ModLib.Logger.LogError($"Failed to apply sub-hook: {nameof(SkipNextValue)}", ex);

                throw;
            }
        }
    }
}