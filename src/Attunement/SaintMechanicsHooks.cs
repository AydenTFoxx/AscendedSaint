using System.Collections.Generic;
using System.Linq;
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

    internal static void UpdateCooldowns()
    {
        foreach (KeyValuePair<PhysicalObject, AscensionCooldown> cooldown in _ascensionCooldowns)
        {
            cooldown.Value.Update(Time.deltaTime);
        }

        _ascensionCooldowns = [.. _ascensionCooldowns.Where(c => !c.Value.IsExpired)];
    }

    /// <summary>
    ///     The "Trigger" phase of Saint's new abilities hook.
    ///     This directly hooks into the game's runtime instructions, allowing the mod to conditionally override Saint's ascension ability behaviors.
    /// </summary>
    public static void AscensionMechanicsILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel? target = null;

        c.GotoNext(
            static x => x.MatchLdloc(18), // physicalObject
            static x => x.MatchLdarg(0), // this
            x => x.MatchBeq(out target) // (end of if block)
        );
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

        c.Emit(OpCodes.Ldloc, 21); // bodyChunk
        c.Emit(OpCodes.Ldarg_0); // this
        c.Emit(OpCodes.Ldloc, 15); // flag2 (didAscendCreature)
        c.EmitDelegate(TryApplySaintMechanics); // bodyChunk, this, flag2
        c.Emit(OpCodes.Brtrue, target); // if (TryApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1435;

        // Result: bodyChunk.vel += Custom.RNV() * 36f; if (TryApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1435;

        c.GotoNext(MoveType.Before,
            static x => x.MatchLdloc(15),
            static x => x.MatchBrtrue(out _)
        ).GotoNext();
        c.MoveAfterLabels();

        // Target: if (flag2 || voidSceneTimer > 0) { ... }
        //             ^^^^^ HERE (Replace)

        ILCursor d = new(c);

        d.GotoNext(MoveType.Before,
            static x => x.MatchLdcI4(0),
            static x => x.MatchStloc(28)
        );

        ILLabel target2 = d.MarkLabel(); // for (int m = 0; m < 20; m++) { ... }

        c.Emit(OpCodes.Ldarg_0).Emit(OpCodes.Ldfld, typeof(Player).GetField(nameof(Player.voidSceneTimer))); // this.voidSceneTimer
        c.EmitDelegate(WasRevivalAscension);

        // Result: if (WasRevivalAscension(flag2, voidSceneTimer) || voidSceneTimer > 0) { ... }
    }

    private static bool TryApplySaintMechanics(BodyChunk bodyChunk, Player self, bool didAscendCreature) =>
        bodyChunk.owner is Creature or Oracle
        && !bodyChunk.owner.HasAscensionCooldown()
        && ApplySaintMechanics(bodyChunk.owner, self, didAscendCreature);

    /// <summary>
    ///     The "Execution" phase of Saint's new abilities hook;
    ///     Contains the actual code for applying new behaviors to Saint's Ascension ability (for instance, reviving creatures).
    /// </summary>
    /// <param name="self">The <c>Player</c> object of the previous phase.</param>
    /// <param name="physicalObject">The <c>PhysicalObject</c> who passed the previous phase's checks.</param>
    private static bool ApplySaintMechanics(PhysicalObject physicalObject, Player self, bool didAscendCreature)
    {
        if (didAscendCreature) return false;

        if (physicalObject is Creature or Oracle && OptionUtils.IsOptionEnabled(Options.ALLOW_REVIVAL))
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

            physicalObject.SetAscensionCooldown(defaultCooldown);

            self.DeactivateAscension();
            self.SaintStagger(80);

            return true;
        }
        else if (physicalObject == self && (OptionUtils.IsOptionEnabled(Options.ALLOW_SELF_ASCENSION) || self.wormCutsceneLockon))
        {
            ModLib.Logger.LogDebug("Attempting to ascend: " + self.SlugCatClass);

            AscensionHandler.AscendCreature(self, self);

            physicalObject.SetAscensionCooldown(defaultCooldown);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces the given <c>Player</c> argument with <c>null</c> in order to allow the player's ascension to target themselves.
    /// </summary>
    /// <param name="self">The player instance to be tested.</param>
    /// <param name="obj">The physical object the ascension ability is targeting.</param>
    /// <returns><c>null</c> if both arguments are the same, <c><paramref name="self"/></c> otherwise.</returns>
    private static Player? NullifySelfReference(Player self, PhysicalObject obj) => obj == self ? null : self;

    private static bool WasRevivalAscension(bool didAscendCreature, float voidSceneTimer) => didAscendCreature && voidSceneTimer < 0;

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