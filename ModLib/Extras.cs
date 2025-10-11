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

/// <summary>
///     A collection of utilities and wrappers for common modding activities.
/// </summary>
public static class Extras
{
    /// <summary>
    ///     Whether or not the Rain Meadow mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsMeadowEnabled { get; set; }

    /// <summary>
    ///     Whether or not the Improved Input Config: Extended mod is present. This value is cached for performance purposes.
    /// </summary>
    public static bool IsIICEnabled { get; set; }

    /// <summary>
    ///     If the current game session is on an online lobby.
    /// </summary>
    public static bool IsOnlineSession => IsMeadowEnabled && MeadowUtils.IsOnline;

    /// <summary>
    ///     If the player is the host of the current game session. On Singleplayer, this is always true.
    /// </summary>
    public static bool IsHostPlayer => !IsMeadowEnabled || MeadowUtils.IsHost;

    /// <summary>
    ///     Wraps a given action in a try-catch, safely performing its code while handling potential exceptions.
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
    ///     Wraps a given IL hook in a try-catch, preventing it from breaking other code when applied.
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
    ///     Refreshes the local <see cref="OptionUtils.SharedOptions"/> instance with the host player's REMIX options.
    /// </summary>
    internal static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        orig.Invoke(self, game);

        if (IsHostPlayer)
        {
            OptionUtils.SharedOptions.RefreshOptions(IsOnlineSession);
        }
    }
}