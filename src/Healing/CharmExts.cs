using System.Collections.Generic;
using System.Linq;

namespace AscendedSaint.Healing;

public static class CharmExts
{
    public static bool CanCharm(Creature creature) => creature is not (null or Overseer) and { abstractCreature.abstractAI: not null, Consious: true, safariControlled: false } && GetCharms(creature.abstractCreature).Count == 0;

    public static bool CharmCreature(this AbstractCreature self, AbstractCreature target, int duration)
    {
        if (!CanCharm(target.realizedCreature)) return false;

        if (Charm.Instances.ContainsKey(self))
        {
            Charm.Instances[target].Add(new Charm(self, target, duration));
        }
        else
        {
            Charm.Instances.Add(target, [new Charm(self, target, duration)]);
        }

        return true;
    }

    public static List<Charm> GetCharms(this AbstractCreature self) => Charm.Instances.TryGetValue(self, out List<Charm> charms) ? charms : [];

    public static bool IsCharmedBy(this AbstractCreature self, AbstractCreature source) => self.GetCharms().Any(c => !c.slatedForDeletetion && c.Source == source);

    public static void StopCharm(this AbstractCreature self, AbstractCreature source) => self.GetCharms().RemoveAll(c => c.Source == source);

    public static void StopCharm(this AbstractCreature self, Charm charm) => self.GetCharms().Remove(charm);

    public static void RemoveAllCharms(this AbstractCreature self) => Charm.Instances.Remove(self);
}