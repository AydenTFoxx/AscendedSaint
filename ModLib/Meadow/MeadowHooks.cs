using System;
using ModLib.Options;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Rain Meadow-specific hooks and events, which are only enabled when the mod itself is present.
/// </summary>
public static class MeadowHooks
{
    private static readonly WeakReference<GameSession> LastGameSession = new(null!);

    /// <summary>
    ///     Applies all Rain Meadow-specific hooks to the game.
    /// </summary>
    public static void AddHooks()
    {
        On.GameSession.ctor += GameSessionHook;
        On.RainWorldGame.Update += GameUpdateHook;
    }

    /// <summary>
    ///     Removes all Rain Meadow-specific hooks from the game.
    /// </summary>
    public static void RemoveHooks()
    {
        On.GameSession.ctor -= GameSessionHook;
        On.RainWorldGame.Update -= GameUpdateHook;
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (LastGameSession.TryGetTarget(out _)) return;

        OptionUtils.SharedOptions.RefreshOptions(!MeadowUtils.IsHost);

        if (!MeadowUtils.IsHost)
        {
            OnlineManager.lobby.owner.SendRPCEvent(ModRPCs.RequestSyncRemixOptions, OnlineManager.mePlayer);
        }

        LastGameSession.SetTarget(self);
    }

    /// <summary>
    ///     Updates the RPC manager on every game tick.
    /// </summary>
    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        ModRPCManager.UpdateRPCs();
    }
}