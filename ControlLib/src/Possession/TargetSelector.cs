using System.Collections.Generic;
using System.Linq;
using ControlLib.Utils;
using MoreSlugcats;
using RWCustom;

namespace ControlLib.Possession;

public sealed class TargetSelector(Player player, PossessionManager manager)
{
    private const float POSSESSION_RADIUS = 480f;

    private WeakList<Creature> creatures;

    public Creature Target { get; private set; }
    public TargetInput Input { get; set; } = new();

    public PossessionManager GetManager() => manager;
    public Player GetPlayer() => player;

    public bool IsTargetValid() => Target is not null && manager.CanPossessCreature(Target);
    public void SetTargetPossession() => manager.StartPossession(Target);
    public void ResetTargetPossession() => manager.StopPossession(Target);

    public void Update()
    {
        if (Input.LockAction) return;

        if (manager.IsPossessing)
        {
            ResetTargetPossession();

            Input.LockAction = true;
            return;
        }
        else if (manager.PossessionCooldown > 0)
        {
            Input.LockAction = true;
            return;
        }

        if (UnityEngine.Random.value < 0.5)
        {
            Target?.room.AddObject(new VoidParticle(Target.mainBodyChunk.pos, Custom.RNV() * 0.5f, 32f));
        }

        player.mushroomCounter = 20;

        if (!Input.IsActive)
        {
            SelectNextCreature(1);
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

            SelectNextCreature(Input.Offset);
        }
    }

    public void ConfirmSelection()
    {
        Input = new();
        creatures.Clear();

        if (Target is null)
        {
            CLLogger.LogWarning("Tried to possess an invalid creature, ignoring.");
        }
        else
        {
            SetTargetPossession();
        }
    }

    public void SelectNextCreature(int offset)
    {
        Input.IsActive = true;

        if (creatures is null or { Count: 0 })
        {
            creatures = GetSortedCreatures(player);
        }

        if (TrySelectNextCreature(lastTarget: Target, offset))
        {
            CLLogger.LogInfo($"Selected creature: {Target}");
        }
    }

    internal bool TrySelectNextCreature(Creature lastTarget = null, int offset = 0)
    {
        if (creatures.Count == 0) return false;

        int i = lastTarget is not null ? creatures.IndexOf(lastTarget) + offset : offset;

        Target = creatures.ElementAtOrDefault(i);
        Target ??= (offset < 0) ? creatures.First() : creatures.Last();

        if (!IsTargetValid())
        {
            CLLogger.LogWarning($"{Target} is not a valid possession target.");

            creatures.Remove(Target);

            Target = null;
        }

        return Target is not null;
    }

    private static float GetDistanceToPosition(Creature creature, Player player) =>
        (creature.mainBodyChunk.pos - player.mainBodyChunk.pos).magnitude;

    private static WeakList<Creature> GetSortedCreatures(Player player)
    {
        List<Creature> sortedCreatures = [.. player.room?.abstractRoom.creatures
            .Select(ac => ac.realizedCreature)
            .Where(c => c is not (null or Player or { dead: true }) && IsInPossessionRange(player, c))
        ];

        if (sortedCreatures is null or { Count: 0 })
        {
            CLLogger.LogWarning("Failed to retrieve any valid creature in the room!");
            return [];
        }

        sortedCreatures.Sort((c1, c2) =>
        {
            float dist1 = GetDistanceToPosition(c1, player);
            float dist2 = GetDistanceToPosition(c2, player);

            return dist1 < dist2
                ? 1
                : dist1 == dist2
                    ? 0
                    : -1;
        });

        return [.. sortedCreatures];
    }

    private static bool IsInPossessionRange(Player player, Creature creature) =>
        Custom.DistLess(player.mainBodyChunk.pos, creature.mainBodyChunk.pos, POSSESSION_RADIUS);
}

public record class TargetInput(bool IsActive, bool LockAction, int Offset)
{
    public TargetInput()
        : this(false, false, 0)
    {
    }

    public bool IsActive { get; set; } = IsActive;
    public bool LockAction { get; set; } = LockAction;
    public int Offset { get; set; } = Offset;
}