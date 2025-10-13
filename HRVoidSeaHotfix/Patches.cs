using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace HRVoidSeaHotfix;

/// <summary>
/// Collection of temporary fixes for Saint's ending sequence.
/// </summary>
/// <remarks>Notice: Most paths here work by simply preventing faulty code from running; They are meant as a fix, not a solution.</remarks>
public static class Patches
{
    private static readonly int _totalHooks = 2;
    private static int _appliedHooks = _totalHooks;

    /// <summary>
    /// Applies this mod's patches to the Void Sea ending sequence.
    /// </summary>
    public static void AddHooks()
    {
        IL.VoidSea.VoidSeaScene.Update += VoidSeaUpdateILHook;
        IL.VoidSea.VoidSeaScene.VoidSeaTreatment += VoidSeaTreatmentILHook;

        ModLib.Logger.LogDebug($"Successfully applied ({_appliedHooks}/{_totalHooks}) patches to the game.");
    }

    /// <summary>
    /// Removes this mod's patches to the Void Sea ending sequence.
    /// </summary>
    public static void RemoveHooks()
    {
        IL.VoidSea.VoidSeaScene.Update -= VoidSeaUpdateILHook;
        IL.VoidSea.VoidSeaScene.VoidSeaTreatment -= VoidSeaTreatmentILHook;

        ModLib.Logger.LogDebug($"Removed patches from the game.");
    }

    /// <summary>
    /// Prevents Saint from drowning in the room leading to the Void Sea (HR_FINAL).
    /// </summary>
    private static void VoidSeaTreatmentILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);
            ILLabel target = null!;

            c.GotoNext(MoveType.After,
                static x => x.MatchLdcI4(1),
                static x => x.MatchStloc(0)
            ).MoveAfterLabels();

            // Target: bool flag = true; <-- HERE (Append)

            ILCursor d = new(c);

            d.GotoNext(x => x.MatchBrfalse(out target));

            // Target: if (Region.IsRubiconRegion(room.world.name)) { ... }
            //        ^ HERE (Prepend)

            c.Emit(OpCodes.Br, target);

            // Result: if (true) { // do nothing } else if (Region.IsRubiconRegion(room.world.name)) { ... }
        }
        catch (Exception ex)
        {
            ModLib.Logger.LogError($"Failed to apply IL hook: {nameof(VoidSeaTreatmentILHook)}", ex);

            _appliedHooks--;
        }
    }

    /// <summary>
    /// Re-enables the Void Sea's subtracks for Saint.
    /// </summary>
    private static void VoidSeaUpdateILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            c.GotoNext(MoveType.After,
                static x => x.MatchCallvirt(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.Players)).GetGetMethod()),
                static x => x.MatchCallvirt(typeof(List<AbstractCreature>).GetProperty(nameof(List<>.Count)).GetGetMethod()),
                static x => x.MatchBlt(out _)
            ).MoveAfterLabels();

            // Target: if (Inverted) { ... }
            //        ^ HERE (Prepend)

            ILCursor d = new(c);
            ILLabel target = null!;

            d.GotoNext(x => x.MatchBrfalse(out target));

            // Target: if (Inverted) { ... }
            //         ^^ HERE (No-op; Referenced for later use)

            c.Emit(OpCodes.Br, target);

            // Result: if (true) { // do nothing } else if (Inverted) { ... }
        }
        catch (Exception ex)
        {
            ModLib.Logger.LogError($"Failed to apply IL hook: {nameof(VoidSeaTreatmentILHook)}", ex);

            _appliedHooks--;
        }
    }
}