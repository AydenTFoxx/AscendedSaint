using System.Collections.Generic;
using System.Linq;
using AscendedSaint.Attunement.Meadow;
using ModLib;
using ModLib.Collections;
using UnityEngine;

namespace AscendedSaint.Attunement;

public static class AscensionHandler
{
    private static IAscensionImpl? _ascensionImpl;
    private static WeakDictionary<PhysicalObject, AscensionCooldown> _ascensionCooldowns = [];

    public static float DefaultAscensionCooldown => Time.fixedDeltaTime * 200f;

    public static bool HasAscensionCooldown(this PhysicalObject self) => self.GetAscensionCooldown() > 0f;

    public static float GetAscensionCooldown(this PhysicalObject self) =>
        _ascensionCooldowns.TryGetValue(self, out AscensionCooldown result) && !result.IsExpired
            ? result.Duration : 0f;

    public static void SetAscensionCooldown(this PhysicalObject self, float duration, bool isRevival = false) =>
        _ascensionCooldowns[self] = new AscensionCooldown(duration, isRevival);

    /// <summary>
    /// Attempts to ascend the given object using the current ascension implementation.
    /// </summary>
    /// <param name="physicalObject">The object to be ascended or revived.</param>
    /// <param name="caller">The player who caused this action.</param>
    /// <returns><c>true</c> if the creature was successfully ascended, <c>false</c> otherwise.</returns>
    public static bool TryAscendObject(PhysicalObject physicalObject, Player caller)
    {
        if (_ascensionImpl is null) return false;

        bool result = false;

        if (physicalObject is Creature creature)
        {
            result = _ascensionImpl.TryAscendCreature(creature, caller);
        }
        else if (physicalObject is Oracle oracle)
        {
            result = _ascensionImpl.TryAscendOracle(oracle, caller);
        }
        else
        {
            Main.Logger.LogWarning($"{physicalObject} is not a valid ascension target.");
        }

        return result;
    }

    /// <summary>
    /// Attempts to obtain the Karma Flower held by the player.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns>A Karma Flower held by the player, or <c>null</c> if none is found.</returns>
    public static KarmaFlower? GetHeldKarmaFlower(Player player) =>
        player.grasps.FirstOrDefault(static grasp => grasp?.grabbed is KarmaFlower)?.grabbed as KarmaFlower;

    /// <summary>
    /// Spawns the special effects of Saint's new abilities.
    /// </summary>
    /// <param name="target">The object which was the target of this ability.</param>
    /// <param name="isRevival">If the performed ability was a revival.</param>
    public static void SpawnAscensionEffects(PhysicalObject target, bool isRevival = true)
    {
        Room room = target.room;
        Vector2 pos = target is Creature creature ? creature.mainBodyChunk.pos : target.bodyChunks[0].pos;

        if (isRevival)
        {
            BodyChunk bodyChunk = target is Creature creature1 ? creature1.mainBodyChunk : target.bodyChunks[0];

            bool isOracle = target is Oracle;
            float markPitch = isOracle ? 0.5f : 1.25f;

            room.AddObject(new ShockWave(pos, isOracle ? 350f : 200, isOracle ? 0.75f : 0.5f, isOracle ? 24 : 30));
            room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

            room.PlaySound(SoundID.Firecracker_Bang, bodyChunk, loop: false, 1f, (isOracle ? 1.5f : 0.5f) + Random.value);
            room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, bodyChunk, loop: false, 1f, markPitch + (Random.value * markPitch));
        }
        else
        {
            room.AddObject(new ShockWave(pos, 150f, 0.25f, 20));
        }

        Main.Logger.LogDebug($"Spawned {(isRevival ? "revival" : "ascension")} effects at {pos} for {target}.");
    }

    /// <summary>
    /// Sets the internal ascension implementation to the appropriate type, based on whether the current session is online or not.
    /// </summary>
    internal static void InitAscensionImpl()
    {
        _ascensionImpl = Extras.IsOnlineSession
            ? new MeadowAscensionImpl()
            : new VanillaAscensionImpl();

        Main.Logger.LogInfo($"Initialized new ascension implementation: {_ascensionImpl}");
    }

    /// <summary>
    /// Updates the cooldowns of recently ascended or revived objects.
    /// </summary>
    internal static void UpdateCooldowns()
    {
        if (_ascensionCooldowns.Count < 1) return;

        bool expired = false;

        foreach (KeyValuePair<PhysicalObject, AscensionCooldown> kvp in _ascensionCooldowns)
        {
            PhysicalObject physicalObject = kvp.Key;
            AscensionCooldown cooldown = kvp.Value;

            if (cooldown.IsRevival)
            {
                BodyChunk bodyChunk = physicalObject is Creature crit ? crit.mainBodyChunk : physicalObject.firstChunk;

                bodyChunk.vel.x = 0f;
                bodyChunk.vel.y = bodyChunk.owner.room.gravity * 1.25f;
            }

            cooldown.Update(Time.deltaTime);

            expired = cooldown.IsExpired;
        }

        if (expired)
        {
            _ascensionCooldowns = [.. _ascensionCooldowns.Where(static c => !c.Value.IsExpired)];
        }
    }

    /// <summary>
    /// Represents a time limit where a creature cannot be affected by Saint's abilities.
    /// </summary>
    /// <param name="Duration">The amount of time the creature will be immune for.</param>
    private sealed record AscensionCooldown
    {
        public float Duration { get; private set; }
        public bool IsExpired { get; private set; }

        public bool IsRevival { get; }

        public AscensionCooldown(float duration, bool isRevival)
        {
            Duration = duration;
            IsRevival = isRevival;
        }

        public void Update(float deltaTime)
        {
            if (IsExpired) return;

            Duration -= deltaTime;

            if (Duration <= 0f)
            {
                IsExpired = true;
            }
        }

        public override string ToString() => Duration.ToString();
    }
}