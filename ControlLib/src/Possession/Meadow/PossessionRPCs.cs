using System;
using RainMeadow;
using Random = UnityEngine.Random;
using static ControlLib.ControlLibMain;

namespace ControlLib.Possession.Meadow;

public static class PossessionRPCs
{
    [SoftRPCMethod]
    public static void ApplyPossessionEffects(RPCEvent rpcEvent, OnlineCreature onlineTarget, bool isPossession)
    {
        if (onlineTarget.realizedCreature is not Creature target || target.room is null)
        {
            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        if (isPossession)
        {
            target.room.AddObject(new TemplarCircle(target, target.mainBodyChunk.pos, 48f, 8f, 2f, 12, true));
            target.room.AddObject(new ShockWave(target.mainBodyChunk.pos, 100f, 0.08f, 4, false));
            target.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, target.mainBodyChunk, loop: false, 1f, 1.25f + (Random.value * 1.25f));
        }
        else
        {
            target.room.AddObject(new ReverseShockwave(target.mainBodyChunk.pos, 64f, 0.05f, 24));
            target.room.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);
        }
    }

    [SoftRPCMethod]
    public static void RequestRemixOptionsSync(RPCEvent rpcEvent, OnlinePlayer onlinePlayer)
    {
        if (!MeadowUtils.IsHost || ClientOptions is null)
        {
            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        CLLogger.LogInfo($"Syncing REMIX options with player {onlinePlayer.id}...");

        onlinePlayer.SendRPCEvent(SyncRemixOptions, new SharedOptions(ClientOptions));
    }

    [SoftRPCMethod]
    public static void SetCreatureControl(RPCEvent rpcEvent, OnlineCreature onlineTarget, bool controlled)
    {
        if (onlineTarget.realizedCreature is not Creature target)
        {
            CLLogger.LogWarning($"{onlineTarget} is not a controllable creature.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        target.abstractCreature.controlled = controlled;

        CLLogger.LogInfo($"{target} is {(controlled ? "now" : "no longer")} being controlled by {rpcEvent.from}.");
    }

    [SoftRPCMethod]
    public static void SyncRemixOptions(RPCEvent rpcEvent, SharedOptions options)
    {
        if (MeadowUtils.IsHost)
        {
            CLLogger.LogWarning("Player is host; Ignoring options sync.");

            rpcEvent.Resolve(new GenericResult.Fail(rpcEvent));
            return;
        }

        ClientOptions?.SetSyncedOptions(options);

        CLLogger.LogInfo($"Synced REMIX options! New values are: {ClientOptions}");
    }

    public static void SendCreatureRPC<T>(Creature creature, T @delegate, params object[] args)
        where T : Delegate
    {
        OnlineCreature? onlineCreature = creature.abstractCreature.GetOnlineCreature();

        if (onlineCreature is null) return;

        foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
        {
            if (onlinePlayer.isMe) continue;

            RPCEvent rpc = onlinePlayer.SendRPCEvent(@delegate, onlineCreature, args);

            CLLogger.LogDebug($"Creature RPC is: {rpc}");
        }
    }

    internal static RPCEvent SendRPCEvent<T>(this OnlinePlayer onlinePlayer, T @delegate, params object[] args)
        where T : Delegate
    {
        RPCEvent rpcEvent = onlinePlayer
            .InvokeOnceRPC(typeof(PossessionRPCs).GetMethod(@delegate.Method.Name).CreateDelegate(typeof(T)), args)
            .Then(ResolveRPCEvent)
            .SetTimeout(1000);

        CLLogger.LogDebug($"Sending RPC event {rpcEvent} to {rpcEvent.to}...");

        return rpcEvent;
    }

    private static void ResolveRPCEvent(GenericResult result)
    {
        switch (result)
        {
            case GenericResult.Ok:
                CLLogger.LogInfo($"Successfully delivered RPC {result.referencedEvent} to {result.to}.");
                break;
            case GenericResult.Fail:
                CLLogger.LogWarning($"Could not run RPC {result.referencedEvent} as {result.to}.");
                break;
            default:
                CLLogger.LogWarning($"Failed to deliver RPC {result.referencedEvent} to {result.to}!");
                break;
        }

        if (result.referencedEvent is RPCEvent rpcEvent)
        {
            rpcEvent.RemoveTimeout();
        }
    }

    public record class SharedOptions : CLOptions.ClientOptions, Serializer.ICustomSerializable
    {
        public SharedOptions(CLOptions.ClientOptions options)
        {
            SetSyncedOptions(options);
        }

        public SharedOptions()
            : base()
        {
        }

        public void CustomSerialize(Serializer serializer)
        {
            if (ClientOptions is null)
            {
                throw new InvalidProgrammerException("ClientOptions was not initialized");
            }

            serializer.Serialize(ref ClientOptions.selectionMode);
            serializer.Serialize(ref ClientOptions.invertControls);
            serializer.Serialize(ref ClientOptions.meadowSlowdown);
            serializer.Serialize(ref ClientOptions.infinitePossession);
            serializer.Serialize(ref ClientOptions.possessAncestors);
            serializer.Serialize(ref ClientOptions.forceMultitargetPossession);
            serializer.Serialize(ref ClientOptions.worldwideMindControl);
        }

        public override string ToString() => $"{nameof(SharedOptions)} => {FormatOptions()}";
    }
}