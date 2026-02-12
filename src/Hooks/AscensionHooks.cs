using AscendedSaint.Attunement;
using ModLib;
using ModLib.Meadow;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace AscendedSaint.Hooks;

/// <summary>
/// All hooks relating to Saint's new abilities and behaviors.
/// </summary>
internal static class AscensionHooks
{
    public static void ApplyHooks()
    {
        IL.Player.ClassMechanicsSaint += AllowSelfAscensionILHook;
        IL.Player.ClassMechanicsSaint += EnableRevivalFeatureILHook;
        IL.Player.ClassMechanicsSaint += PreventAscensionSoundILHook;
    }

    public static void RemoveHooks()
    {
        IL.Player.ClassMechanicsSaint -= AllowSelfAscensionILHook;
        IL.Player.ClassMechanicsSaint -= EnableRevivalFeatureILHook;
        IL.Player.ClassMechanicsSaint -= PreventAscensionSoundILHook;
    }

    /// <summary>
    /// Allows the player to ascend themselves with Saint's attunement ability. Toggleable with a REMIX option.
    /// </summary>
    private static void AllowSelfAscensionILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(
            static x => x.MatchLdloc(18), // physicalObject
            static x => x.MatchLdarg(0), // this
            static x => x.MatchBeq(out _) // (end of if block)
        ).GotoNext(MoveType.After).MoveAfterLabels();

        // Target: if (physicalObject != this && ...)
        //                               ^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldloc, 18) // physicalObject
         .EmitDelegate(NullifySelfReference); // this, physicalObject

        // Result: if (physicalObject != NullifySelfReference(this, physicalObject) && ...)

        static Player? NullifySelfReference(Player self, PhysicalObject obj)
        {
            return self == obj && OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION) ? null : self;
        }
    }

    /// <summary>
    /// Allows the player to revive creatures and iterators using Saint's attunement ability.
    /// Toggleable with a REMIX option; Optionally requires a Karma Flower per revived target.
    /// </summary>
    private static void EnableRevivalFeatureILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(MoveType.Before,
            static x => x.MatchLdloc(20),
            static x => x.MatchLdcI4(1),
            static x => x.MatchAdd(),
            static x => x.MatchStloc(20)
        );

        ILLabel target = c.MarkLabel();

        // Target: foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
        //                                      ^^ HERE (No-op; Referenced for later use)

        ILCursor d = new(context);

        d.GotoNext(static x => x.MatchStloc(21))
         .GotoNext(MoveType.After,
            static x => x.MatchStfld(typeof(BodyChunk).GetField(nameof(BodyChunk.vel)))
        ).MoveAfterLabels();

        // Target: bodyChunk.vel += Custom.RNV() * 36f; <-- HERE (Append)

        d.Emit(OpCodes.Ldarg_0) // this (self)
         .Emit(OpCodes.Ldloc, 18) // physicalObject (target)
         .Emit(OpCodes.Ldloc, 15) // flag2 (didAscendCreature)
         .EmitDelegate(ApplySaintMechanics); // this, physicalObject, flag2

        d.Emit(OpCodes.Dup)
         .Emit(OpCodes.Stloc, 15) // flag2 = ApplySaintMechanics(this, physicalObject, flag2);
         .Emit(OpCodes.Brtrue, target); // if (flag2) continue;

        // Result:
        //      bodyChunk.vel += Custom.RNV() * 36f;
        //      flag2 = ApplySaintMechanics(bodyChunk, this, flag2);
        //      if (flag2)
        //          continue;
    }

    /// <summary>
    /// Prevents the usual sound effects for Saint's attunement ability to play when a revival is performed.
    /// </summary>
    private static void PreventAscensionSoundILHook(ILContext context)
    {
        ILCursor c = new(context);

        c.GotoNext(static x => x.MatchStloc(26))
         .GotoNext(MoveType.Before,
            static x => x.MatchLdloc(15),
            static x => x.MatchBrtrue(out _)
        ).MoveAfterLabels();

        // Target: if (flag2 || voidSceneTimer > 0) { ... }
        //        ^ HERE (Prepend)

        ILCursor d = new(c);

        d.GotoNext(MoveType.Before,
            static x => x.MatchLdcI4(0),
            static x => x.MatchStloc(28)
        );

        ILLabel target = d.MarkLabel();

        // Target: for (int m = 0; m < 20; m++) { ... }
        //              ^^^^^^^^^ HERE (No-op; Referenced for later use)

        c.Emit(OpCodes.Ldarg_0)
         .EmitDelegate(IgnoreAscensionSound);

        c.Emit(OpCodes.Brtrue, target);

        // Result: if (IgnoreAscensionSound(this)) { goto IL_156c; } else if (flag2 || voidSceneTimer > 0) { ... }

        static bool IgnoreAscensionSound(Player self)
        {
            if (self.voidSceneTimer == -1)
            {
                self.voidSceneTimer = 0;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Applies the custom mechanics of this mod for Saint's attunement ability.
    /// </summary>
    /// <param name="target">The targeted object for ascension/revival.</param>
    /// <param name="self">The player itself.</param>
    /// <param name="didAscendCreature">
    ///     If any ascension or revival has been performed;
    ///     Corresponds to the local variable "flag2" in the original (decompiled) code.
    /// </param>
    /// <returns></returns>
    private static bool ApplySaintMechanics(Player self, PhysicalObject target, bool didAscendCreature)
    {
        if ((Extras.IsOnlineSession && !MeadowUtils.IsMine(self))
            || target is not (Creature or Oracle)
            || target.HasAscensionCooldown())
            return didAscendCreature;

        if (target != self && target is Creature { dead: false } or Oracle { Alive: true })
        {
            target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown * 0.25f);

            return didAscendCreature;
        }

        bool requireKarmaFlower = OptionUtils.IsOptionEnabled(Options.REQUIRE_KARMA_FLOWER) && target != self;
        KarmaFlower? karmaFlower = requireKarmaFlower
            ? AscensionHandler.GetHeldKarmaFlower(self)
            : null;

        if (requireKarmaFlower && karmaFlower is null)
        {
            Main.Logger.LogDebug($"Player has no Karma Flower, ignoring: {target}");

            target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown * 0.5f);
        }
        else
        {
            Main.Logger.LogDebug($"Attempting to ascend or revive: {target}");

            bool result = AscensionHandler.TryAscendObject(target, self);

            if (result)
            {
                didAscendCreature = true;

                if (target == self)
                {
                    Extras.GrantFakeAchievement($"{Main.MOD_GUID}/self_ascension");
                }
                else
                {
                    Extras.GrantFakeAchievement($"{Main.MOD_GUID}/revival");

                    karmaFlower?.Destroy();

                    self.SaintStagger(80);

                    self.burstX = 0f;
                    self.burstY = 0f;

                    self.monkAscension = false;

                    self.voidSceneTimer = -1; // Prevents the sound effects for actual ascension from playing later
                }
            }

            target.SetAscensionCooldown(AscensionHandler.DefaultAscensionCooldown, isRevival: result && target != self);
        }

        return didAscendCreature;
    }
}