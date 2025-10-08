using System.Collections.Generic;
using System.Linq;
using ModLib;
using ModLib.Generics;
using ModLib.Options;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace AscendedSaint.Attunement;

/// <summary>
/// All hooks relating to Saint's unique abilities.
/// </summary>
public static class SaintMechanicsHooks
{
    private static WeakDictionary<PhysicalObject, AscensionCooldown> _ascensionCooldowns = [];
    private static readonly float defaultCooldown = Time.fixedDeltaTime * 20f;

    /// <summary>
    /// Determines if a given creature is on cooldown to be ascended or revived.
    /// </summary>
    /// <param name="self">The creature or object itself.</param>
    /// <returns><c>true</c> if the creature is on cooldown, <c>false</c> otherwise.</returns>
    private static bool HasAscensionCooldown(this PhysicalObject self) => self.GetAscensionCooldown() > 0f;

    /// <summary>
    /// Retrieves a given creature's ascension cooldown value, if any.
    /// </summary>
    /// <param name="self">The creature or object itself.</param>
    /// <param name="result">The retrieved cooldown value, if any.</param>
    /// <returns><c>true</c> if a value was retrieved, <c>false</c> otherwise.</returns>
    private static float GetAscensionCooldown(this PhysicalObject self) =>
        _ascensionCooldowns.TryGetValue(self, out AscensionCooldown result) && !result.IsExpired
            ? result.Duration
            : 0f;

    /// <summary>
    /// Sets a given creature's ascension coldown value.
    /// </summary>
    /// <param name="self">The creature or object itself.</param>
    /// <param name="cooldown">The cooldown to be applied.</param>
    private static void SetAscensionCooldown(this PhysicalObject self, float duration) =>
        _ascensionCooldowns.Add(self, new AscensionCooldown(duration));

    public static void UpdateCooldowns()
    {
        bool expired = false;

        foreach (KeyValuePair<PhysicalObject, AscensionCooldown> cooldown in _ascensionCooldowns)
        {
            cooldown.Value.Update(Time.deltaTime);

            expired = cooldown.Value.IsExpired;
        }

        if (expired)
        {
            _ascensionCooldowns = [.. _ascensionCooldowns.Where(c => !c.Value.IsExpired)];
        }
    }

    public static void AddHooks()
    {
        IL.Player.ClassMechanicsSaint += Extras.WrapILHook(AscensionMechanicsILHook);
    }

    public static void RemoveHooks()
    {
        IL.Player.ClassMechanicsSaint -= Extras.WrapILHook(AscensionMechanicsILHook);
    }

    /// <summary>
    ///     The "Trigger" phase of Saint's new abilities hook.
    ///     This directly hooks into the game's runtime instructions, allowing the mod to conditionally override Saint's ascension ability behaviors.
    /// </summary>
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

    /// <summary>
    ///     The "Execution" phase of Saint's new abilities hook;
    ///     Contains the actual code for applying new behaviors to Saint's Ascension ability (for instance, reviving creatures).
    /// </summary>
    /// <param name="physicalObject">The object to be ascended or revived..</param>
    /// <param name="self">The player itself who caused this action.</param>
    private static bool ApplySaintMechanics(PhysicalObject physicalObject, Player self, bool didAscendCreature)
    {
        if (didAscendCreature || physicalObject is not (Creature or Oracle) || physicalObject.HasAscensionCooldown())
        {
            return didAscendCreature;
        }

        if (AscensionHandler.CanReviveObject(physicalObject)
            && OptionUtils.IsOptionEnabled(Options.ALLOW_REVIVAL))
        {
            ModLib.Logger.LogDebug("Attempting to revive: " + physicalObject);

            if (OptionUtils.IsOptionEnabled(Options.REQUIRE_KARMA_FLOWER))
            {
                PhysicalObject? karmaFlower = AscensionHandler.GetHeldKarmaFlower(self);

                if (karmaFlower is null)
                {
                    ModLib.Logger.LogDebug("Player has no Karma Flower, ignoring.");
                    return false;
                }

                karmaFlower.Destroy();
            }

            if (physicalObject is Creature creature)
            {
                AscensionHandler.AscendCreature(creature, self);
            }
            else
            {
                AscensionHandler.AscendOracle((Oracle)physicalObject, self);
            }

            self.DeactivateAscension();
            self.SaintStagger(80);

            self.voidSceneTimer = -1;

            didAscendCreature = true;
        }
        else if (physicalObject == self && (OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION) || self.wormCutsceneLockon))
        {
            ModLib.Logger.LogDebug("Attempting to ascend: " + self.SlugCatClass);

            AscensionHandler.AscendCreature(self, self);

            didAscendCreature = true;
        }

        physicalObject.SetAscensionCooldown(defaultCooldown);

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

    /// <summary>
    /// Represents a time limit where a creature cannot be affected by Saint's abilities.
    /// </summary>
    /// <param name="Duration">The amount of time the creature will be immune for.</param>
    private sealed record AscensionCooldown(float Duration)
    {
        public float Duration { get; private set; } = Duration;
        public bool IsExpired { get; private set; }

        public void Update(float deltaTime)
        {
            if (IsExpired) return;

            Duration -= deltaTime;

            if (Duration <= 0f)
            {
                IsExpired = true;
            }
        }

        public override string ToString() => Duration.ToString();
    }
}