using System;
using System.Collections.Generic;
using ModLib.Generics;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
/// A simple tracker of sent RPC events, used to prevent unresolved SoftRPCs from hanging around indefinitely.
/// </summary>
public static class ModRPCManager
{
    private static readonly WeakDictionary<RPCEvent, RPCTimeout> _activeRPCs = [];

    public static void RemoveTimeout(this RPCEvent self)
    {
        if (!TryGetTimeout(self, out _)) return;

        _activeRPCs.Remove(self);
    }

    public static RPCEvent SetTimeout(this RPCEvent self, int lifetime)
    {
        if (TryGetTimeout(self, out RPCTimeout timeout))
        {
            timeout.Lifetime = lifetime;
        }
        else
        {
            _activeRPCs[self] = new(lifetime);
        }

        return self;
    }

    public static bool TryGetTimeout(this RPCEvent self, out RPCTimeout timeout) =>
        _activeRPCs.TryGetValue(self, out timeout);

    public static void UpdateRPCs()
    {
        if (_activeRPCs.Count < 1) return;

        foreach (KeyValuePair<RPCEvent, RPCTimeout> managedRPC in _activeRPCs)
        {
            managedRPC.Value.Lifetime--;

            if (managedRPC.Value.Lifetime < 1)
            {
                Logger.LogWarning($"RPC event {managedRPC.Key} failed to be delivered; Timed out waiting for response.");

                managedRPC.Key.Abort();

                _activeRPCs.Remove(managedRPC.Key);
            }
        }
    }

    public static RPCEvent SendRPCEvent<T>(this OnlinePlayer onlinePlayer, T @delegate, params object[] args)
        where T : Delegate
    {
        Type rpcSource = @delegate.Method.DeclaringType;

        RPCEvent rpcEvent = onlinePlayer
            .InvokeOnceRPC(rpcSource.GetMethod(@delegate.Method.Name).CreateDelegate(typeof(T)), args)
            .Then(ResolveRPCEvent)
            .SetTimeout(1000);

        Logger.LogDebug($"Sending RPC event {rpcEvent} to {rpcEvent.to}...");

        return rpcEvent;
    }

    public static void BroadcastOnceRPCInRoom(this OnlineEntity source, Delegate del, params object[] args)
    {
        if (source.currentlyJoinedResource is not RoomSession roomSession) return;

        foreach (OnlinePlayer participant in roomSession.participants)
        {
            if (participant.isMe) continue;

            participant.SendRPCEvent(del, args);
        }
    }

    public static void ResolveRPCEvent(GenericResult result)
    {
        switch (result)
        {
            case GenericResult.Ok:
                Logger.LogInfo($"Successfully delivered RPC {result.referencedEvent} to {result.from}.");
                break;
            case GenericResult.Fail:
                Logger.LogWarning($"Could not run RPC {result.referencedEvent} as {result.from}.");
                break;
            default:
                Logger.LogWarning($"Failed to deliver RPC {result.referencedEvent} to {result.from}!");
                break;
        }

        if (result.referencedEvent is RPCEvent rpcEvent)
        {
            rpcEvent.RemoveTimeout();
        }
    }

    public sealed record RPCTimeout(int Lifetime)
    {
        public int Lifetime { get; set; } = Lifetime;
    }
}