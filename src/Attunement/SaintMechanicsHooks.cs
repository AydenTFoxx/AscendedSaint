using ModLib;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AscendedSaint.Attunement;

/// <summary>
/// All hooks relating to Saint's unique abilities.
/// </summary>
public static class SaintMechanicsHooks
{
    public static void AddHooks() => IL.Player.ClassMechanicsSaint += Extras.WrapILHook(AscensionMechanicsILHook);

    public static void RemoveHooks() => IL.Player.ClassMechanicsSaint -= Extras.WrapILHook(AscensionMechanicsILHook);

    private static void AscensionMechanicsILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(MoveType.After,
            static x => x.MatchLdloc(18), // physicalObject
            static x => x.MatchLdarg(0), // this
            x => x.MatchBeq(out target) // (end of if block)
        ).GotoPrev();
        c.MoveAfterLabels();

        // Target: if (physicalObject != this && ...)
        //                               ^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldloc, 18); // physicalObject
        c.EmitDelegate(NullifySelfReference); // this, physicalObject

        // Result: if (physicalObject != NullifySelfReference(this, physicalObject) && ...)

        c.GotoNext(x => x.MatchBrfalse(out _));
        c.GotoNext(
            MoveType.After,
            x => x.MatchStfld(out _)
        );
        c.MoveAfterLabels();

        // Target: bodyChunk.vel += Custom.RNV() * 36f; <-- HERE (Append)

        c.Emit(OpCodes.Ldloc, 18); // physicalObject
        c.Emit(OpCodes.Ldarg_0); // this
        c.Emit(OpCodes.Ldloc, 15); // flag2 (didAscendCreature)
        c.EmitDelegate(ApplySaintMechanics); // bodyChunk, this, flag2
        c.Emit(OpCodes.Stloc, 15);
        c.Emit(OpCodes.Ldloc, 15);
        c.Emit(OpCodes.Brtrue, target); // if (ApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1435;

        // Result: bodyChunk.vel += Custom.RNV() * 36f; if (ApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1435;

        c.GotoNext(MoveType.Before,
            static x => x.MatchLdloc(15),
            static x => x.MatchBrtrue(out _)
        ).MoveAfterLabels();

        // Target: if (flag2 || voidSceneTimer > 0) { ... }
        //        ^ HERE (Prepend)

        ILCursor d = new(c);

        d.GotoNext(MoveType.Before,
            static x => x.MatchLdcI4(0),
            static x => x.MatchStloc(28)
        ); // for (int m = 0; m < 20; m++) { ... }

        ILLabel target2 = d.MarkLabel();

        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldfld, typeof(Player).GetField(nameof(Player.voidSceneTimer))); // this.voidSceneTimer
        c.Emit(OpCodes.Ldc_I4, -1); // -1
        c.Emit(OpCodes.Beq, target2); // if (this.voidSeaTimer == -1) { goto IL_156c; }

        // Result: if (this.voidSeaTimer == -1) { goto IL_156c; } else if (flag2 || voidSceneTimer > 0) { ... }
    }

    private static bool ApplySaintMechanics(PhysicalObject physicalObject, Player self, bool didAscendCreature)
    {
        if (didAscendCreature || physicalObject is not (Creature or Oracle))
        {
            return didAscendCreature;
        }

        if (AscensionHandler.CanReviveObject(physicalObject)
            && OptionUtils.IsOptionEnabled(Options.ALLOW_REVIVAL))
        {
            Logger.LogDebug("Attempting to revive: " + physicalObject);

            if (OptionUtils.IsOptionEnabled(Options.REQUIRE_KARMA_FLOWER))
            {
                PhysicalObject? karmaFlower = AscensionHandler.GetHeldKarmaFlower(self);

                if (karmaFlower is null)
                {
                    Logger.LogDebug("Player has no Karma Flower, ignoring.");
                    return false;
                }

                karmaFlower.Destroy();
            }

            AscensionHandler.AscendObject(physicalObject, self);

            self.DeactivateAscension();
            self.SaintStagger(80);

            self.voidSceneTimer = -1;

            didAscendCreature = true;
        }
        else if (physicalObject == self && (OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION) || self.wormCutsceneLockon))
        {
            Logger.LogDebug("Attempting to ascend: " + self.SlugCatClass);

            AscensionHandler.AscendObject(self, self);

            didAscendCreature = true;
        }

        return didAscendCreature;
    }

    /// <summary>
    /// Replaces the given <c>Player</c> argument with <c>null</c> in order to allow their ascension ability to target themselves.
    /// </summary>
    /// <param name="self">The player instance to be tested.</param>
    /// <param name="obj">The physical object the ascension ability is targeting.</param>
    /// <returns><c>null</c> if both arguments are the same, <c><paramref name="self"/></c> otherwise.</returns>
    private static Player? NullifySelfReference(Player self, PhysicalObject obj) =>
        obj == self && OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION)
            ? null
            : self;
}