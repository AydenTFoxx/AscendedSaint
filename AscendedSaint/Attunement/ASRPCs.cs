using System;
using RainMeadow;
using static AscendedSaint.AscendedSaintMain;
using static AscendedSaint.Attunement.ASUtils;

namespace AscendedSaint.Attunement;

/// <summary>
/// Custom events sent to or received in order to properly sync settings and the mod's behavior.
/// </summary>
public class ASRPCs
{
    /// <summary>
    /// Sets the client's settings to those received from the host.
    /// </summary>
    /// <param name="options">The options object sent by the host player.</param>
    /// <remarks>This event is sent from the host player to the client upon joining a lobby.</remarks>
    [RPCMethod]
    public static void SyncRemixSettings(SharedOptions options)
    {
        if (OnlineManager.lobby.isOwner) return;

        ClientOptions.allowRevival = options.allowRevival;
        ClientOptions.allowSelfAscension = options.allowSelfAscension;
        ClientOptions.requireKarmaFlower = options.requireKarmaFlower;
        ClientOptions.revivalHealthFactor = options.revivalHealthFactor;

        ASLogger.LogInfo("Synced REMIX settings with client!");
        ASLogger.LogDebug($"Received settings are: {options}");
    }

    /// <summary>
    /// Requests the host player to sync its settings with the client.
    /// </summary>
    /// <param name="callingPlayer">The player who requested the sync.</param>
    /// <remarks>This event is sent from the client player to the host upon joining a lobby.</remarks>
    [RPCMethod]
    public static void RequestRemixSync(OnlinePlayer callingPlayer)
    {
        if (!OnlineManager.lobby.isOwner) return;

        ASLogger.LogInfo($"Received request for REMIX settings sync! Sending data to player {callingPlayer.inLobbyId}...");

        callingPlayer.InvokeOnceRPC(typeof(ASRPCs).GetMethod("SyncRemixSettings").CreateDelegate(typeof(Action<SharedOptions>)), new SharedOptions(ClientOptions));
    }

    /// <summary>
    /// Syncs the ascension effects of a given creature to the player.
    /// </summary>
    /// <param name="onlineObject">The creature who was ascended or revived.</param>
    [RPCMethod]
    public static void SyncAscensionEffects(OnlinePhysicalObject onlineObject)
    {
        if (onlineObject is null || onlineObject.apo.realizedObject is not Creature or Oracle)
        {
            ASLogger.LogWarning("Got a request to sync the ascension of an invalid creature!");
            return;
        }

        SpawnAscensionEffects(onlineObject.apo.realizedObject, isRevival: onlineObject.apo.realizedObject is Creature creature ? !creature.dead : onlineObject.apo.realizedObject is Oracle { health: > 0f });
    }

    /// <summary>
    /// Updates a revived creature from its owner to all subscribed players.
    /// </summary>
    /// <param name="revivedCreature">The creature who was revived.</param>
    /// <remarks>While creature revival is usually synced on its own, iterators and especially players need special handling for proper sync.</remarks>
    [RPCMethod]
    public static void SyncCreatureRevival(OnlinePhysicalObject revivedCreature)
    {
        PhysicalObject physicalObject = revivedCreature.apo.realizedObject;

        if (physicalObject is Creature creature)
        {
            ASLogger.LogInfo($"{creature.Template.name} was revived!");

            ReviveCreature(creature, ClientOptions.revivalHealthFactor);
        }
        else if (physicalObject is Oracle oracle)
        {
            ASLogger.LogInfo($"{Utils.GetOracleName(oracle.ID)} was revived!");

            ReviveOracle(oracle);
        }
        else
        {
            ASLogger.LogWarning($"Expected creature or iterator revived, got: {physicalObject}");
        }
    }

    /// <summary>
    /// Removes a creature from the world's respawn list. A variant of <c>RemoveFromRespawnsList</c> which can be sent as a RPC event.
    /// </summary>
    /// <param name="onlineCreature">The creature to be removed.</param>
    /// <seealso cref="RemoveFromRespawnsList(Creature)"/>
    [RPCMethod]
    public static void SyncRemoveFromRespawnsList(OnlineCreature creature)
    {
        CreatureState state = creature.abstractCreature.state;
        EntityID ID = creature.abstractCreature.ID;

        GameSession gameSession = creature.realizedCreature.room.game.session;

        if (state.alive && ID.spawner >= 0 && gameSession is StoryGameSession storySession)
        {
            storySession.saveState.respawnCreatures.Remove(ID.spawner);
        }

        ASLogger.LogInfo($"{creature} has been removed from the respawns list!");
    }
}


/// <summary>
/// A serializable variant of <c>ClientOptions</c>, used for sending and receiving player settings.
/// </summary>
public class SharedOptions : ASOptions.ClientOptions, Serializer.ICustomSerializable
{
    public SharedOptions()
    {
        RefreshOptions();
    }

    public SharedOptions(ASOptions.ClientOptions options)
    {
        SetOptions(options);
    }

    public void CustomSerialize(Serializer serializer)
    {
        serializer.Serialize(ref ClientOptions.allowRevival);
        serializer.Serialize(ref ClientOptions.allowSelfAscension);
        serializer.Serialize(ref ClientOptions.requireKarmaFlower);
        serializer.Serialize(ref ClientOptions.revivalHealthFactor);
    }

    public override string ToString() => $"ASRPCs.SharedOptions => {FormatOptions()}";
}