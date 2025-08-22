using System.Collections.Generic;
using System.Text;
using ControlLib.Utils;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession;

/// <summary>
/// Stores and manages the player's possessed creatures.
/// </summary>
/// <param name="player">The player itself.</param>
public sealed class PossessionManager
{
    private const int MAX_POSSESSION_TIME = 520;

    private readonly WeakCollection<Creature> MyPossessions = [];
    private readonly Player player;
    private Player.PlayerController controller;

    public TargetSelector TargetSelector { get; private set; }
    public int PossessionCooldown { get; private set; } = 0;
    public int PossessionTime { get; private set; } = MAX_POSSESSION_TIME;
    public bool IsPossessing => MyPossessions.Count > 0;

    public PossessionManager(Player player)
    {
        this.player = player;

        TargetSelector = new(player, this);
    }

    /// <summary>
    /// Retrieves the player associated with this <c>PossessionManager</c> instance.
    /// </summary>
    /// <returns>The <c>Player</c> who owns this manager instance.</returns>
    public Player GetPlayer() => player;

    /// <summary>
    /// Determines if the player is allowed to start a new possession.
    /// </summary>
    /// <returns><c>true</c> if the player can use their possession ability, <c>false</c> otherwise.</returns>
    public bool CanPossessCreature() => !IsPossessing && PossessionTime > 0 && PossessionCooldown == 0;

    /// <summary>
    /// Determines if the player can possess the given creature.
    /// </summary>
    /// <param name="target">The creature to be tested.</param>
    /// <returns><c>true</c> if the player can use their possession ability, <c>false</c> otherwise.</returns>
    public bool CanPossessCreature(Creature target) =>
        CanPossessCreature()
        && target is not (null or Player)
        && !target.TryGetPossession(out _)
        && IsPossessionValid(target);

    /// <summary>
    /// Validates the player's possession of a given creature.
    /// </summary>
    /// <param name="target">The creature to be tested.</param>
    /// <returns><c>true</c> if this possession is valid, <c>false</c> otherwise.</returns>
    public bool IsPossessionValid(Creature target) => player.Consious && !target.dead && target.room == player.room;

    /// <summary>
    /// Determines if the player is currently possessing the given creature.
    /// </summary>
    /// <param name="target">The creature to be tested.</param>
    /// <returns><c>true</c> if the player is possessing this creature, <c>false</c> otherwise.</returns>
    public bool HasPossession(Creature target) => MyPossessions.Contains(target);

    /// <summary>
    /// Removes all possessions of the player. Possessed creatures will automatically stop their own possessions.
    /// </summary>
    public void ResetAllPossessions() => MyPossessions.Clear();

    /// <summary>
    /// Initializes a new possession with the given creature as a target.
    /// </summary>
    /// <param name="target">The creature to possess.</param>
    public void StartPossession(Creature target)
    {
        MyPossessions.Add(target);
        PossessionCooldown = 20;

        player.room.AddObject(new ShockWave(target.mainBodyChunk.pos, 64f, 0.5f, 24));
        player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, target.mainBodyChunk, loop: false, 1f, 1.25f + (Random.value * 1.25f));

        controller ??= player.controller;
        player.controller = new Player.NullController();

        target.UpdateCachedPossession();
        target.abstractCreature.controlled = true;
    }

    /// <summary>
    /// Interrupts the possession of the given creature.
    /// </summary>
    /// <param name="target">The creature to stop possessing.</param>
    public void StopPossession(Creature target)
    {
        MyPossessions.Remove(target);
        PossessionCooldown = 20;

        if (MyPossessions.Count == 0)
        {
            player.controller = controller;
            controller = null;
        }

        if (PossessionTime == 0)
        {
            for (int k = 0; k < 20; k++)
            {
                player.room.AddObject(new Spark(target.mainBodyChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
            }
        }

        player.room.AddObject(new ReverseShockwave(target.mainBodyChunk.pos, 48f, 1f, 32));
        player.room.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);

        target.UpdateCachedPossession();
        target.abstractCreature.controlled = false;
    }

    /// <summary>
    /// Updates the player's possession behaviors and controls.
    /// </summary>
    public void Update()
    {
        if (InputHandler.IsKeyPressed(player, InputHandler.Keys.POSSESS))
        {
            TargetSelector.Update();
        }
        else
        {
            if (TargetSelector.Input.IsActive && TargetSelector.IsTargetValid())
                TargetSelector.ConfirmSelection();

            if (TargetSelector.Input.LockAction)
                TargetSelector.Input.LockAction = false;
        }

        if (IsPossessing)
        {
            player.Blink(10);

            PossessionTime--;

            if (PossessionTime < 1 || !player.Consious)
            {
                player.Stun(20);
                player.aerobicLevel = 1f;

                ResetAllPossessions();
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

    /// <summary>
    /// Retrieves a <c>string</c> representation of this <c>PossessionManager</c> instance.
    /// </summary>
    /// <returns>A <c>string</c> containing the instance's values and possessions.</returns>
    public override string ToString() => $"{nameof(PossessionManager)} => ({FormatPossessions(MyPossessions)}) [{PossessionTime}t; {PossessionCooldown}c]";

    /// <summary>
    /// Formats a list all of the player's possessed creatures for logging purposes.
    /// </summary>
    /// <param name="possessions">A list of the player's possessed creatures.</param>
    /// <returns>A formatted <c>string</c> listing all of the possessed creatures' names and IDs.</returns>
    private string FormatPossessions(ICollection<Creature> possessions)
    {
        StringBuilder stringBuilder = new();

        foreach (Creature creature in possessions)
        {
            stringBuilder.Append($"{creature};");
        }

        return stringBuilder.ToString();
    }
}