using System;
using System.Security.Permissions;
using ModLib.Meadow;
using ModLib.Options;
using MonoMod.Cil;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace ModLib;

public static class Extras
{
    /// <summary>
    /// Wraps a given action in a try-catch, safely performing its code while handling potential exceptions.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    /// <remarks>Use sparsely; If possible, avoid throwing an exception at all instead of using this method.</remarks>
    public static void WrapAction(Action action)
    {
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to run wrapped action: {action.Method.Name}", ex);
        }
    }

    /// <summary>
    /// Wraps a given IL hook in a try-catch, preventing it from breaking other code when applied.
    /// </summary>
    /// <param name="action">The hook method to be wrapped.</param>
    /// <returns>An <c>ILContext.Manipulator</c> instance to be passed in place of the method itself.</returns>
    /// <remarks>Usage of this method is akin to the original <c>WrapInit</c> method; See SlugTemplate for an example of this.</remarks>
    public static ILContext.Manipulator WrapILHook(Action<ILContext> action)
    {
        return (context) =>
        {
            try
            {
                action.Invoke(context);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to apply IL hook: {action.Method.Name}", ex);
            }
        };
    }

    /// <summary>
    /// Ensures the client's <c>SharedOptions</c> object is always synced with the values from the mod's REMIX menu.
    /// </summary>
    public static void AddPlayerHook(On.GameSession.orig_AddPlayer orig, GameSession self, AbstractCreature player)
    {
        orig.Invoke(self, player);

        bool isOnlineSession = CompatibilityManager.IsRainMeadowEnabled() && MeadowUtils.IsOnline;

        if (self.game.Players.Count <= 1 && (!isOnlineSession || MeadowUtils.IsHost))
        {
            OptionUtils.SharedOptions.RefreshOptions(isOnlineSession);
        }
        else
        {
            // Likely a Jolly Co-op session or similar; No need to sync again.
            Logger.LogDebug($"{self.game.FirstAnyPlayer} is already present, ignoring.");
        }
    }

    /// <summary>
    /// Syncs the player's <c>SharedOptions</c> object with the host of the current online lobby.
    /// </summary>
    public static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (CompatibilityManager.IsRainMeadowEnabled() && !MeadowUtils.IsHost)
        {
            OptionUtils.SharedOptions.SetOptions(null);

            MeadowUtils.RequestOptionsSync();
        }
    }
}