using System.Runtime.CompilerServices;
using ControlLib.Utils;

namespace ControlLib.Possession;

/// <summary>
/// Extension methods for retrieving the possession holders of a given creature.
/// </summary>
public static class PossessionExts
{
    /// <summary>
    /// Stores the result of previous queries for a given creature and its possessing player.
    /// </summary>
    /// <remarks>References are valid for as long as the possession lasts; Once possession ends, the given creature's key-value pair is discarded.</remarks>
    private static readonly ConditionalWeakTable<Creature, Player> _cachedPossessions = new();
    /// <summary>
    /// Stores all players with a <c>PossessionManager</c> instance.
    /// </summary>
    /// <remarks>This is used as a reference to determine which player is currently possessing a given creature.</remarks>
    private static readonly WeakDictionary<Player, PossessionManager> _possessionHolders = [];

    /// <summary>
    /// Obtains the given creature's possessing player, if any.
    /// </summary>
    /// <param name="self">The creature to be queried.</param>
    /// <returns>The <c>Player</c> who is currently possessing this creature, or <c>null</c> if none is found.</returns>
    /// <remarks>Unlike <see cref="GetPossessionManager(Player)"/>, this does not create a new instance should none be found.</remarks>
    public static Player GetPossession(this Creature self) => TryGetPossession(self, out Player possession) ? possession : null;

    /// <summary>
    /// Obtains the given player's <c>PossessionManager</c> instance. If none is found, a new one is created with default values.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <returns>The existing <c>PossessionManager</c> instance, or a new one if none was found.</returns>
    public static PossessionManager GetPossessionManager(this Player self)
    {
        if (TryGetPossessionManager(self, out PossessionManager manager)) return manager;

        PossessionManager newManager = new(self);

        _possessionHolders.Add(self, newManager);
        return newManager;
    }

    /// <summary>
    /// Attempts to retrieve the given creature's possessing player. If none is found, <c>null</c> is returned instead.
    /// </summary>
    /// <param name="self">The creature to be queried.</param>
    /// <param name="possession">The output value; May be a <c>Player</c> instance or <c>null</c>.</param>
    /// <returns><c>true</c> if a value was found, <c>false</c> otherwise.</returns>
    public static bool TryGetPossession(this Creature self, out Player possession)
    {
        possession = null;

        if (_cachedPossessions.TryGetValue(self, out Player player))
        {
            possession = player;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the given player's <c>PossessionManager</c> instance. If none is found, <c>null</c> is returned instead.
    /// </summary>
    /// <param name="self">The player to be queried.</param>
    /// <param name="manager">The output value; May be a <c>PossessionManager</c> instance or <c>null</c>.</param>
    /// <returns><c>true</c> if a value was found, <c>false</c> otherwise.</returns>
    public static bool TryGetPossessionManager(this Player self, out PossessionManager manager) => _possessionHolders.TryGetValue(self, out manager);

    /// <summary>
    /// Adds or removes the given creature's cached possession pair, depending on whether the possession is still valid.
    /// </summary>
    /// <param name="self">The creature to be checked.</param>
    /// <remarks>This should always be called upon updating a creature's possession state.</remarks>
    public static void UpdateCachedPossession(this Creature self)
    {
        if (_cachedPossessions.TryGetValue(self, out Player possession))
        {
            if (!possession.GetPossessionManager().HasPossession(self))
            {
                _cachedPossessions.Remove(self);
            }
        }
        else
        {
            foreach (PossessionManager manager in _possessionHolders.Values)
            {
                if (manager.HasPossession(self))
                {
                    _cachedPossessions.Add(self, manager.GetPlayer());
                }
            }
        }
    }
}