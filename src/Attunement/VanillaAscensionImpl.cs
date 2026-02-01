using AscendedSaint.Features;

namespace AscendedSaint.Attunement;

public class VanillaAscensionImpl : IAscensionImpl
{
    public bool TryAscendCreature(Creature target, Player caller)
    {
        bool result = true;

        if (target.dead)
        {
            result = RevivalFeature.ReviveCreature(target);

            if (result)
            {
                SpawnAscensionEffects(target, true);

                RemoveFromRespawnsList(target);
            }
        }
        else if (target == caller)
        {
            target.Die();

            SpawnAscensionEffects(target, false);
        }
        else
        {
            result = false;
        }

        return result;
    }

    public bool TryAscendOracle(Oracle target, Player caller)
    {
        bool result = RevivalFeature.ReviveOracle(target);

        if (result)
            SpawnAscensionEffects(target, true);

        return result;
    }

    public void SpawnAscensionEffects(PhysicalObject target, bool isRevival) =>
        AscensionHandler.SpawnAscensionEffects(target, isRevival);

    public void RemoveFromRespawnsList(Creature creature) =>
        RevivalFeature.RemoveFromRespawnsList(creature);
}