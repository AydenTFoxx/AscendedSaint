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
public class PossessionManager(Player player)
{
    private const int MAX_POSSESSION_TIME = 520;

    // TODO: Add extra data for stored creature instead of Player ref?
    private readonly WeakDictionary<Creature, Player> MyPossessions = [];
    private Player.PlayerController controller;
    private bool actionButtonPressed;

    public int PossessionCooldown { get; private set; } = 0;
    public int PossessionTime { get; private set; } = MAX_POSSESSION_TIME;

    public bool IsPossessing => MyPossessions.Count > 0;

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
    public bool CanPossessCreature(Creature target) => CanPossessCreature() && target is not null && target is not Player && IsPossessionValid(target);

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
    public bool HasPossession(Creature target) => MyPossessions.ContainsKey(target);

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
        MyPossessions.Add(target, player);
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

        player.room.AddObject(new ReverseShockwave(target.mainBodyChunk.pos, 48f, 0.1f, 32));
        player.room.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);

        target.UpdateCachedPossession();
        target.abstractCreature.controlled = false;
    }

    /// <summary>
    /// Updates the player's possession behaviors and controls.
    /// </summary>
    public void Update()
    {
        if (InputHandler.IsKeyPressed(player, InputHandler.Keys.POSSESSION_KEY))
        {
            UpdateControls();
        }
        else if (actionButtonPressed)
        {
            actionButtonPressed = false;
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
    /// Evaluates which action to perform upon pressing the Special ability button.
    /// </summary>
    public void UpdateControls()
    {
        if (actionButtonPressed) return;

        if (CanPossessCreature())
        {
            CLLogger.LogDebug("Checking creatures!");

            player.room?.abstractRoom.creatures.ForEach(crit =>
            {
                if (IsPossessing) return;

                if (CanPossessCreature(crit.realizedCreature))
                {
                    try
                    {
                        StartPossession(crit.realizedCreature);
                        CLLogger.LogInfo($"{player} has possessed {crit}!");
                    }
                    catch (System.Exception ex)
                    {
                        CLLogger.LogError($"Failed to possess {crit}!", ex);
                    }
                }
                else
                {
                    CLLogger.LogInfo($"Skipping: {crit}");
                }
            });
        }
        else if (IsPossessing)
        {
            CLLogger.LogInfo($"Removing all possessions of {player}!");

            ResetAllPossessions();
        }

        actionButtonPressed = true;
    }

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

    /// <summary>
    /// Retrieves a <c>string</c> representation of this <c>PossessionManager</c> instance.
    /// </summary>
    /// <returns>A <c>string</c> containing the instance's values and possessions.</returns>
    public override string ToString() => $"{nameof(PossessionManager)} => ({FormatPossessions(MyPossessions.Keys)}) [{PossessionTime}t; {PossessionCooldown}c]";
}