using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace ControlLib.Possession;

public static class PossessionHooks
{
    public static void ApplyHooks()
    {
        IL.Creature.Update += UpdatePossessedCreatureILHook;
        IL.Player.Update += UpdatePossessingPlayerILHook;
    }

    public static void RemoveHooks()
    {
        IL.Creature.Update -= UpdatePossessedCreatureILHook;
        IL.Player.Update -= UpdatePossessingPlayerILHook;
    }

    /// <summary>
    /// Overrides the game's default behavior for overriding a creature's controls so it can (potentially) support more than one player at once.
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
    /// Prevents possessing players from registering inputs, while also updating their <c>PossessionState</c> values.
    /// </summary>
    private static void UpdatePossessingPlayerILHook(ILContext context)
    {
        try
        {
            ILCursor c = new(context);

            c.GotoNext(static x => x.MatchCall(typeof(Player).GetMethod(nameof(Player.checkInput))));

            ILCursor d = new(c);

            c.GotoPrev().MoveAfterLabels();
            d.GotoNext().MoveAfterLabels();

            ILLabel target = d.MarkLabel();

            c.Emit(OpCodes.Ldarg_0).EmitDelegate(UpdatePlayerPossessionState);
            c.Emit(OpCodes.Brtrue, target);
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to apply hook: {nameof(UpdatePossessingPlayerILHook)}", ex);
        }
    }

    private static bool UpdateCreaturePossession(Creature self)
    {
        if (!self.TryGetPossession(out Player player) || !player.Consious) return false;

        PossessionManager manager = player.GetPossessionManager();

        if (!manager.MyPossessions.ContainsKey(self))
        {
            manager.StopPossession(self);

            self.UpdateCachedPossession();

            return false;
        }

        self.SafariControlInputUpdate(player.playerState.playerNumber);

        return true;
    }

    private static bool UpdatePlayerPossessionState(Player self)
    {
        PossessionManager manager = self.GetPossessionManager();

        manager.Update();

        if (manager.IsPossessing)
        {
            for (int i = 0; i < self.input.Length; i++)
            {
                self.input[i] = new Player.InputPackage();
            }
        }

        return manager.IsPossessing;
    }
}