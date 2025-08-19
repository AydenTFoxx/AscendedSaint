using System.Runtime.CompilerServices;
using RainMeadow;

namespace AscendedSaint.Attunement;

/// <summary>
/// Holds CTWs with helper functions for easy caching and retrieval of commonly used objects.
/// </summary>
public static class ASMeadowQueries
{
    /// <summary>
    /// Holds weak references to previously queried <c>OnlinePhysicalEntity</c> instances. If a query is made for a key which is present here, its value is returned instead.
    /// </summary>
    private static readonly ConditionalWeakTable<PhysicalObject, object> _cachedOnlineObjects = new();

    /// <summary>
    /// Obtains the <c>OnlinePhysicalObject</c>-equivalent instance of the given <c>PhysicalObject</c>.
    /// </summary>
    /// <param name="self">The <c>PhysicalObject</c> to be queried.</param>
    /// <returns>The <c>OnlinePhysicalObject</c> which represents the given input, or <c>null</c> if none is found.</returns>
    public static OnlinePhysicalObject GetOnlinePhysicalObject(PhysicalObject self) => (OnlinePhysicalObject)_cachedOnlineObjects.GetValue(self, QueryOnlinePhysicalEntity);

    /// <summary>
    /// Queries the world's active entities in order to find a given <c>PhysicalObject</c> instance.
    /// </summary>
    /// <param name="physicalObject">The <c>PhysicalObject</c> to be queried.</param>
    /// <returns>The <c>OnlinePhysicalObject</c> which represents the given input, or <c>null</c> if none is found.</returns>
    private static OnlinePhysicalObject QueryOnlinePhysicalEntity(PhysicalObject physicalObject)
    {
        WorldSession worldSession = QueryWorldSession(physicalObject);

        if (worldSession is null)
        {
            ASLogger.LogWarning($"Could not find WorldSession of {physicalObject}, aborting operation.");
        }
        else
        {
            foreach (OnlineEntity entity in worldSession.activeEntities)
            {
                if (entity is not OnlinePhysicalObject onlineObject || onlineObject.apo.realizedObject != physicalObject) continue;

                return onlineObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Obtains the <c>WorldSession</c> instance where the given <c>PhysicalObject</c> is found at.
    /// </summary>
    /// <param name="physicalObject">The physical object to be queried.</param>
    /// <returns>The object's <c>WorldSession</c> instance, or <c>null</c> if none is found.</returns>
    private static WorldSession QueryWorldSession(PhysicalObject physicalObject)
    {
        foreach (WorldSession session in OnlineManager.lobby.worldSessions.Values)
        {
            if (session.world == physicalObject.room.world)
            {
                return session;
            }
        }

        ASLogger.LogWarning($"Failed to obtain the WorldSession instance of {physicalObject}!");

        return null;
    }

    /// <summary>
    /// Holds weak references to previously queried <c>OnlinePlayer</c> names. If a query is made for a key which is present here, its value is returned instead.
    /// </summary>
    private static readonly ConditionalWeakTable<Player, string> _cachedPlayerNames = new();

    /// <summary>
    /// Obtains the online name of the given player.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>"Invalid Player"</c> if none is found.</returns>
    public static string GetPlayerName(Player self) => _cachedPlayerNames.GetValue(self, QueryPlayerName);

    /// <summary>
    /// Queries the lobby's online players in order to find the name of a given player.
    /// </summary>
    /// <param name="physicalObject">The player to be queried.</param>
    /// <returns>A <c>String</c> containing the player's name, or <c>"Invalid Player"</c> if none is found.</returns>
    private static string QueryPlayerName(Player player)
    {
        OnlinePhysicalObject playerOPO = GetOnlinePhysicalObject(player);

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (playerOPO.owner == onlinePlayer) return onlinePlayer.id.GetPersonaName();
        }

        ASLogger.LogWarning($"Failed to retrieve the OnlinePlayer instance of {player}!");

        return "Invalid Player";
    }
}