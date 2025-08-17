using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
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
                c.GotoNext(x => x.MatchBrfalse(out var _)).MoveAfterLabels();

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

        public static void InvertGoalPos(ILCursor c)
        {
            try
            {
                c.GotoNext(x => x.MatchStfld(typeof(VoidWorm.MainWormBehavior).GetField(nameof(VoidWorm.MainWormBehavior.goalPos)))).MoveAfterLabels();

                // Target: this.goalPos = new Vector2(...);
                //                        ^^^^^^^^^^^^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetVector);

                // Result: this.goalPos = InvertTargetVector(player, new Vector2(...));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertGoalPos)}", ex);

                throw;
            }
        }

        public static void InvertSwimUpGoal(ILCursor c)
        {
            try
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.voidWormsAltitude))),
                    x => x.MatchLdcR4(7000f)
                ).MoveAfterLabels();

                // Target: this.goalPos = new Vector2(base.voidSea.sceneOrigo.x, base.voidSea.voidWormsAltitude + 7000f);
                //                                                                                                ^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.goalPos = new Vector2(base.voidSea.sceneOrigo.x, base.voidSea.voidWormsAltitude + InvertTargetValue(player, 7000f));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertSwimUpGoal)}", ex);

                throw;
            }
        }

        /// <summary>
        /// Overrides the usual check to start the Void Worm's SwimDown phase.
        /// </summary>
        public static void InvertSwimDownTrigger(ILCursor c)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchStfld(typeof(VoidSeaScene).GetField(nameof(VoidSeaScene.ridingWorm)))).MoveAfterLabels();

                // Target: base.voidSea.ridingWorm = true; <-- HERE (Append)

                ILCursor d = new(c);

                d.GotoNext(x => x.MatchLdsfld(typeof(VoidWorm.MainWormBehavior.Phase).GetField(nameof(VoidWorm.MainWormBehavior.Phase.SwimDown))));
                d.GotoPrev(x => x.MatchLdarg(0));

                ILLabel target = d.MarkLabel();

                c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ShouldSwimDown);
                c.Emit(OpCodes.Brtrue, target);

                // Result: base.voidSea.ridingWorm = true; if (!ShouldSwimDown(player, this)) { ... }
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertSwimDownTrigger)}", ex);

                throw;
            }
        }

        public static void InvertSwimDownGoal(ILCursor c)
        {
            try
            {
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdcR4(0f),
                    x => x.MatchLdcR4(-100000f)
                ).MoveAfterLabels();

                // Target: this.goalPos = this.worm.chunks[0].pos + new Vector2(0f, -100000f);
                //                                                                   ^^^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.goalPos = this.worm.chunks[0].pos + new Vector2(0f, InvertTargetValue(player, -100000f));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertSwimDownGoal)}", ex);

                throw;
            }
        }

        public static void InvertSuperSwim(ILCursor c)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(0f)).MoveAfterLabels();

                // Target: this.SuperSwim(-40f * Mathf.InverseLerp(...));
                //                        ^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.SuperSwim(InvertTargetValue(player, -40f) * Mathf.InverseLerp(...));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertSuperSwim)}", ex);

                throw;
            }
        }

        public static void InvertSwimDownPhase(ILCursor c)
        {
            try
            {
                ILLabel target1 = null;

                c.GotoNext(
                    x => x.MatchLdsfld(typeof(MoreSlugcatsEnums.SlugcatStatsName).GetField(nameof(MoreSlugcatsEnums.SlugcatStatsName.Artificer))),
                    x => x.MatchCall(typeof(ExtEnum<SlugcatStats.Name>).GetMethod("op_Inequality"))
                );
                c.GotoNext(MoveType.After, x => x.MatchBrfalse(out target1)).MoveAfterLabels();

                // Target: if (!ModManager.MSC || player.playerState.slugcatCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer) { ... } <-- HERE (Append)

                c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(SwimDownPhase);
                c.Emit(OpCodes.Brfalse, target1);

                // Result: (!ModManager.MSC || player.playerState.slugcatCharacter != MoreSlugcatsEnums.SlugcatStatsName.Artificer || !SwimDownPhase(player, this)) { ... }
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertSwimDownPhase)}", ex);

                throw;
            }
        }

        public static void InvertShortGoalPos(ILCursor c)
        {
            try
            {
                c.GotoNext(x => x.MatchLdfld(typeof(VoidWorm.MainWormBehavior).GetField(nameof(VoidWorm.MainWormBehavior.goalPos))));
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(-100)).MoveAfterLabels();

                // Target: this.goalPos = Vector2.Lerp(this.goalPos, player.mainBodyChunk.pos + new Vector2(-100f, 500f), 0.2f);

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.goalPos = Vector2.Lerp(this.goalPos, player.mainBodyChunk.pos + new Vector2(InvertTargetValue(player, -100f), 500f), 0.2f);
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertShortGoalPos)}", ex);

                throw;
            }
        }

        public static void InvertLongGoalPos(ILCursor c)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(100000f)).MoveAfterLabels();

                // Target (1): this.goalPos = new Vector2(player.mainBodyChunk.pos.x, this.worm.chunks[0].pos.y - 100000f);
                //                                                                                            ^^^^^^^ HERE (Replace)

                // Target (2): this.goalPos = new Vector2(player.mainBodyChunk.pos.x, 100000f);
                //                                                                ^^^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result (1): this.goalPos = new Vector2(player.mainBodyChunk.pos.x, this.worm.chunks[0].pos.y - InvertTargetValue(player, 100000f));

                // Result (2): this.goalPos = new Vector2(player.mainBodyChunk.pos.x, InvertTargetValue(player, 100000f));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertLongGoalPos)}", ex);

                throw;
            }
        }

        public static void InvertLongSuperSwim(ILCursor c)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(-150f)).MoveAfterLabels();

                // Target: this.SuperSwim(-150f * Mathf.InverseLerp(600f, 650f, (float)this.timeInPhase));
                //                        ^^^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.SuperSwim(InvertTargetValue(player, -150f) * Mathf.InverseLerp(600f, 650f, (float)this.timeInPhase));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertLongSuperSwim)}", ex);

                throw;
            }
        }

        public static void InvertShortSuperSwim(ILCursor c)
        {
            try
            {
                c.GotoNext(MoveType.After, x => x.MatchLdcR4(50f)).MoveAfterLabels();

                // Target: this.SuperSwim(50f);
                //                        ^^^ HERE (Replace)

                c.Emit(OpCodes.Ldloc_0).EmitDelegate(InvertTargetValue);

                // Result: this.SuperSwim(InvertTargetValue(player, 50f));
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(InvertShortSuperSwim)}", ex);

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

                // Target: if (this.worm.chunks[0].pos.y > 10000f && this.worm.chunks[this.worm.chunks.Length - 1].pos.y > 10000f) { ... }

                c.Emit(OpCodes.Ldloc_0).Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(ShouldDestroySelf);
                c.Emit(OpCodes.Brtrue, target2);

                // Result: if ((this.worm.chunks[0].pos.y > 10000f && this.worm.chunks[this.worm.chunks.Length - 1].pos.y > 10000f) || ShouldDestroySelf(player, this))
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError($"Failed to apply sub-hook: {nameof(AllowWormDestruction)}", ex);

                throw;
            }
        }
    }
}