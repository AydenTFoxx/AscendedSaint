using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ControlLib.Utils;
using ControlLib.Utils.Generics;
using RWCustom;
using UnityEngine;

namespace ControlLib.Possession;

/// <summary>
/// Selects creatures for possession based on relevance and distance to the player.
/// </summary>
/// <param name="player">The player itself.</param>
/// <param name="manager">The player's <c>PossessionManager</c> instance.</param>
public class TargetSelector(Player player, PossessionManager manager)
{
    public WeakList<Creature> Targets { get; private set; } = [];
    public TargetSelectionState State { get; private set; } = TargetSelectionState.Idle;
    public TargetInput Input { get; private set; } = new();

    public bool HasValidTargets
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

    private WeakList<Creature> queryCreatures = [];

    /// <summary>
    /// Applies the current selection of targets for possession.
    /// </summary>
    public void ApplySelectedTargets()
    {
        MoveToState(TargetSelectionState.Ready);

        UpdateStates();
    }

    /// <summary>
    /// Moves the internal state machine to the given state.
    /// </summary>
    /// <param name="state">The new state of the state machine.</param>
    public void MoveToState(TargetSelectionState state)
    {
        if (State == state)
        {
            CLLogger.LogWarning($"Cannot move to a state the instance is already at: {state}");
        }
        else
        {
            if (state - State > 1)
            {
                CLLogger.LogInfo($"Warning! Skipping from {State} to {state}.");
            }

            State = state;
            CLLogger.LogInfo($"Moving into state: {State}");
        }
    }

    /// <summary>
    /// Resets the selector's input data. Also restores the player's controls if they have no possession.
    /// </summary>
    public void ResetSelectorInput()
    {
        Input = new();

        if (!manager.IsPossessing)
        {
            player.controller = null;
        }
    }

    /// <summary>
    /// Updates the target selector's behaviors.
    /// </summary>
    public void Update()
    {
        if (Input.LockAction) return;

        Input.InputTime++;

        if (Input.InputTime > manager.PossessionTimePotential || manager.PossessionCooldown > 0)
        {
            Input.LockAction = true;
            return;
        }

        UpdateStates();

        if (HasValidTargets)
        {
            if (player.graphicsModule is PlayerGraphics playerGraphics)
                playerGraphics.LookAtObject(Targets.First());

            if (Input.InputTime % 4 == 0)
            {
                foreach (Creature target in Targets)
                {
                    player.room?.AddObject(new ShockWave(target.mainBodyChunk.pos, 64f, 0.05f, 4));
                }
            }
        }
    }

    /// <summary>
    /// Updates the target selector's internal states.
    /// </summary>
    /// <param name="isRecursive">If this function has recursively called itself.</param>
    /// <exception cref="InvalidEnumArgumentException">The current state is not recognized by the state machine.</exception>
    private void UpdateStates(bool isRecursive = false)
    {
        switch (State)
        {
            case TargetSelectionState.Idle:
                {
                    if (manager.IsPossessing)
                    {
                        manager.ResetAllPossessions();

                        Input.LockAction = true;
                        Input.Initialized = true;

                        return;
                    }

                    player.controller ??= PossessionManager.GetFadeOutController(player);
                    queryCreatures = QueryCreatures(player);

                    MoveToState(TargetSelectionState.Querying);
                    break;
                }

            case TargetSelectionState.Querying:
                {
                    if (player.mushroomCounter < 10)
                    {
                        player.mushroomCounter = 10;
                    }

                    if (!UpdateInputOffset() && !isRecursive) return;

                    if (queryCreatures.Count > 0)
                    {
                        if (TrySelectNewTarget(Targets.ElementAtOrDefault(0), out Creature? target))
                        {
                            Targets = [target!];

                            if (player.monkAscension
                                && TrySelectNewTarget(Targets.First().Template, out WeakList<Creature>? targets))
                            {
                                Targets = targets;
                            }
                        }
                        else
                        {
                            CLLogger.LogInfo("Target was invalid, ignoring.");
                        }
                    }
                    else if (!isRecursive)
                    {
                        CLLogger.LogInfo("Query is empty; Refreshing.");

                        queryCreatures = QueryCreatures(player);

                        UpdateStates(isRecursive: true);
                    }
                    else
                    {
                        Input.LockAction = true;
                        if (player.mushroomCounter < 20)
                        {
                            player.mushroomCounter = 20;
                        }
                        CLLogger.LogWarning("Failed to query for creatures in the room; Aborting operation.");
                    }

                    break;
                }

            case TargetSelectionState.Ready:
                {
                    queryCreatures.Clear();

                    if (Targets is null or { Count: 0 })
                    {
                        CLLogger.LogWarning("List is null or empty; Aborting operation.");

                        MoveToState(TargetSelectionState.Idle);
                        return;
                    }

                    if (Input.InputTime > manager.PossessionTimePotential)
                    {
                        CLLogger.LogInfo("Player took too long, ignoring input.");

                        Targets.Clear();

                        MoveToState(TargetSelectionState.Idle);
                        return;
                    }

                    foreach (Creature target in Targets)
                    {
                        if (manager.CanPossessCreature(target))
                        {
                            manager.StartPossession(target);
                        }
                    }

                    CLLogger.LogInfo($"Started the possession of {Targets.Count} target(s): {PossessionManager.FormatPossessions(Targets)}");

                    player.monkAscension = false;
                    Targets.Clear();

                    MoveToState(TargetSelectionState.Idle);
                    break;
                }

            default:
                Input.LockAction = true;
                throw new InvalidEnumArgumentException(nameof(State), (int)State, State.GetType());
        }
    }

    /// <summary>
    /// Retrieves the player's input and updates the target selector's input offset.
    /// </summary>
    /// <returns><c>true</c> if the value of <c>Input.Offset</c> has changed, <c>false</c> otherwise.</returns>
    private bool UpdateInputOffset()
    {
        Player.InputPackage input = InputHandler.GetVanillaInput(player);
        int offset = input.x + input.y;

        if (offset == 0 && Input.Offset != offset)
            Input.Offset = 0;

        if (Input.Offset == offset && Input.Initialized)
            return false;

        Input.Offset = offset;
        Input.Initialized = true;

        return true;
    }

    /// <summary>
    /// Attempts to select all creatures of a given template for possession.
    /// </summary>
    /// <param name="template">The creature template to search for.</param>
    /// <param name="targets">The list of valid targets with that template; May be an empty list.</param>
    /// <returns><c>true</c> if the output of <c><paramref name="targets"/></c> is greater than zero, <c>false</c> otherwise.</returns>
    private bool TrySelectNewTarget(CreatureTemplate template, out WeakList<Creature> targets)
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
    private bool TrySelectNewTarget(Creature? lastCreature, out Creature? target)
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
    private static List<Creature> GetAllCreatures(Player player, CreatureTemplate template) =>
        [.. player.room.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => c is not null && c.Template == template)
        ];

    /// <summary>
    /// Retrieves all valid creatures for possession within the player's possession range.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns>A list of all possessable creatures in the room, if any.</returns>
    private static List<Creature> GetCreatures(Player player)
    {
        if (player.room is null)
        {
            CLLogger.LogWarning($"{player} is not in a room; Cannot query for creatures there.");
            return [];
        }

        List<Creature> creatures = [.. player.room.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => c != player && IsValidSelectionTarget(c) && IsInPossessionRange(player, c))
        ];

        creatures.Sort(new TargetSorter(player.mainBodyChunk.pos));

        return creatures;
    }

    /// <summary>
    /// Determines if the given creature is within possession range of the player.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <param name="creature">The creature to possess.</param>
    /// <returns><c>true</c> if the creature is within possession range, <c>false</c> otherwise.</returns>
    private static bool IsInPossessionRange(Player player, Creature creature) =>
        Custom.DistLess(player.mainBodyChunk.pos, creature.mainBodyChunk.pos, (player.room?.game.session is ArenaGameSession) ? 1024f : 720f);

    /// <summary>
    /// Determines if the given creature is a valid target for possession.
    /// </summary>
    /// <param name="creature">The creature to be tested.</param>
    /// <returns><c>true</c> if the creature can be possessed, <c>false</c> otherwise.</returns>
    private static bool IsValidSelectionTarget(Creature creature) =>
        !PossessionManager.IsBannedPossessionTarget(creature) && !creature.TryGetPossession(out _);

    /// <summary>
    /// Retrieves a list of all possessable creatures in the player's room.
    /// </summary>
    /// <param name="player">The player itself.</param>
    /// <returns>A list of potential targets for possession, if any.</returns>
    /// <remarks>
    /// If the player is currently using Saint's Ascension ability, the returned list will only contain
    /// one item per creature template (that is, one Yellow Lizard, one Small Centipede, etc.)
    /// </remarks>
    private static WeakList<Creature> QueryCreatures(Player player) =>
        player.monkAscension
            ? [.. GetCreatures(player).Distinct(new TargetEqualityComparer())]
            : [.. GetCreatures(player)];

    /// <summary>
    /// Sorts a list of creatures based on their distance to a given point.
    /// </summary>
    /// <param name="playerPos">The position for measuring distance to.</param>
    /// <remarks>Creatures with no <c>HealthState</c> have inherently lower priority.</remarks>
    private class TargetSorter(Vector2 playerPos) : IComparer<Creature>
    {
        public int Compare(Creature x, Creature y)
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
    private class TargetEqualityComparer : IEqualityComparer<Creature>
    {
        public bool Equals(Creature x, Creature y) => x.Template == y.Template;
        public int GetHashCode(Creature obj) => base.GetHashCode();
    }

    /// <summary>
    /// Stores the selector's input-related states and values.
    /// </summary>
    public record class TargetInput
    {
        public bool Initialized { get; set; }
        public bool LockAction { get; set; }
        public int InputTime { get; set; }
        public int Offset { get; set; }
    }

    /// <summary>
    /// The potential states of the internal state machine.
    /// </summary>
    public enum TargetSelectionState
    {
        Idle,
        Querying,
        Ready
    }
}