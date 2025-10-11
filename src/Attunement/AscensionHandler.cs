using System.Collections.Generic;
using System.Linq;
using ModLib.Generics;
using UnityEngine;

namespace AscendedSaint.Attunement;

public static class AscensionHandler
{
    public static IAscensionImpl? AscensionImpl { get; set; }

    private static WeakDictionary<PhysicalObject, AscensionCooldown> _ascensionCooldowns = [];
    private static readonly float defaultCooldown = Time.fixedDeltaTime * 20f;

    private static bool HasAscensionCooldown(this PhysicalObject self) => self.GetAscensionCooldown() > 0f;

    private static float GetAscensionCooldown(this PhysicalObject self) =>
        _ascensionCooldowns.TryGetValue(self, out AscensionCooldown result) && !result.IsExpired
            ? result.Duration : 0f;

    private static void SetAscensionCooldown(this PhysicalObject self, float duration) =>
        _ascensionCooldowns.Add(self, new AscensionCooldown(duration));

    /// <summary>
    /// Attempts to ascend the given object using the current ascension implementation.
    /// </summary>
    /// <param name="physicalObject">The object to be ascended or revived.</param>
    /// <param name="caller">The player who caused this action.</param>
    /// <returns><c>true</c> if the creature was successfully ascended, <c>false</c> otherwise.</returns>
    public static bool TryAscendObject(PhysicalObject physicalObject, Player caller)
    {
        if (AscensionImpl is null || physicalObject.HasAscensionCooldown()) return false;

        bool result;

        if (physicalObject is Creature creature)
        {
            result = AscensionImpl.TryAscendCreature(creature, caller);
        }
        else if (physicalObject is Oracle oracle)
        {
            result = AscensionImpl.TryAscendOracle(oracle, caller);
        }
        else
        {
            ModLib.Logger.LogWarning($"{physicalObject} is not a valid ascension target.");
            return false;
        }

        physicalObject.SetAscensionCooldown(defaultCooldown);

        return result;
    }

    /// <summary>
    /// Attempts to obtain the Karma Flower held by the player.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns>A Karma Flower held by the player, or <c>null</c> if none is found.</returns>
    public static KarmaFlower? GetHeldKarmaFlower(Player player) =>
        player.grasps.FirstOrDefault(grasp => grasp?.grabbed is KarmaFlower)?.grabbed as KarmaFlower;

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

            float shockWaveSize = isOracle ? 350f : 200;
            float shockWaveIntensity = isOracle ? 0.75f : 0.5f;
            int shockWaveLifetime = isOracle ? 24 : 30;
            float firecrackerPitch = isOracle ? 1.5f : 0.5f;
            float markPitch = isOracle ? 0.5f : 1.25f;

            room.AddObject(new ShockWave(pos, shockWaveSize, shockWaveIntensity, shockWaveLifetime));
            room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));

            room.PlaySound(SoundID.Firecracker_Bang, bodyChunk, loop: false, 1f, firecrackerPitch + Random.value);
            room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, bodyChunk, loop: false, 1f, markPitch + (Random.value * markPitch));
        }
        else
        {
            room.AddObject(new ShockWave(pos, 150f, 0.25f, 20));
        }

        ModLib.Logger.LogDebug($"Spawned {(isRevival ? "revival" : "ascension")} effects at {pos} for {target}.");
    }

    /// <summary>
    /// Updates the cooldowns of recently ascended or revived objects.
    /// </summary>
    public static void UpdateCooldowns()
    {
        if (_ascensionCooldowns.Count < 1) return;

        bool expired = false;

        foreach (KeyValuePair<PhysicalObject, AscensionCooldown> cooldown in _ascensionCooldowns)
        {
            cooldown.Value.Update(Time.deltaTime);

            expired = cooldown.Value.IsExpired;
        }

        if (expired)
        {
            _ascensionCooldowns = [.. _ascensionCooldowns.Where(c => !c.Value.IsExpired)];
        }
    }

    /// <summary>
    /// Represents a time limit where a creature cannot be affected by Saint's abilities.
    /// </summary>
    /// <param name="Duration">The amount of time the creature will be immune for.</param>
    private sealed record AscensionCooldown(float Duration)
    {
        public float Duration { get; private set; } = Duration;
        public bool IsExpired { get; private set; }

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