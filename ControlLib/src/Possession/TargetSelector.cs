using System;
using System.Collections.Generic;
using System.Linq;
using ControlLib.Possession.Graphics;
using ControlLib.Utils;
using ControlLib.Utils.Generics;
using RWCustom;
using UnityEngine;
using static ControlLib.Utils.OptionUtils;

namespace ControlLib.Possession;

/// <summary>
/// Selects creatures for possession based on relevance and distance to the player.
/// </summary>
/// <param name="player">The player itself.</param>
/// <param name="manager">The player's <c>PossessionManager</c> instance.</param>
public partial class TargetSelector(Player player, PossessionManager manager)
{
    public WeakList<Creature> Targets { get; protected set; } = [];
    public TargetSelectionState State { get; protected set; } = TargetSelectionState.Idle;
    public TargetInput Input { get; protected set; } = new();

    public virtual bool HasValidTargets
    {
        get
        {
            if (Targets is null or { Count: 0 }) return false;

            foreach (Creature target in Targets)
            {
                if (!IsValidSelectionTarget(target) || target.room != player.room)
                {
                    Targets.Remove(target);
                }
            }

            return Targets.Count > 0;
        }
    }

    public bool _exceededTimeLimit;
    public bool ExceededTimeLimit
    {
        get
        {
            if (Input.InputTime > PossessionManager.PossessionTimePotential)
            {
                _exceededTimeLimit = true;
            }
            return _exceededTimeLimit;
        }
        set => _exceededTimeLimit = value;
    }

    public Player Player => player;
    public PossessionManager PossessionManager => manager;

    protected WeakList<Creature> queryCreatures = [];
    protected TargetCursor targetCursor = new(manager);

    /// <summary>
    /// Applies the current selection of targets for possession.
    /// </summary>
    public virtual void ApplySelectedTargets()
    {
        MoveToState(TargetSelectionState.Ready);

        State.UpdatePhase(this);
    }

    public void QueryTargetCursor()
    {
        if (State is not QueryingState queryingState) return;

        queryCreatures = QueryCreatures(player, targetCursor);
        queryingState.UpdatePhase(this, isRecursive: true);

        Input.QueriedCursor = true;

        if (IsClientOptionValue(CLOptions.SELECTION_MODE, "ascension"))
        {
            targetCursor.ResetCursor();
        }
    }

    /// <summary>
    /// Moves the internal state machine to the given state.
    /// </summary>
    /// <param name="state">The new state of the state machine.</param>
    public void MoveToState(TargetSelectionState state)
    {
        try
        {
            State = State.MoveToState(state);
        }
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to move to state {state}!", ex);
        }
    }

    /// <summary>
    /// Resets the selector's input data. Also restores the player's controls if they have no possession.
    /// </summary>
    public void ResetSelectorInput()
    {
        Input = new();

        if (!PossessionManager.IsPossessing)
        {
            player.controller = null;
        }
    }

    /// <summary>
    /// Updates the target selector's behaviors.
    /// </summary>
    public virtual void Update()
    {
        if (Input.LockAction) return;

        Input.InputTime++;

        if (ExceededTimeLimit || PossessionManager.PossessionCooldown > 0)
        {
            if (ExceededTimeLimit)
            {
                PossessionManager.PossessionCooldown = 80;

                player.exhausted = true;
                player.aerobicLevel = 1f;
            }

            Input.LockAction = true;
            return;
        }

        State.UpdatePhase(this);

        if (HasValidTargets)
        {
            if (player.graphicsModule is PlayerGraphics playerGraphics)
                playerGraphics.LookAtObject(Targets.First());

            int module = CompatibilityManager.IsRainMeadowEnabled() && !IsOptionEnabled(CLOptions.MEADOW_SLOWDOWN) ? 8 : 4;

            if (Input.InputTime % module == 0)
            {
                foreach (Creature target in Targets)
                {
                    player.room?.AddObject(new ShockWave(target.mainBodyChunk.pos, 64f, 0.05f, module));
                }
            }
        }
    }

    /// <summary>
    /// Retrieves the player's input and updates the target selector's input offset.
    /// </summary>
    /// <returns><c>true</c> if the value of <c>Input.Offset</c> has changed, <c>false</c> otherwise.</returns>
    protected virtual bool UpdateInputOffset()
    {
        Player.InputPackage input = InputHandler.GetVanillaInput(player);
        int offset = input.x + input.y;

        if (IsClientOptionValue(CLOptions.SELECTION_MODE, "ascension"))
        {
            if (input.x == 0 && input.y == 0 && Input.Initialized) return false;

            if (targetCursor.slatedForDeletetion)
            {
                CLLogger.LogWarning($"{targetCursor} was deleted; Recreating object.");

                targetCursor = new(manager);
            }

            targetCursor.UpdateCursor(input);

            Input.Initialized = true;
            return false;
        }

        if (offset == 0 && Input.Offset != offset)
            Input.Offset = 0;

        if (Input.Offset == offset && Input.Initialized)
            return false;

        Input.Offset = IsClientOptionEnabled(CLOptions.INVERT_CONTROLS) ? -offset : offset;
        Input.Initialized = true;

        return true;
    }

    /// <summary>
    /// Attempts to select all creatures of a given template for possession.
    /// </summary>
    /// <param name="template">The creature template to search for.</param>
    /// <param name="targets">The list of valid targets with that template; May be an empty list.</param>
    /// <returns><c>true</c> if the output of <c><paramref name="targets"/></c> is greater than zero, <c>false</c> otherwise.</returns>
    protected virtual bool TrySelectNewTarget(CreatureTemplate template, out WeakList<Creature> targets)
    {
        List<Creature> allCrits = GetAllCreatures(player, template);

        targets = [];

        foreach (Creature target in allCrits)
        {
            if (IsValidSelectionTarget(target))
            {
                targets.Add(target);
            }
        }

        return targets.Count > 0;
    }

    /// <summary>
    /// Attempts to select a new target for possession based on the previous selection.
    /// </summary>
    /// <param name="lastCreature">The last creature to be selected; Can be <c>null</c>.</param>
    /// <param name="target">The new selected creature for possession; May be <c>null</c>.</param>
    /// <returns><c>true</c> if a valid creature was selected, <c>false</c> otherwise.</returns>
    protected virtual bool TrySelectNewTarget(Creature? lastCreature, out Creature? target)
    {
        int i = (lastCreature is not null ? queryCreatures.IndexOf(lastCreature) : 0) + Input.Offset;

        target = queryCreatures.ElementAtOrDefault(i);
        target ??= (Input.Offset > 0) ? queryCreatures.First() : queryCreatures.Last();

        if (!IsValidSelectionTarget(target))
        {
            CLLogger.LogWarning($"{target} is not a valid possession target.");

            queryCreatures.Remove(target);

            target = null;
        }

        return target is not null;
    }

    /// <summary>
    /// Retrieves all creatures in the player's room of a given template.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <param name="template">The creature template to seach for.</param>
    /// <returns>A list of all creatures in the room with the given template, if any.</returns>
    public static List<Creature> GetAllCreatures(Player player, CreatureTemplate template) =>
        [.. player.room.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => c is not null && GetCreatureSelector(template).Invoke(c))
        ];

    /// <summary>
    /// Retrieves the selector predicate to be used for determining creature type matches.
    /// </summary>
    /// <param name="template">The creature template to be tested.</param>
    /// <returns>A <c>Predicate</c> for evaluating if a creature is of a given type.</returns>
    public static Predicate<Creature> GetCreatureSelector(CreatureTemplate template) =>
        IsOptionEnabled(CLOptions.WORLDWIDE_MIND_CONTROL)
            ? IsValidSelectionTarget
            : IsOptionEnabled(CLOptions.POSSESS_ANCESTORS)
                ? c => c.Template.ancestor == template.ancestor
                : c => c.Template == template;

    /// <summary>
    /// Retrieves all valid creatures for possession within the player's possession range.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns>A list of all possessable creatures in the room, if any.</returns>
    public static List<Creature> GetCreatures(Player player, TargetCursor cursor)
    {
        if (player.room is null)
        {
            CLLogger.LogWarning($"{player} is not in a room; Cannot query for creatures there.");
            return [];
        }

        Vector2 pos = IsClientOptionValue(CLOptions.SELECTION_MODE, "ascension")
            ? cursor.targetPos + cursor.camPos
            : player.mainBodyChunk.pos;

        List<Creature> creatures = [.. player.room.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => c != player && IsValidSelectionTarget(c) && IsInPossessionRange(pos, c))
        ];

        creatures.Sort(new TargetSorter(pos));

        return creatures;
    }

    /// <summary>
    /// Retrieves the max range at which the player can possess creatures.
    /// </summary>
    /// <param name="gameSession">The current game session.</param>
    /// <returns>A float determining how far a creature can be from the player to be eligible for possession.</returns>
    public static float GetPossessionRange(GameSession? gameSession) =>
        IsOptionEnabled(CLOptions.WORLDWIDE_MIND_CONTROL)
            ? 9999f
            : IsClientOptionValue(CLOptions.SELECTION_MODE, "ascension")
                ? 120f
                : gameSession is ArenaGameSession
                    ? 1024f
                    : 720f;

    /// <summary>
    /// Determines if the given creature is within possession range of the player.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <param name="creature">The creature to possess.</param>
    /// <returns><c>true</c> if the creature is within possession range, <c>false</c> otherwise.</returns>
    public static bool IsInPossessionRange(Vector2 pos, Creature creature) =>
        Custom.DistLess(pos, creature.mainBodyChunk.pos, GetPossessionRange(creature.room?.game.session));

    /// <summary>
    /// Determines if the given creature is a valid target for possession.
    /// </summary>
    /// <param name="creature">The creature to be tested.</param>
    /// <returns><c>true</c> if the creature can be possessed, <c>false</c> otherwise.</returns>
    public static bool IsValidSelectionTarget(Creature creature) =>
        !PossessionManager.IsBannedPossessionTarget(creature) && !creature.abstractCreature.controlled;

    /// <summary>
    /// Retrieves a list of all possessable creatures in the player's room.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <returns>A list of potential targets for possession, if any.</returns>
    /// <remarks>
    /// If the player is currently using Saint's Ascension ability, the returned list will only contain
    /// one item per creature template (that is, one Yellow Lizard, one Small Centipede, etc.)
    /// </remarks>
    public static WeakList<Creature> QueryCreatures(Player player, TargetCursor cursor) =>
        (player.monkAscension && !IsOptionEnabled(CLOptions.FORCE_MULTITARGET_POSSESSION))
            ? [.. GetCreatures(player, cursor).Distinct(new TargetEqualityComparer())]
            : [.. GetCreatures(player, cursor)];

    /// <summary>
    /// Evaluates the value to be set for the player's <c>mushroomCounter</c> field.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="count">The value to be set.</param>
    /// <returns>The new value for the player's <c>mushroomCounter</c> field.</returns>
    /// <remarks>If the player is in a Rain Meadow lobby, this will also depend on the host's <c>meadowSlowdown</c> setting.</remarks>
    public static int SetMushroomCounter(Player player, int count) =>
        ShouldSetMushroomCounter(player, count)
            ? count
            : player.mushroomCounter;

    /// <summary>
    /// Determines if the player's <c>mushroomCounter</c> field should be updated.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <param name="count">The value to be set.</param>
    /// <returns><c>true</c> if the value should be updated, <c>false</c> otherwise.</returns>
    /// <remarks>Has explicit support for Rain Meadow compatibility, where the host's options are also taken into account for this check.</remarks>
    public static bool ShouldSetMushroomCounter(Player player, int count) =>
        CompatibilityManager.IsRainMeadowEnabled()
            ? (!MeadowUtils.IsOnline || IsOptionEnabled(CLOptions.MEADOW_SLOWDOWN)) && player.mushroomCounter < count
            : player.mushroomCounter < count;

    /// <summary>
    /// Sorts a list of creatures based on their distance to a given point.
    /// </summary>
    /// <param name="playerPos">The position for measuring distance to.</param>
    /// <remarks>Creatures with no <c>HealthState</c> have inherently lower priority.</remarks>
    protected class TargetSorter(Vector2 playerPos) : IComparer<Creature>
    {
        public virtual int Compare(Creature x, Creature y)
        {
            if (x.State is not HealthState && y.State is HealthState) return 1;

            float xDist = Vector2.Distance(x.mainBodyChunk.pos, playerPos);
            float yDist = Vector2.Distance(y.mainBodyChunk.pos, playerPos);

            return xDist < yDist
                ? -1
                : xDist == yDist
                    ? 0
                    : 1;
        }
    }

    /// <summary>
    /// Compares two creatures and returns <c>true</c> if both share the same template.
    /// </summary>
    protected class TargetEqualityComparer : IEqualityComparer<Creature>
    {
        public virtual bool Equals(Creature x, Creature y) => x.Template == y.Template;
        public virtual int GetHashCode(Creature obj) => base.GetHashCode();
    }

    /// <summary>
    /// Stores the selector's input-related states and values.
    /// </summary>
    public record class TargetInput
    {
        public bool Initialized { get; set; }
        public bool LockAction { get; set; }
        public bool QueriedCursor { get; set; }
        public int InputTime { get; set; }
        public int Offset { get; set; }
    }
}