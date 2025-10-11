using System;
using AscendedSaint.Attunement;
using AscendedSaint.Features;
using ModLib.Meadow;
using RainMeadow;

namespace AscendedSaint.Meadow;

public class MeadowAscensionImpl : IAscensionImpl
{
    public bool TryAscendCreature(Creature target, Player caller)
    {
        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null && MeadowUtils.IsOnline)
        {
            ModLib.Logger.LogWarning("Cannot ascend or revive a null object! Aborting operation.");
            return false;
        }

        if (target.dead)
        {
            if (target.IsLocal())
            {
                RevivalFeature.ReviveCreature(target);
            }
            else
            {
                MeadowUtils.RequestOwnership(onlineObject!, BuildRevivalCallback<Creature>(onlineObject!, RevivalFeature.ReviveCreature));
            }

            SpawnAscensionEffects(target, true);

            RemoveFromRespawnsList(target);

            MeadowUtils.LogSystemMessage($"{GetCreatureName(target)} was revived by {MeadowUtils.GetOnlineName(caller)}.");
        }
        else if (target == caller)
        {
            target.Die();

            SpawnAscensionEffects(target, false);

            MeadowUtils.LogSystemMessage($"{MeadowUtils.GetOnlineName(caller)} self-ascended.");
        }
        else return false;

        return true;
    }

    public bool TryAscendOracle(Oracle target, Player caller)
    {
        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null && MeadowUtils.IsOnline)
        {
            ModLib.Logger.LogWarning("Cannot ascend or revive a null object! Aborting operation.");
            return false;
        }

        if (target.IsLocal())
        {
            RevivalFeature.ReviveOracle(target);
        }
        else
        {
            MeadowUtils.RequestOwnership(onlineObject!, BuildRevivalCallback<Oracle>(onlineObject!, RevivalFeature.ReviveOracle));
        }

        SpawnAscensionEffects(target, true);

        MeadowUtils.LogSystemMessage($"{RevivalFeature.GetOracleName(target.ID)} was revived by {MeadowUtils.GetOnlineName(caller)}.");

        return true;
    }

    public void SpawnAscensionEffects(PhysicalObject target, bool isRevival)
    {
        OnlinePhysicalObject? onlineObject = target.abstractPhysicalObject.GetOnlineObject();

        if (onlineObject is null && MeadowUtils.IsOnline) return;

        onlineObject?.BroadcastOnceRPCInRoom(MyRPCs.SyncAscensionEffects, onlineObject);

        AscensionHandler.SpawnAscensionEffects(target, isRevival);
    }

    public void RemoveFromRespawnsList(Creature creature)
    {
        if (creature is Player) return;

        if (MeadowUtils.IsHost)
        {
            RevivalFeature.RemoveFromRespawnsList(creature);
        }
        else
        {
            OnlineManager.lobby.owner.SendRPCEvent(MyRPCs.SyncRemoveFromRespawnsList, creature.abstractCreature.GetOnlineCreature()!);
        }
    }

    /// <summary>
    ///     Retrieves the name to be used for identifying an ascended or revived creature.
    /// </summary>
    /// <remarks>
    ///     If the provided creature is an online player, their persona name is returned instead.
    /// </remarks>
    /// <param name="creature">The creature itself.</param>
    /// <returns>A string with this creature's name.</returns>
    private static string GetCreatureName(Creature creature) =>
        MeadowUtils.IsOnline && creature is Player player && player.AI is null
            ? MeadowUtils.GetOnlineName(player) ?? "Unknown Player"
            : creature.Template.name;

    /// <summary>
    ///     Encapsulates the provided arguments into a delegate to be executed after requesting for an online object's ownership.
    /// </summary>
    /// <remarks>
    ///     If the request is successful, the revival method is invoked;
    ///     If it fails, an RPC is sent to the owner to revive the creature themselves.
    ///     In case of an error, the operation is aborted.
    /// </remarks>
    /// <typeparam name="T">The type of the physical object to be revived.</typeparam>
    /// <param name="onlineObject">The online representation of this physical object.</param>
    /// <param name="revivalMethod">The revival method to be called to revive this object.</param>
    /// <returns>A callback delegate to be executed upon requesting an online object's ownership, whether successful or not.</returns>
    private static Action<GenericResult> BuildRevivalCallback<T>(OnlinePhysicalObject onlineObject, Func<T, bool> revivalMethod)
        where T : PhysicalObject
    {
        return (result) =>
        {
            T target = (T)onlineObject.apo.realizedObject;

            switch (result)
            {
                case GenericResult.Ok:
                    ModLib.Logger.LogDebug($"Received ownership of {target}? {onlineObject.isMine}; Running revival method...");

                    revivalMethod.Invoke(target);
                    break;
                case GenericResult.Fail:
                    ModLib.Logger.LogInfo($"Could not request the ownership of {target}; Requesting owner to run revival RPC...");

                    onlineObject.owner.SendRPCEvent(MyRPCs.SyncObjectRevival, onlineObject);
                    break;
                case GenericResult.Error:
                default:
                    ModLib.Logger.LogWarning($"Could not revive creature {target}! (Result: {result})");
                    break;
            }
        };
    }
}