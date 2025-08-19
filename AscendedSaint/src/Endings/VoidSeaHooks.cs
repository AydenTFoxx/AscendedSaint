using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using VoidSea;

namespace AscendedSaint.Endings;

/// <summary>
/// Hooks and helper methods for modifying the behaviors of the main Void Worm (Jerry), and by extension, the sequence of events played once the player goes past the mass of "distant" Void Worms.
/// </summary>
public static partial class VoidSeaHooks
{
    /// <summary>
    /// Modifies several values in the main Void Worm's behaviors to allow Saint to ascend upwards, instead of violently crashing against the floor.
    /// </summary>
    public static void InvertSaintAscensionILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            CustomAscension.AllowSaintAscension(c);

            CustomAscension.SkipNextValue(c, 500f);
            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 500f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 7000f);

            c.GotoNext(MoveType.After, x => x.MatchStfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.ridingWorm)))).MoveAfterLabels();

            ILCursor d = new(c);
            ILLabel target3 = null;

            d.GotoNext(MoveType.After, x => x.MatchBleUn(out target3)).MoveAfterLabels();

            ILLabel target1 = d.MarkLabel();

            c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
            CustomAscension.OverrideNextConditional(c, target1, ShouldSwimDown);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 0f, -100000f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, -40f);

            ILLabel target2 = null;

            c.GotoNext(
                x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer))),
                x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Inequality"))
            );
            c.GotoPrev(x => x.MatchBrfalse(out target2));
            c.GotoPrev().MoveAfterLabels();

            c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
            CustomAscension.OverrideNextConditional(c, target2, SwimDownPhase);

            c.GotoNext(x => x.MatchLdfld(typeof(VoidWorm.MainWormBehavior).GetField(nameof(VoidWorm.MainWormBehavior.goalPos))));

            CustomAscension.SkipNextValue(c, 500f);
            CustomAscension.SkipNextValue(c, 500f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 100000f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, -150f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 100000f);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 50f);

            CustomAscension.AllowWormDestruction(c);
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(InvertSaintAscensionILHook)}", ex);
        }
    }

    /// <summary>
    /// Overrides the usual check for placing the Egg so it can be at the correct height in Saint's ascension.
    /// </summary>
    public static void UpdateSaintInVoidSeaILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            c.GotoNext(x => x.MatchLdfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.deepDivePhase))));
            c.GotoPrev().MoveAfterLabels();

            ILCursor d = new(c);
            ILLabel target2 = null;

            d.GotoNext(MoveType.After, x => x.MatchBleUn(out target2)).MoveAfterLabels();

            ILLabel target1 = d.MarkLabel();

            c.Emit(OpCodes.Ldarg_1).Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(ShouldPlaceEgg);
            c.Emit(OpCodes.Brtrue, target1);

            CustomAscension.InvertNextValue(c, OpCodes.Ldarg_1, -11000f);
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(UpdateSaintInVoidSeaILHook)}", ex);
        }
    }

    /// <summary>
    /// Skips the execution of <c>SaintEndUpdate</c> if any of the mod's alternate endings are playing.
    /// </summary>
    public static void IgnoreSaintEndUpdateILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            c.GotoNext(x => x.MatchCallvirt(typeof(PhysicalObject).GetProperty(nameof(PhysicalObject.graphicsModule)).GetGetMethod()));
            c.GotoPrev(MoveType.After, x => x.MatchBrfalse(out _)).MoveAfterLabels();

            ILCursor d = new(c);

            d.GotoNext(x => x.MatchLdarg(0));

            c.Emit(OpCodes.Ldloc_1).EmitDelegate(VoidUtils.ShouldPlayAlternateEnding);
            c.Emit(OpCodes.Brfalse, d.MarkLabel());
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(IgnoreSaintEndUpdateILHook)}", ex);
        }
    }

    public static void RemoveSaintGhostsILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);
            ILCursor d = new(c);

            d.GotoNext(x => x.MatchRet());

            c.Emit(OpCodes.Ldarg_0).EmitDelegate(TryRemovePlayerGhost);
            c.Emit(OpCodes.Brtrue, d.MarkLabel());
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(RemoveSaintGhostsILHook)}", ex);
        }
    }

    /// <summary>
    /// Obtains the correct depth the Void Worm should swim towards during its long swim phase.
    /// </summary>
    /// <returns>A <c>float</c> value offset to match Rubicon's Void Sea's depth.</returns>
    private static float GetTargetDepth(VoidWorm.MainWormBehavior self, float value) => self.voidSea.voidWormsAltitude + value;

    /// <summary>
    /// Conditionally overrides the base game's checks to prevent the Void Worm from attaching its string to Saint, effectively allowing them to proceed with a regular ascension instead.
    /// </summary>
    /// <returns><c>true</c> if the player should ascend as other slugcats, <c>false</c> otherwise.</returns>
    /// <seealso cref="VoidUtils.ShouldPlayAlternateEnding(Player)"/>
    private static bool InvertConditional(bool value, Player player) => value || VoidUtils.ShouldPlayAlternateEnding(player);

    /// <summary>
    /// Conditionally inverts the given float to account for Rubicon's inverted Void Sea.
    /// </summary>
    /// <returns><c>true</c> if the player should ascend as other slugcats, <c>false</c> otherwise.</returns>
    /// <seealso cref="VoidUtils.ShouldPlayAlternateEnding(Player)"/>
    private static float InvertTargetValue(float value, Player player) => VoidUtils.ShouldPlayAlternateEnding(player) ? value * -1f : value;

    /// <summary>
    /// Determines if the Void Worm should destroy itself after leaving the player behind.
    /// </summary>
    /// <returns><c>true</c> if the Void Worm should be destroyed, <c>false</c> otherwise.</returns>
    /// <remarks>This is used to properly despawn the main Void Worm in Saint's unique ascension.</remarks>
    private static bool ShouldDestroySelf(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y < -10000f && self.worm.chunks[self.worm.chunks.Length - 1].pos.y < -10000f;

    /// <summary>
    /// Determines if the Egg should be placed at the end of Saint's ascension.
    /// </summary>
    /// <returns><c>true</c> if the Egg should be allowed to be placed, <c>false</c> otherwise.</returns>
    private static bool ShouldPlaceEgg(Player player, VoidSeaScene self) =>
        (int)self.deepDivePhase >= (int)VoidSeaScene.DeepDivePhase.EggScenario && player.mainBodyChunk.pos.y < 10000f;

    /// <summary>
    /// Determines if the Void Worm should start swimming downwards (or upwards in Saint's case).
    /// </summary>
    /// <returns><c>true</c> if the Void Worm should start its SwimDown phase, <c>false</c> otherwise.</returns>
    private static bool ShouldSwimDown(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && (self.worm.chunks[0].pos.y < self.voidSea.voidWormsAltitude - 7000f || self.timeInPhase > 100);

    /// <summary>
    /// Overrides the Void Worm's SwimDown phase to account for positive numbers.
    /// </summary>
    /// <returns><c>true</c> if the Void Worm's phase was overriden, <c>false</c> otherwise.</returns>
    private static bool SwimDownPhase(Player player, VoidWorm.MainWormBehavior self)
    {
        if (!VoidUtils.ShouldPlayAlternateEnding(player)) return false;

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

        return true;
    }

    private static bool TryRemovePlayerGhost(PlayerGhosts.Ghost self)
    {
        if (VoidUtils.ShouldPlayAlternateEnding(self.creature))
        {
            self.Destroy();
            return true;
        }

        return false;
    }

    public static class CustomAscension
    {
        public static void AllowSaintAscension(ILCursor c)
        {
            try
            {
                c.GotoNext(
                    x => x.MatchLdstr("HR"),
                    x => x.MatchCall(typeof(string).GetMethod("op_Inequality"))
                );
                c.GotoNext(x => x.MatchBrfalse(out _)).MoveAfterLabels();

                // Target: else if (... || base.voidSea.room.world.name != "HR")
                //                         ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertConditional);

                // Result: else if (... || InvertConditional(player, base.voidSea.room.world.name != "HR")) { ... }
            }
            catch (Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(AllowSaintAscension)}", ex);

                throw;
            }
        }

        public static void AllowWormDestruction(ILCursor c)
        {
            try
            {
                c.GotoNext().GotoNext(MoveType.After); // Assuming InvertShortSuperSwim(ILCursor) was called beforehand.

                ILCursor d1 = new(c);

                d1.GotoNext(x => x.MatchCallvirt(typeof(VoidSeaScene).GetMethod(nameof(VoidSeaScene.DestroyMainWorm))));
                d1.GotoPrev(x => x.MatchLdarg(0));

                ILLabel target2 = d1.MarkLabel();
                ILLabel target3 = null;

                d1.GotoNext(x => x.MatchBr(out target3));

                // Target: if (this.worm.chunks[0].pos.y > 10000f && this.worm.chunks[this.worm.chunks.Length - 1].pos.y > 10000f) { ... }

                c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ShouldDestroySelf);
                c.Emit(OpCodes.Brtrue, target2);
                c.Emit(OpCodes.Br, target3);

                // Result: if ((this.worm.chunks[0].pos.y > 10000f && this.worm.chunks[this.worm.chunks.Length - 1].pos.y > 10000f) || ShouldDestroySelf(player, this))
            }
            catch (Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(AllowWormDestruction)}", ex);

                throw;
            }
        }

        public static void InvertNextValue(ILCursor c, OpCode playerCode, params float[] values)
        {
            try
            {
                foreach (float value in values)
                {
                    c.GotoNext(MoveType.After, x => x.MatchLdcR4(value));
                }

                c.MoveAfterLabels();

                c.Emit(playerCode).EmitDelegate(InvertTargetValue);
            }
            catch (Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertNextValue)}", ex);

                throw;
            }
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
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(OverrideNextConditional)}", ex);

                throw;
            }
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
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(SkipNextValue)}", ex);

                throw;
            }
        }
    }
}