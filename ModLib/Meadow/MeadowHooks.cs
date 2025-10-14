using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Rain Meadow-specific hooks and events, which are only enabled when the mod itself is present.
/// </summary>
public static class MeadowHooks
{
    /// <summary>
    ///     Applies all Rain Meadow-specific hooks to the game.
    /// </summary>
    public static void AddHooks()
    {
        On.RainWorldGame.Update += GameUpdateHook;

        MatchmakingManager.OnLobbyJoined += JoinLobbyHook;
    }

    /// <summary>
    ///     Removes all Rain Meadow-specific hooks from the game.
    /// </summary>
    public static void RemoveHooks()
    {
        On.RainWorldGame.Update -= GameUpdateHook;

        MatchmakingManager.OnLobbyJoined -= JoinLobbyHook;
    }

    /// <summary>
    ///     Updates the RPC manager on every game tick.
    /// </summary>
    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        ModRPCManager.UpdateRPCs();
    }

    /// <summary>
    ///     Requests the owner of the joined online lobby to sync their REMIX options with the player.
    /// </summary>
    private static void JoinLobbyHook(bool ok, string error)
    {
        if (!ok || MeadowUtils.IsHost) return;

        OnlineManager.lobby.owner.SendRPCEvent(ModRPCs.RequestSyncRemixOptions, OnlineManager.mePlayer);
    }
}