using System.Collections.Generic;
using System.Linq;
using ControlLib.Utils;
using RWCustom;
using static ControlLib.ControlLibMain;

namespace ControlLib.Possession;

public sealed class TargetSelector(Player player, PossessionManager manager)
{
    private const float POSSESSION_RADIUS = 720f;
    private const int MAX_INPUT_TIME = 120;

    private WeakList<Creature> creatures = [];

    public Creature? Target { get; private set; }
    public TargetInput Input { get; private set; } = new();

    public bool IsTargetValid() => Target is not null && manager.CanPossessCreature(Target);
    public void SetTargetPossession() => manager.StartPossession(Target!);
    public void ResetTargetPossession() => manager.StopPossession(Target!);

    public void ConfirmSelection()
    {
        Input = new();
        creatures.Clear();

        if (IsTargetValid() && Input.InputTime < MAX_INPUT_TIME)
        {
            SetTargetPossession();
        }

        if (Target is null)
        {
            manager.UnfreezePlayerControls();
        }
    }

    public void SelectNextCreature(int offset, Creature? lastTarget)
    {
        Input.IsActive = true;

        if (creatures is { Count: 0 })
        {
            creatures = GetSortedCreatures(player);
        }

        if (!TrySelectNextCreature(lastTarget, offset)) return;

        CLLogger.LogInfo($"Selected creature: {Target}");
    }

    internal bool TrySelectNextCreature(Creature? lastTarget = null, int offset = 0)
    {
        if (creatures is { Count: 0 }) return false;

        int i = lastTarget is not null ? creatures.IndexOf(lastTarget) + offset : offset;

        Target = creatures.ElementAtOrDefault(i);
        Target ??= (offset > 0) ? creatures.First() : creatures.Last();

        if (!IsTargetValid())
        {
            CLLogger.LogWarning($"{Target} is not a valid possession target.");

            creatures.Remove(Target);

            Target = null;
        }

        return Target is not null;
    }

    public void Update()
    {
        if (Input.LockAction || Input.InputTime > MAX_INPUT_TIME) return;

        if (Target is not null && manager.HasPossession(Target))
        {
            ResetTargetPossession();

            Input.LockAction = true;
            return;
        }
        else if (!manager.IsPossessing && manager.PossessionCooldown > 0)
        {
            Input.LockAction = true;
            return;
        }

        Input.InputTime++;

        if (player.mushroomCounter < 10)
        {
            player.mushroomCounter = 10;
        }

        if (player.controller is null)
        {
            manager.FreezePlayerControls();
        }

        if (Target is not null && player.graphicsModule is PlayerGraphics playerGraphics)
        {
            playerGraphics.LookAtObject(Target);

            if (Input.InputTime % 4 == 0)
            {
                player.room?.AddObject(new ShockWave(Target.mainBodyChunk.pos, 48f, 0.02f, 4));
            }
        }

        if (ClientOptions?.selectionMode == "classic")
        {
            UpdateClassicMode();
        } // TODO: Add "ascension" selection mode
    }

    public void UpdateClassicMode()
    {
        if (!Input.IsActive)
        {
            SelectNextCreature(0, null);
            return;
        }

        Player.InputPackage input = InputHandler.GetVanillaInput(player);
        int offset = input.x + input.y;

        if (offset == 0)
        {
            Input.Offset = 0;
        }
        else if (offset != Input.Offset)
        {
            Input.Offset = offset;

            CLLogger.LogDebug($"Received input! {Input.Offset}");

            SelectNextCreature(Input.Offset, Target);
        }
    }

    private static float GetDistanceToPlayer(Creature creature, Player player) =>
        UnityEngine.Vector2.Distance(creature.mainBodyChunk.pos, player.mainBodyChunk.pos);

    private static WeakList<Creature> GetSortedCreatures(Player player)
    {
        if (player.room is null)
        {
            CLLogger.LogWarning("Player is not in a room; No creature can be queried.");
            return [];
        }

        List<Creature> sortedCreatures = [.. player.room.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => !PossessionManager.IsBannedPossessionTarget(c) && IsInPossessionRange(player, c))
        ];

        if (sortedCreatures is null or { Count: 0 })
        {
            CLLogger.LogWarning("Failed to retrieve any valid creature in the room!");
            return [];
        }

        sortedCreatures.Sort((c1, c2) =>
        {
            if (c1 is Fly && c2 is not Fly) return 1;

            float dist1 = GetDistanceToPlayer(c1, player);
            float dist2 = GetDistanceToPlayer(c2, player);

            return dist1 < dist2
                ? -1
                : dist1 == dist2
                    ? 0
                    : 1;
        });

        CLLogger.LogDebug($"Sorted crits: {PossessionManager.FormatPossessions(sortedCreatures)}");

        return [.. sortedCreatures];
    }

    private static bool IsInPossessionRange(Player player, Creature creature) =>
        Custom.DistLess(player.mainBodyChunk.pos, creature.mainBodyChunk.pos, POSSESSION_RADIUS);
}

public record class TargetInput()
{
    public bool IsActive { get; set; }
    public bool LockAction { get; set; }
    public int Offset { get; set; }
    public int InputTime { get; set; }
}