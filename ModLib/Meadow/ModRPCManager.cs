using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     A simple tracker of sent RPC events, used to prevent unresolved SoftRPCs from hanging around indefinitely.
/// </summary>
public static class ModRPCManager
{
    private static List<RPCTimeout> _activeRPCs = [];

    private static void RemoveTimeout(this RPCEvent self)
    {
        if (!TryGetTimeout(self, out RPCTimeout timeout)) return;

        _activeRPCs.Remove(timeout);
    }

    private static RPCEvent SetTimeout(this RPCEvent self, int lifetime)
    {
        if (TryGetTimeout(self, out RPCTimeout timeout))
        {
            timeout.Lifetime = lifetime;
        }
        else
        {
            _activeRPCs.Add(new RPCTimeout(self, lifetime));
        }

        return self;
    }

    private static bool TryGetTimeout(this RPCEvent self, out RPCTimeout timeout)
    {
        timeout = _activeRPCs.Find(t => t.Source == self);

        return timeout is not null;
    }

    /// <summary>
    ///     Updates all pending <see cref="RPCEvent"/> instances, removing them on expiration.
    /// </summary>
    public static void UpdateRPCs()
    {
        if (_activeRPCs.Count < 1) return;

        bool expired = false;
        foreach (RPCTimeout rpcTimeout in _activeRPCs)
        {
            rpcTimeout.Update();

            expired = rpcTimeout.Lifetime < 1;
        }

        if (expired)
        {
            _activeRPCs = [.. _activeRPCs.Where(t => t.Lifetime > 0)];
        }
    }

    /// <summary>
    ///     Sends an RPC event to the online player, which is automatically aborted if the recipient does not answer after a certain time limit.
    /// </summary>
    /// <typeparam name="T">The type of the RPC delegate to be sent.</typeparam>
    /// <param name="onlinePlayer">The recipient who will receive this RPC event.</param>
    /// <param name="delegate">The RPC method to be sent.</param>
    /// <param name="args">Any arguments of the RPC method.</param>
    /// <returns>The <see cref="RPCEvent"/> instance sent to the online player.</returns>
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

    /// <summary>
    ///     Sends a single RPC event to all players in the same room as the online entity.
    /// </summary>
    /// <param name="source">The online entity who will send the RPC events.</param>
    /// <param name="del">The RPC method to be sent.</param>
    /// <param name="args">Any arguments of the RPC method.</param>
    public static void BroadcastOnceRPCInRoom(this OnlineEntity source, Delegate del, params object[] args)
    {
        if (source.currentlyJoinedResource is not RoomSession roomSession) return;

        foreach (OnlinePlayer participant in roomSession.participants)
        {
            if (participant.isMe) continue;

            participant.SendRPCEvent(del, args);
        }
    }

    /// <summary>
    ///     Logs the result of the resolved RPC event, then removes its timeout irrespective of its result.
    /// </summary>
    /// <param name="result">The result of the resolved RPC event.</param>
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

    /// <summary>
    ///     A self-contained timer which automatically aborts a given RPC once its internal timer runs out.
    /// </summary>
    /// <param name="Source">The RPC event this timeout is tied to.</param>
    /// <param name="Lifetime">The duration of the internal timer.</param>
    private sealed record RPCTimeout(RPCEvent Source, int Lifetime)
    {
        public RPCEvent Source { get; } = Source;

        public int Lifetime { get; set; } = Lifetime;
        public bool Expired { get; private set; }

        public void Update()
        {
            if (Expired) return;

            if (Source.aborted)
            {
                Expired = true;
                return;
            }

            Lifetime--;

            if (Lifetime < 1)
            {
                Logger.LogWarning($"RPC event {Source} failed to be delivered; Timed out waiting for response.");

                Source.Abort();

                Expired = true;
            }
        }
    }
}