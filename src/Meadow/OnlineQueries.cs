using RainMeadow;

namespace AscendedSaint.Meadow;

/// <summary>
/// Helper methods for retrieving the "online" counterpart of local objects.
/// </summary>
public static class OnlineQueries
{
    /// <summary>
    /// Obtains the online name of the given player.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>null</c> if none is found.</returns>
    public static string? GetOnlineName(this Player self)
    {
        OnlinePhysicalObject? playerOPO = self.abstractPhysicalObject.GetOnlineObject();

        if (playerOPO is null)
        {
            ModLib.Logger.LogWarning("Failed to retrieve the OnlinePhysicalObject representation of the player, aborting.");
            return null;
        }

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (playerOPO.owner == onlinePlayer)
                return onlinePlayer.id.GetPersonaName();
        }

        ModLib.Logger.LogWarning($"Failed to retrieve the OnlinePlayer instance of {self}!");

        return null;
    }
}