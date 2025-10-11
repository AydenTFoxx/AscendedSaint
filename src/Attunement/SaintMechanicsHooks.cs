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

        c.GotoNext(MoveType.After,
            static x => x.MatchLdloc(18), // physicalObject
            static x => x.MatchLdarg(0), // this
            static x => x.MatchBeq(out _) // (end of if block)
        );
        c.GotoPrev(MoveType.After).MoveAfterLabels();

        // Target: if (physicalObject != this && ...)
        //                               ^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldloc, 18); // physicalObject
        c.EmitDelegate(NullifySelfReference); // this, physicalObject

        // Result: if (physicalObject != NullifySelfReference(this, physicalObject) && ...)

        ILCursor d = new(c);

        d.GotoNext(MoveType.Before,
            static x => x.MatchLdloc(20),
            static x => x.MatchLdcI4(1),
            static x => x.MatchAdd(),
            static x => x.MatchStloc(20)
        );

        ILLabel target1 = d.MarkLabel();

        // Target: foreach (BodyChunk bodyChunk in physicalObject.bodyChunks) { ... }
        //         ^^^^^^^ HERE (No-op; Referenced for later use)

        c.GotoNext(static x => x.MatchBrfalse(out _));
        c.GotoNext(MoveType.After,
            static x => x.MatchStfld(out _)
        );
        c.MoveAfterLabels();

        // Target: bodyChunk.vel += Custom.RNV() * 36f; <-- HERE (Append)

        c.Emit(OpCodes.Ldloc, 18); // physicalObject
        c.Emit(OpCodes.Ldarg_0); // this
        c.Emit(OpCodes.Ldloc, 15); // flag2 (didAscendCreature)
        c.EmitDelegate(ApplySaintMechanics); // physicalObject, this, flag2
        c.Emit(OpCodes.Dup);
        c.Emit(OpCodes.Stloc, 15); // flag2 = ApplySaintMechanics(bodyChunk, this, flag2);
        c.Emit(OpCodes.Brtrue, target1); // if (flag2) continue;

        // Result:
        //      bodyChunk.vel += Custom.RNV() * 36f;
        //      flag2 = ApplySaintMechanics(physicalObject, this, flag2);
        //      if (flag2)
        //          continue;

        c.GotoNext(MoveType.Before,
            static x => x.MatchLdloc(15),
            static x => x.MatchBrtrue(out _)
        ).MoveAfterLabels();

        // Target: if (flag2 || voidSceneTimer > 0) { ... }
        //        ^ HERE (Prepend)

        ILCursor d1 = new(c);

        d1.GotoNext(MoveType.Before,
            static x => x.MatchLdcI4(0),
            static x => x.MatchStloc(28)
        ); // for (int m = 0; m < 20; m++) { ... }

        ILLabel target2 = d1.MarkLabel();

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
            if (!self.monkAscension)
            {
                self.burstX = 0f;
                self.burstY = 0f;
            }

            return didAscendCreature;
        }

        bool requireKarmaFlower = OptionUtils.IsOptionEnabled(Options.REQUIRE_KARMA_FLOWER) && physicalObject != self;
        KarmaFlower? karmaFlower = requireKarmaFlower
            ? AscensionHandler.GetHeldKarmaFlower(self)
            : null;

        if (requireKarmaFlower && karmaFlower is null)
        {
            Logger.LogDebug("Player has no Karma Flower, ignoring.");

            return didAscendCreature;
        }

        bool result = AscensionHandler.TryAscendObject(physicalObject, self);

        if (result)
        {
            didAscendCreature = true;

            self.SaintStagger(80);

            self.monkAscension = false;

            self.burstX = 0f;
            self.burstY = 0f;

            if (physicalObject != self)
                self.voidSceneTimer = -1;

            karmaFlower?.Destroy();
        }

        return didAscendCreature;
    }

    private static Player? NullifySelfReference(Player self, PhysicalObject obj) =>
        self == obj && OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION) ? null : self;
}