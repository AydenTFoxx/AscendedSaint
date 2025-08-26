using RainMeadow;
using ControlLib.Possession.Meadow;

namespace ControlLib.Possession;

public static class MeadowUtils
{
    public static bool IsOnline => OnlineManager.lobby is not null;
    public static bool IsHost => !IsOnline || OnlineManager.lobby.isOwner;

    public static bool IsGameMode(MeadowGameModes gameMode)
    {
        if (!IsOnline) return false;

        int gamemode = OnlineManager.lobby.gameMode switch
        {
            MeadowGameMode => 0,
            StoryGameMode => 1,
            ArenaOnlineGameMode => 2,
            _ => -1
        };

        return gamemode == (int)gameMode;
    }

    public static bool IsMine(PhysicalObject physicalObject) => physicalObject.IsLocal();

    public static void RequestOptionsSync()
    {
        if (!IsOnline || ControlLibMain.ClientOptions is null) return;

        OnlineManager.lobby.owner.SendRPCEvent(PossessionRPCs.RequestRemixOptionsSync, OnlineManager.mePlayer);
    }

    public static void RequestOwnership(PhysicalObject physicalObject)
    {
        try
        {
            CLLogger.LogDebug($"Requesting ownership of {physicalObject}...");

            physicalObject.abstractPhysicalObject.GetOnlineObject()?.Request();

            CLLogger.LogDebug($"New owner is: {physicalObject.abstractPhysicalObject.GetOnlineObject()?.owner}");
        }
        catch (System.Exception ex)
        {
            CLLogger.LogError($"Failed to request ownership of {physicalObject}!", ex);
        }
    }

    public static void SyncCreaturePossession(Creature target, bool isPossession)
    {
        if (!IsOnline) return;

        CLLogger.LogDebug($"Syncing possession of {target} with all players.");

        PossessionRPCs.SendCreatureRPC(target, PossessionRPCs.ApplyPossessionEffects, isPossession);
        PossessionRPCs.SendCreatureRPC(target, PossessionRPCs.SetCreatureControl, isPossession);
    }

    public enum MeadowGameModes
    {
        Meadow,
        Story,
        Arena,
        Custom = -1
    }
}