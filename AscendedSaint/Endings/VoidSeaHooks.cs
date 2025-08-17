using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;
using VoidSea;

namespace AscendedSaint.Endings;

/// <summary>
/// Hooks and helper methods for modifying the behaviors of the main Void Worm (Jerry), and by extension, the sequence of events played once the player goes past the mass of "distant" Void Worms.
/// </summary>
public static partial class VoidSeaHooks
{
    /// <summary>
    /// Modifies the main Void Worm's behaviors to allow Saint to ascend like other slugcats.
    /// </summary>
    public static void EnableCustomAscensionILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            CustomAscension.AllowSaintAscension(c); // Allows the Void Worm to attach its string to Saint.

            CustomAscension.InvertSwimUpGoal(c); // Inverts the Void Worm's goalPos during the AttachingString phase, so it can more consistently attach its string to Saint.

            CustomAscension.InvertSwimDownTrigger(c); // Replaces the game's own check for when the Void Worm should swim downwards, to account for the Void Sea's unique coordinates in Rubicon.

            CustomAscension.InvertSwimDownGoal(c);

            CustomAscension.InvertSuperSwim(c);

            CustomAscension.InvertSwimDownPhase(c);

            CustomAscension.InvertShortGoalPos(c);
            CustomAscension.InvertShortGoalPos(c);

            CustomAscension.InvertLongGoalPos(c);

            CustomAscension.InvertLongSuperSwim(c);

            CustomAscension.InvertLongGoalPos(c);

            CustomAscension.InvertShortSuperSwim(c);

            CustomAscension.AllowWormDestruction(c);

            ASLogger.LogDebug(context);
        }
        catch (System.Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(EnableCustomAscensionILHook)}", ex);
        }
    }

    /// <summary>
    /// Modifies the Egg's position to be accessible to Saint.
    /// </summary>
    /// <remarks>If the game's default ending is playing, this has no effect.</remarks>
    public static void SpawnTheEggAsSaintILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            c.GotoNext(x => x.MatchStfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.theEgg))));

            // Target: this.theEgg = new VoidSeaScene.TheEgg(this, new Vector2(-200000f, -200000f));
            //                       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ HERE (Replace)

            c.Emit(OpCodes.Ldarg_0).EmitDelegate(InvertEggPosition);

            // Result: this.theEgg = this.Inverted ? new VoidSeaScene.TheEgg(this, new Vector2(-200000f, 200000f)) : new VoidSeaScene.TheEgg(this, new Vector2(-200000f, -200000f));
        }
        catch (System.Exception ex)
        {
            ASLogger.LogError($"Failed to apply IL hook: {nameof(SpawnTheEggAsSaintILHook)}", ex);
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
    /// Inverts the spawning position of the Egg in Rubicon.
    /// </summary>
    /// <param name="egg">The original Egg instance.</param>
    /// <param name="self">The VoidSeaScene object itself.</param>
    /// <returns>A <c>VoidSeaScene.TheEgg</c> instance with the correct depth for the given world.</returns>
    private static VoidSeaScene.TheEgg InvertEggPosition(VoidSeaScene.TheEgg egg, VoidSeaScene self) =>
        self.Inverted ? new VoidSeaScene.TheEgg(self, new Vector2(egg.pos.x, egg.pos.y * -1)) : egg;

    /// <summary>
    /// Conditionally inverts the given argument to account for Rubicon's inverted Void Sea.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <param name="value">The value to be inverted.</param>
    /// <returns><c>value * -1</c> if the given player can ascend as other slugcats, <c>value</c> otherwise.</returns>
    /// <seealso cref="VoidUtils.ShouldPlayAlternateEnding(Player)"/>
    private static float InvertTargetValue(float value, Player player) =>
        VoidUtils.ShouldPlayAlternateEnding(player) ? value * -1.0f : value;

    private static Vector2 InvertTargetVector(Vector2 vector2, Player player) =>
        VoidUtils.ShouldPlayAlternateEnding(player) ? new Vector2(vector2.x, vector2.y * -1f) : vector2;

    /// <summary>
    /// Determines if the Void Worm should destroy itself after leaving the player behind.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="self"></param>
    /// <returns><c>true</c> if the Void Worm should be destroyed, <c>false</c> otherwise.</returns>
    /// <remarks>This is used to properly despawn the main Void Worm in Saint's unique ascension.</remarks>
    private static bool ShouldDestroySelf(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y <= 0f && self.worm.chunks[self.worm.chunks.Length - 1].pos.y <= 0f;

    /// <summary>
    /// Determines if the Void Worm should start swimming downwards (or upwards in Saint's case).
    /// </summary>
    /// <param name="player">The player for reference.</param>
    /// <param name="self">The VoidWormBehavior object itself.</param>
    /// <returns><c>true</c> if the Void Worm should start its SwimDown phase, <c>false</c> otherwise.</returns>
    private static bool ShouldSwimDown(Player player, VoidWorm.MainWormBehavior self) =>
        VoidUtils.ShouldPlayAlternateEnding(player) && self.worm.chunks[0].pos.y <= self.goalPos.y;

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
}