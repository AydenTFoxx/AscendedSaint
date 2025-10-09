using AscendedSaint.Features;
using ModLib.Options;

namespace AscendedSaint.Attunement;

public class VanillaAscensionImpl : IAscensionImpl
{
    public void AscendCreature(Creature target, Player caller)
    {
        if (target.dead)
        {
            RevivalFeature.ReviveCreature(target, OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR));

            SpawnAscensionEffects(target, true);
        }
        else if (target == caller)
        {
            target.Die();

            SpawnAscensionEffects(target, false);
        }
        else
        {
            ModLib.Logger.LogWarning($"Could not ascend or revive creature: {target}");
        }
    }

    public void AscendOracle(Oracle target, Player caller)
    {
        if (!RevivalFeature.CanReviveOracle(target))
        {
            ModLib.Logger.LogWarning($"Cannot revive oracle {RevivalFeature.GetOracleName(target.ID)}!");
            return;
        }

        RevivalFeature.ReviveOracle(target);

        SpawnAscensionEffects(target, true);
    }

    public void SpawnAscensionEffects(PhysicalObject target, bool isRevival) =>
        AscensionHandler.SpawnAscensionEffects(target, isRevival);
}