using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ControlLib.Possession;

/// <summary>
/// A collection of hooks for updating creatures' possession states.
/// </summary>
public static class PossessionHooks
{
    /// <summary>
    /// Applies the Possession module's hooks to the game.
    /// </summary>
    public static void ApplyHooks()
    {
        IL.Creature.Update += UpdatePossessedCreatureILHook;
        On.Player.Update += UpdatePlayerPossessionHook;
    }

    /// <summary>
    /// Removes the Possession module's hooks from the game.
    /// </summary>
    public static void RemoveHooks()
    {
        IL.Creature.Update -= UpdatePossessedCreatureILHook;
        On.Player.Update -= UpdatePlayerPossessionHook;
    }

    /// <summary>
    /// Updates the player's possession manager. If none is found, a new one is created, then updated as well.
    /// </summary>
    private static void UpdatePlayerPossessionHook(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig.Invoke(self, eu);

        PossessionManager manager = self.GetPossessionManager();

        manager.Update();
    }

    /// <summary>
    /// Conditionally overrides the game's default behavior for taking control of creatures in Safari Mode.
    /// Also adds basic behaviors for validating a creature's possession state.
    /// </summary>
    private static void UpdatePossessedCreatureILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);
            ILLabel target = null;

            c.GotoNext(
                MoveType.After,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.MSC))),
                x => x.MatchBrfalse(out target)
            ).MoveAfterLabels();

            c.Emit(OpCodes.Ldarg_0).EmitDelegate(UpdateCreaturePossession);
            c.Emit(OpCodes.Brtrue, target);
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to apply hook: {nameof(UpdatePossessedCreatureILHook)}", ex);
        }
    }

    /// <summary>
    /// Updates the creature's possession state. If the possession is no longer valid, it is removed instead.
    /// </summary>
    /// <param name="self">The creature itself.</param>
    /// <returns><c>true</c> if the game's default behavior was overriden, <c>false</c> otherwise.</returns>
    private static bool UpdateCreaturePossession(Creature self)
    {
        if (!self.TryGetPossession(out Player player)) return false;

        PossessionManager manager = player.GetPossessionManager();

        if (!player.Consious || !manager.MyPossessions.ContainsKey(self))
        {
            manager.StopPossession(self);

            return false;
        }

        self.SafariControlInputUpdate(player.playerState.playerNumber);

        return true;
    }
}