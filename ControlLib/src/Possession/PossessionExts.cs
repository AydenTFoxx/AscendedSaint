using System.Runtime.CompilerServices;
using ControlLib.Utils;

namespace ControlLib.Possession;

public static class PossessionExts
{
    private static readonly ConditionalWeakTable<Creature, Player> _cachedPossessions = new();
    private static readonly WeakDictionary<Player, PossessionManager> _possessionHolders = [];

    public static PossessionManager GetPossessionManager(this Player self)
    {
        if (TryGetPossessionManager(self, out PossessionManager manager)) return manager;

        PossessionManager newManager = new(self);

        _possessionHolders.Add(self, newManager);
        return newManager;
    }

    public static Player GetPossession(this Creature self) => TryGetPossession(self, out Player possession) ? possession : null;

    public static bool TryGetPossession(this Creature self, out Player possession)
    {
        if (_cachedPossessions.TryGetValue(self, out Player player))
        {
            possession = player;
            return true;
        }

        possession = null;
        foreach (PossessionManager manager in _possessionHolders.Values)
        {
            if (manager.MyPossessions.TryGetValue(self, out possession))
            {
                _cachedPossessions.Add(self, possession);
                return true;
            }
        }

        return false;
    }

    public static bool TryGetPossessionManager(this Player self, out PossessionManager manager) => _possessionHolders.TryGetValue(self, out manager);

    public static void UpdateCachedPossession(this Creature self)
    {
        if (_cachedPossessions.TryGetValue(self, out Player possession)
            && !possession.GetPossessionManager().MyPossessions.ContainsKey(self))
        {
            _cachedPossessions.Remove(self);
        }
    }
}