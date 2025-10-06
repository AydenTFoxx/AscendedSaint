using System.Runtime.CompilerServices;
using RainMeadow;

namespace AscendedSaint.Meadow;

/// <summary>
/// Helper methods for retrieving the "online" counterpart of local objects.
/// </summary>
public static class OnlineQueries
{
    /// <summary>
    /// Holds weak references to previously queried <c>OnlinePlayer</c> names. If a query is made for a key which is present here, its value is returned instead.
    /// </summary>
    private static readonly ConditionalWeakTable<Player, string> _cachedPlayerNames = new();

    /// <summary>
    /// Obtains the online name of the given player.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>null</c> if none is found.</returns>
    public static string? GetPlayerName(Player self)
    {
        if (_cachedPlayerNames.TryGetValue(self, out string value))
        {
            return value;
        }

        string? newName = QueryPlayerName(self);

        if (newName is null) return null; // Cannot use string.IsNullOrEmpty() here, NotNullWhenAttribute does not yet exist :(

        _cachedPlayerNames.Add(self, newName);

        return newName;
    }

    /// <summary>
    /// Queries the lobby's online players in order to find the name of a given player.
    /// </summary>
    /// <param name="physicalObject">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>null</c> if none is found.</returns>
    private static string? QueryPlayerName(Player player)
    {
        OnlinePhysicalObject? playerOPO = player.abstractPhysicalObject.GetOnlineObject();

        if (playerOPO is null)
        {
            ModLib.Logger.LogWarning("Failed to retrieve the OnlinePhysicalObject representation of the player, aborting.");
            return null;
        }

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (playerOPO.owner == onlinePlayer) return onlinePlayer.id.GetPersonaName();
        }

        ModLib.Logger.LogWarning($"Failed to retrieve the OnlinePlayer instance of {player}!");

        return null;
    }
}