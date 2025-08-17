using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using static AscendedSaint.AscendedSaintMain;

namespace AscendedSaint.Attunement;

/// <summary>
/// All hooks relating to Saint's unique abilities.
/// </summary>
public static class SaintMechanicsHooks
{
    private static readonly ConditionalWeakTable<PhysicalObject, AscensionCooldown> _revivedCreatures = new();

    public static bool GetAscensionCooldown(this PhysicalObject self, out AscensionCooldown result) => _revivedCreatures.TryGetValue(self, out result);

    public static void SetAscensionCooldown(this PhysicalObject self, int cooldown = 0) =>
        self.room.AddObject(_revivedCreatures.GetValue(self, (_) => new AscensionCooldown(cooldown)));

    /// <summary>
    /// The "Trigger" phase of Saint's new abilities hook. This directly hooks into the game's runtime instructions, allowing the mod to conditionally override Saint's ascension ability behaviors.
    /// </summary>
    public static void AscensionMechanicsILHook(ILContext context)
    {
        ILCursor c = new(context);
        ILLabel target = null;

        c.GotoNext(x => x.MatchStloc(18), x => x.MatchLdloc(18));
        c.GotoNext(x => x.MatchBeq(out var _));
        c.MoveAfterLabels();

        // Target: if (physicalObject != this && ...)
        //                               ^^^^ HERE (Replace)

        c.Emit(OpCodes.Ldloc, 18);
        c.EmitDelegate(NullifySelfReference);

        // Result: if (physicalObject != NullifySelfReference(physicalObject, this) && ...)

        c.GotoNext(x => x.MatchBrfalse(out var _));
        c.GotoNext(
            MoveType.After,
            x => x.MatchStfld(out var _)
        );
        c.MoveAfterLabels();

        // Target: bodyChunk.vel += Custom.RNV() * 36f; <-- HERE (Append)

        ILCursor d = new(c);

        d.GotoNext(x => x.MatchCall(typeof(RWCustom.Custom).GetMethod(nameof(RWCustom.Custom.RNV))));
        d.GotoPrev(x => x.MatchBr(out target));

        // Target: for (int n = 0; n < 20; n++) { ... }
        //         ^^^ HERE (No-Op) (Referenced for branch injection)

        c.Emit(OpCodes.Ldloc, 21); // bodyChunk
        c.Emit(OpCodes.Ldarg_0); // this
        c.Emit(OpCodes.Ldloc, 15); // flag2 (didAscendCreature)
        c.EmitDelegate(TryApplySaintMechanics); // bodyChunk, this, flag2
        c.Emit(OpCodes.Brtrue, target); // if (TryApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1700;

        // Result: bodyChunk.vel += Custom.RNV() * 36f; if (TryApplySaintMechanics(bodyChunk, this, flag2)) goto IL_1700;
    }

    private static bool TryApplySaintMechanics(BodyChunk bodyChunk, Player self, bool didAscendCreature)
    {
        ASLogger.LogDebug($"Checking {bodyChunk.owner}...");

        if ((bodyChunk.owner is not Creature && bodyChunk.owner is not Oracle) || bodyChunk.owner.GetAscensionCooldown(out var _)) return false;

        ASLogger.LogDebug($"{bodyChunk.owner} is a valid target!");

        if (self.room.game.session is StoryGameSession storySession)
        {
            DeathPersistentSaveData saveData = storySession.saveState.deathPersistentSaveData;

            ASLogger.LogDebug($"# WORLD STATE IS: [ripMoon: {saveData.ripMoon}; ripPebbles: {saveData.ripPebbles}]");
        }

        return ApplySaintMechanics(bodyChunk.owner, self, false);
    }

    /// <summary>
    /// The "Execution" phase of Saint's new abilities hook; Contains the actual code for applying new behaviors to Saint's Ascension ability (for instance, reviving creatures).
    /// </summary>
    /// <param name="self">The <c>Player</c> object of the previous phase.</param>
    /// <param name="physicalObject">The <c>PhysicalObject</c> who passed the previous phase's checks.</param>
    private static bool ApplySaintMechanics(PhysicalObject physicalObject, Player self, bool didAscendCreature)
    {
        if (didAscendCreature) return false;

        if (ASUtils.CanReviveCreature(physicalObject) && ClientOptions.allowRevival)
        {
            ASLogger.LogDebug("Attempting to revive: " + physicalObject);

            if (ClientOptions.requireKarmaFlower)
            {
                PhysicalObject karmaFlower = ASUtils.GetHeldKarmaFlower(self);

                if (karmaFlower is null)
                {
                    ASLogger.LogDebug("Player has no Karma Flower, ignoring.");
                    return false;
                }

                karmaFlower.Destroy();
            }

            ASUtils.AscendCreature(physicalObject, self);

            physicalObject.SetAscensionCooldown(20);

            self.DeactivateAscension();
            self.SaintStagger(80);

            return true;
        }
        else if (physicalObject == self && (ClientOptions.allowSelfAscension || self.wormCutsceneLockon))
        {
            ASLogger.LogDebug("Attempting to ascend: " + self.SlugCatClass);

            ASUtils.AscendCreature(physicalObject, self);

            physicalObject.SetAscensionCooldown(20);

            self.voidSceneTimer = 1;

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
    private static Player NullifySelfReference(Player self, PhysicalObject obj) => obj == self ? null : self;
}

public class AscensionCooldown(int cooldown) : UpdatableAndDeletable
{
    public int Cooldown => cooldown;

    public override void Update(bool eu)
    {
        base.Update(eu);

        cooldown--;

        if (cooldown < 1)
        {
            Destroy();
        }
    }
}