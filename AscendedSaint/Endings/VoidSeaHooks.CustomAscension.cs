using Mono.Cecil.Cil;
using MonoMod.Cil;
using VoidSea;

namespace AscendedSaint.Endings;

public static partial class VoidSeaHooks
{
    private static class CustomAscension
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
            catch (System.Exception ex)
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
            catch (System.Exception ex)
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
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertNextValue)}", ex);

                throw;
            }
        }

        public static void OverrideNextConditional(ILCursor c, ILLabel target, System.Delegate @delegate)
        {
            try
            {
                c.EmitDelegate(@delegate);
                c.Emit(OpCodes.Brtrue, target);
            }
            catch (System.Exception ex)
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
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(SkipNextValue)}", ex);

                throw;
            }
        }
    }
}