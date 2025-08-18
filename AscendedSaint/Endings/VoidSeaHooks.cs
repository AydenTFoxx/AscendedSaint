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

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 7000f);

            c.GotoNext(MoveType.After, x => x.MatchStfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.ridingWorm)))).MoveAfterLabels();

            ILCursor d = new(c);

            ILLabel target3 = null;

            d.GotoNext(MoveType.After, x => x.MatchBleUn(out target3)).MoveAfterLabels();

            ILLabel target1 = d.MarkLabel();

            c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
            CustomAscension.OverrideNextConditional(c, target1, ShouldSwimDown);

            c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
            CustomAscension.OverrideNextConditional(c, target3, TryPlayAlternateEnding);

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

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 500f);
            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_0, 500f);

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

    public static void UpdateSaintEggPhaseILHook(ILContext context)
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
            CustomAscension.OverrideNextConditional(c, target1, ShouldPlaceEgg);

            c.Emit(OpCodes.Ldarg_1).Emit(OpCodes.Ldarg_0);
            CustomAscension.OverrideNextConditional(c, target2, ShouldOverrideEggPosition);

            CustomAscension.InvertNextValue(c, OpCodes.Ldarg_1, -11000f);
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(UpdateSaintEggPhaseILHook)}", ex);
        }
    }

    public static void SetSaintEggSpawnPositionILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            CustomAscension.InvertNextValue(c, OpCodes.Ldloc_1, 0f, 11000f);
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(SetSaintEggSpawnPositionILHook)}", ex);
        }
    }

    public static void IgnoreMethodILHook(ILContext context)
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

            ASLogger.LogDebug(context);
        }
        catch (Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(IgnoreMethodILHook)}", ex);
        }
    }

    /// <summary>
    /// Obtains the correct depth the Void Worm should swim towards.
    /// </summary>
    /// <param name="self"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    private static float GetTargetDepth(VoidWorm.MainWormBehavior self, float value) => self.voidSea.voidWormsAltitude + value;

    /// <summary>
    /// Conditionally overrides the base game's checks to prevent the Void Worm from attaching its string to Saint, effectively allowing them to proceed with a regular ascension instead.
    /// </summary>
    /// <param name="value">The previous check's value.</param>
    /// <param name="player">The player to be tested.</param>
    /// <returns><c>true</c> if the player should ascend as other slugcats, <c>false</c> otherwise.</returns>
    /// <seealso cref="VoidUtils.ShouldPlayAlternateEnding(Player)"/>
    private static bool InvertConditional(bool value, Player player) => value || VoidUtils.ShouldPlayAlternateEnding(player);

    /// <summary>
    /// Conditionally inverts the given float to account for Rubicon's inverted Void Sea.
    /// </summary>
    /// <param name="value">The previous check's value.</param>
    /// <param name="player">The player to be tested.</param>
    /// <returns><c>true</c> if the player should ascend as other slugcats, <c>false</c> otherwise.</returns>
    /// <seealso cref="VoidUtils.ShouldPlayAlternateEnding(Player)"/>
    private static float InvertTargetValue(float value, Player player) =>
        VoidUtils.ShouldPlayAlternateEnding(player) ? value * -1.0f : value;

    /// <summary>
    /// Determines if the Void Worm should destroy itself after leaving the player behind.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="self"></param>
    /// <returns><c>true</c> if the Void Worm should be destroyed, <c>false</c> otherwise.</returns>
    /// <remarks>This is used to properly despawn the main Void Worm in Saint's unique ascension.</remarks>
    private static bool ShouldDestroySelf(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y < -10000f && self.worm.chunks[self.worm.chunks.Length - 1].pos.y < -10000f;

    private static bool ShouldOverrideEggPosition(Player player, VoidSeaScene self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && !ShouldPlaceEgg(player, self);

    private static bool ShouldPlaceEgg(Player player, VoidSeaScene self) =>
        (int)self.deepDivePhase >= (int)VoidSeaScene.DeepDivePhase.EggScenario && player.mainBodyChunk.pos.y < 10000f;

    /// <summary>
    /// Determines if the Void Worm should start swimming downwards (or upwards in Saint's case).
    /// </summary>
    /// <param name="player">The player for reference.</param>
    /// <param name="self">The VoidWormBehavior object itself.</param>
    /// <returns><c>true</c> if the Void Worm should start its SwimDown phase, <c>false</c> otherwise.</returns>
    private static bool ShouldSwimDown(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y < self.voidSea.voidWormsAltitude - 21000f;

    /// <summary>
    /// Overrides the Void Worm's SwimDown phase to account for positive numbers.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="self"></param>
    /// <returns></returns>
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

    private static bool TryPlayAlternateEnding(Player player, VoidWorm.MainWormBehavior self)
    {
        if (VoidUtils.ShouldPlayAlternateEnding(player) && !ShouldSwimDown(player, self))
        {
            self.SwitchPhase(VoidWorm.MainWormBehavior.Phase.SwimDown);
            return true;
        }
        return false;
    }
}