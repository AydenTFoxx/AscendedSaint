using System.Collections.Generic;
using System.Text;
using ControlLib.Utils;
using ControlLib.Utils.Generics;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession;

/// <summary>
/// Stores and manages the player's possessed creatures.
/// </summary>
/// <param name="player">The player itself.</param>
public sealed class PossessionManager
{
    public int PossessionTimePotential { get; }
    public int MaxPossessionTime => PossessionTimePotential + ((player.room?.game.session is StoryGameSession storySession ? storySession.saveState.deathPersistentSaveData.karma : 0) * 40);

    private readonly WeakCollection<Creature> MyPossessions = [];
    private readonly Player player;

    public TargetSelector TargetSelector { get; private set; }
    public int PossessionCooldown { get; private set; }
    public int PossessionTime { get; private set; }
    public bool IsPossessing => MyPossessions.Count > 0;

    public PossessionManager(Player player)
    {
        this.player = player;

        TargetSelector = new(player, this);

        PossessionTimePotential = player.SlugCatClass == SlugcatStats.Name.Yellow || player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint
            ? 520
            : player.SlugCatClass == SlugcatStats.Name.Red || player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer
                ? 180
                : player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel
                    ? 1
                    : 360;

        PossessionTime = MaxPossessionTime;
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
    public bool CanPossessCreature() => PossessionTime > 0 && PossessionCooldown == 0;

    /// <summary>
    /// Determines if the player can possess the given creature.
    /// </summary>
    /// <param name="target">The creature to be tested.</param>
    /// <returns><c>true</c> if the player can use their possession ability, <c>false</c> otherwise.</returns>
    public bool CanPossessCreature(Creature target) =>
        CanPossessCreature()
        && !IsBannedPossessionTarget(target)
        && !target.TryGetPossession(out _)
        && IsPossessionValid(target);

    public static bool IsBannedPossessionTarget(Creature target) => target is null or Player or Overseer or { dead: true };

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
    public void ResetAllPossessions()
    {
        MyPossessions.Clear();

        player.controller = null;
    }

    /// <summary>
    /// Initializes a new possession with the given creature as a target.
    /// </summary>
    /// <param name="target">The creature to possess.</param>
    public void StartPossession(Creature target)
    {
        if (player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel && player.room is not null)
        {
            ScavengerBomb bomb = new(
                new(
                    player.room.world,
                    AbstractPhysicalObject.AbstractObjectType.ScavengerBomb,
                    null,
                    player.abstractCreature.pos,
                    player.room.world.game.GetNewID()
                ),
                player.room.world
            );

            bomb.abstractPhysicalObject.RealizeInRoom();
            bomb.Explode(player.mainBodyChunk);

            CLLogger.LogMessage($"Game over, {player.SlugCatClass}.");
            return;
        }

        MyPossessions.Add(target);

        player.room?.AddObject(new TemplarCircle(target, target.mainBodyChunk.pos, 48f, 8f, 2f, 12, true));
        player.room?.AddObject(new ShockWave(target.mainBodyChunk.pos, 100f, 0.08f, 4, false));
        player.room?.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, target.mainBodyChunk, loop: false, 1f, 1.25f + (Random.value * 1.25f));

        player.controller ??= GetFadeOutController(player);

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

        if (!IsPossessing)
        {
            player.controller = null;
        }

        if (PossessionTime == 0)
        {
            for (int k = 0; k < 20; k++)
            {
                target.room?.AddObject(new Spark(target.mainBodyChunk.pos, Custom.RNV() * Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
            }
        }

        target.room?.AddObject(new ReverseShockwave(target.mainBodyChunk.pos, 64f, 0.05f, 24));
        player.room?.PlaySound(SoundID.HUD_Pause_Game, target.mainBodyChunk, loop: false, 1f, 0.5f);

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
        else if (TargetSelector.Input.Initialized)
        {
            TargetSelector.ResetSelectorInput();

            if (TargetSelector.HasValidTargets)
                TargetSelector.ApplySelectedTargets();
        }

        if (IsPossessing)
        {
            player.Blink(10);

            PossessionTime--;

            if (PossessionTime < 1 || !player.Consious)
            {
                if (player.Consious)
                {
                    player.aerobicLevel = 1f;
                    player.airInLungs *= 0.25f;
                    player.exhausted = true;
                    player.Stun(35);
                }

                ResetAllPossessions();
            }
        }
        else if (PossessionTime < MaxPossessionTime)
        {
            PossessionTime++;
        }

        if (PossessionCooldown > 0)
        {
            PossessionCooldown--;
        }
    }

    public static FadeOutController GetFadeOutController(Player player)
    {
        Player.InputPackage input = InputHandler.GetVanillaInput(player);

        return new FadeOutController(input.x, player.standing ? 1 : input.y);
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
    public static string FormatPossessions(ICollection<Creature> possessions)
    {
        StringBuilder stringBuilder = new();

        foreach (Creature creature in possessions)
        {
            stringBuilder.Append($"{creature};");
        }

        return stringBuilder.ToString();
    }

    public class FadeOutController(int x, int y) : Player.PlayerController
    {
        public int FadeOutX() => x = (int)Mathf.Lerp(x, 0f, 20f);
        public int FadeOutY() => y = (int)Mathf.Lerp(y, 0f, 20f);

        public override Player.InputPackage GetInput() =>
            new(gamePad: false, Options.ControlSetup.Preset.None, FadeOutX(), FadeOutY(), jmp: false, thrw: false, pckp: false, mp: false, crouchToggle: false);
    }
}