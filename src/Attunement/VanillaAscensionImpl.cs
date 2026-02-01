using AscendedSaint.Utils;

namespace AscendedSaint.Attunement;

public class VanillaAscensionImpl : IAscensionImpl
{
    public bool TryAscendCreature(Creature target, Player caller)
    {
        bool result = false;

        if (target.dead)
        {
            result = RevivalHelper.ReviveCreature(target);

            if (result)
            {
                SpawnAscensionEffects(target, true);

                RemoveFromRespawnsList(target);
            }
        }
        else if (target == caller)
        {
            target.Die();

            result = target.dead;

            SpawnAscensionEffects(target, false);
        }

        return result;
    }

    public bool TryAscendOracle(Oracle target, Player caller)
    {
        bool result = RevivalHelper.ReviveOracle(target);

        if (result)
            SpawnAscensionEffects(target, true);

        return result;
    }

    public void SpawnAscensionEffects(PhysicalObject target, bool isRevival) =>
        AscensionHandler.SpawnAscensionEffects(target, isRevival);

    public void RemoveFromRespawnsList(Creature creature) =>
        RevivalHelper.RemoveFromRespawnsList(creature);
}