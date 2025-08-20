using ControlLib.Utils;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession;

public class PossessionManager(Player player)
{
    private const int MAX_POSSESSION_TIME = 520;

    // TODO: Add extra data for stored creature instead of Player ref?
    public WeakDictionary<Creature, Player> MyPossessions { get; private set; } = [];

    public int PossessionsCount { get; private set; }
    public int PossessionCooldown { get; private set; }

    public int PossessionTime { get; private set; } = MAX_POSSESSION_TIME;

    public bool IsPossessing => PossessionsCount > 0;

    public bool CanPossessCreature() => !IsPossessing && PossessionTime > 0 && PossessionCooldown == 0;
    public bool CanPossessCreature(Creature target) => CanPossessCreature() && target is not null && target is not Player && !target.dead;

    public void ResetAllPossessions() => MyPossessions.Clear();

    public void StartPossession(Creature target)
    {
        MyPossessions.Add(target, player);

        PossessionsCount++;
        PossessionCooldown = 20;

        player.room.AddObject(new KarmicShockwave(target, target.mainBodyChunk.pos, 20, 20f, 32f));
        player.room.PlaySound(SoundID.Moon_Wake_Up_Swarmer_Ping, target.mainBodyChunk, loop: false, 1f, 1.25f + (Random.value * 0.5f));

        target.abstractCreature.controlled = true;
    }

    public void StopPossession(Creature target)
    {
        MyPossessions.Remove(target);

        PossessionsCount--;
        PossessionCooldown = 20;

        for (int k = 0; k < 20; k++)
        {
            player.room.AddObject(new Spark(target.mainBodyChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
        }

        player.room.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);

        target.abstractCreature.controlled = false;
    }

    public void Update()
    {
        if (IsPossessing)
        {
            player.Blink(10);

            PossessionTime--;

            if (PossessionTime < 1 || !player.Consious)
            {
                player.Stun(20);
                player.aerobicLevel = 1f;

                PossessionsCount = 0;
            }
        }
        else if (PossessionTime < MAX_POSSESSION_TIME)
        {
            PossessionTime++;
        }

        if (PossessionCooldown > 0)
        {
            PossessionCooldown--;
        }
    }

    public override string ToString() => $"{nameof(PossessionManager)} => [{PossessionsCount}p; {PossessionTime}t; {PossessionCooldown}c]";
}