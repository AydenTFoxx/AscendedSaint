using AscendedSaint.Attunement;
using AscendedSaint.Features;
using ModLib.Meadow;
using ModLib.Options;
using RainMeadow;

namespace AscendedSaint.Meadow;

public class MeadowAscensionImpl : IAscensionImpl
{
    public void AscendCreature(Creature target, Player caller)
    {
        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null)
        {
            ModLib.Logger.LogWarning("Cannot ascend or revive a null object! Aborting operation.");
            return;
        }

        if (target.dead)
        {
            if (target.IsLocal())
            {
                RevivalFeature.ReviveCreature(target, OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR));
            }
            else
            {
                MeadowUtils.RequestOwnership(target.abstractPhysicalObject.GetOnlineObject(), RevivalCallback);
            }

            SpawnAscensionEffects(target, true);

            MeadowUtils.LogSystemMessage($"{GetCreatureName(target)} was revived by {MeadowUtils.GetOnlineName(caller)}.");
        }
        else if (target == caller)
        {
            target.Die();

            SpawnAscensionEffects(target, false);

            MeadowUtils.LogSystemMessage($"{MeadowUtils.GetOnlineName(caller)} self-ascended.");
        }
        else
        {
            ModLib.Logger.LogWarning($"Could not ascend or revive creature: {target}");
        }

        void RevivalCallback(GenericResult result)
        {
            if (result is not GenericResult.Ok)
            {
                ModLib.Logger.LogWarning($"Could not revive creature {target}! (Result: {result})");
                return;
            }

            RevivalFeature.ReviveCreature(target, OptionUtils.GetOptionValue(Options.REVIVAL_HEALTH_FACTOR));
        }
    }

    public void AscendOracle(Oracle target, Player caller)
    {
        if (!RevivalFeature.CanReviveOracle(target))
        {
            ModLib.Logger.LogWarning($"Cannot revive oracle {RevivalFeature.GetOracleName(target.ID)}!");
            return;
        }

        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null)
        {
            ModLib.Logger.LogWarning("Cannot ascend or revive a null object! Aborting operation.");
            return;
        }

        if (target.IsLocal())
        {
            RevivalFeature.ReviveOracle(target);
        }
        else
        {
            MeadowUtils.RequestOwnership(onlineObject, RevivalCallback);
        }

        SpawnAscensionEffects(target, true);

        MeadowUtils.LogSystemMessage($"{RevivalFeature.GetOracleName(target.ID)} was revived by {MeadowUtils.GetOnlineName(caller)}.");

        void RevivalCallback(GenericResult result)
        {
            if (result is not GenericResult.Ok)
            {
                ModLib.Logger.LogWarning($"Could not revive oracle {target}! (Result: {result})");
                return;
            }

            RevivalFeature.ReviveOracle(target);
        }
    }

    public void SpawnAscensionEffects(PhysicalObject target, bool isRevival)
    {
        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null) return;

        onlineObject.BroadcastOnceRPCInRoom(MyRPCs.SyncAscensionEffects, onlineObject);

        AscensionHandler.SpawnAscensionEffects(target, isRevival);
    }

    private static string GetCreatureName(Creature creature) =>
        creature is Player player
            ? MeadowUtils.GetOnlineName(player) ?? "Unknown Player"
            : creature.Template.name;
}