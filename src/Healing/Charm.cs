using System.Collections.Generic;
using ModLib.Objects;
using ModLib.Collections;

namespace AscendedSaint.Healing;

/// <summary>
///     Manipulative magic which prevents its victim from attacking a given creature.
///     Can be set to last for a given amount of time (in ticks), or indefinitely.
/// </summary>
public class Charm : GlobalUpdatableAndDeletable
{
    internal static readonly WeakDictionary<AbstractCreature, List<Charm>> Instances = [];

    private readonly Creature? originalFriend;
    private readonly RelationshipTracker.DynamicRelationship? originalRelationship;

    /// <summary>
    ///     The creature who has charmed the target; Usually a player.
    /// </summary>
    public AbstractCreature Source { get; }

    /// <summary>
    ///     The creature being charmed by the source.
    /// </summary>
    public AbstractCreature Target { get; }

    /// <summary>
    ///     The duration the charm effect will last for.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    ///     Determines if the charm will last indefinitely (i.e. if duration was set to -1 at construction).
    /// </summary>
    public bool IsInfinite { get; }

    /// <summary>
    ///     Creates a new charm effect between two given creatures for a specified duration.
    /// </summary>
    /// <param name="source">The source of the charm, who will become immune to being attacked.</param>
    /// <param name="target">The target of the charm, who will be unable to attack the source.</param>
    /// <param name="duration">The duration of the charm; Set this to <c>-1</c> for an infinite duration.</param>
    public Charm(AbstractCreature source, AbstractCreature target, int duration)
    {
        Source = source;
        Target = target;
        Duration = duration;

        IsInfinite = duration == -1;

        ArtificialIntelligence? targetAI = GetCreatureAI(Target);

        if (targetAI is null) return;

        if (targetAI.friendTracker is not null)
        {
            originalFriend = targetAI.friendTracker.friend;

            targetAI.friendTracker.friend = source.realizedCreature;

            Main.Logger.LogDebug($"Overriding friendship with {target}! Was: {originalFriend}; Now: {source}");
        }

        if (targetAI.relationshipTracker is RelationshipTracker repTracker)
        {
            for (int i = 0; i < repTracker.relationships.Count; i++)
            {
                if (repTracker.relationships[i].trackerRep.representedCreature == source)
                {
                    originalRelationship = repTracker.relationships[i];

                    if (repTracker.visualize)
                        repTracker.viz.ClearSpecific(repTracker.relationships[i]);

                    repTracker.relationships.RemoveAt(i);
                }
            }

            Main.Logger.LogDebug($"{target}: Forgetting dynamic relationship with {source}; Will be restored once this charm instance is destroyed.");
        }

        Main.Logger.LogDebug($"{target} was charmed by {source}!");
    }

    public override void Update(bool eu)
    {
        base.Update(eu);

        if (Source is null or { slatedForDeletion: true } or { realizedCreature.dead: true }
            || Target is null or { slatedForDeletion: true } or { realizedCreature.dead: true })
        {
            Destroy();
            return;
        }

        if (!IsInfinite)
        {
            Duration--;

            if (Duration <= 0)
                Destroy();
        }
    }

    public override void Destroy()
    {
        base.Destroy();

        ArtificialIntelligence? targetAI = GetCreatureAI(Target);

        if (targetAI is not null)
        {
            if (originalFriend is not null)
            {
                targetAI.friendTracker.friend = originalFriend;

                Main.Logger.LogDebug($"Restored friendship of {Target} to: {originalFriend}");
            }

            if (originalRelationship is not null)
            {
                targetAI.relationshipTracker.ForgetCreatureAndStopTracking(Source);

                targetAI.relationshipTracker.relationships.Add(originalRelationship);
                targetAI.relationshipTracker.viz?.NewRel(originalRelationship);

                Main.Logger.LogDebug($"Restored relationship of {Target} with {Source}.");
            }

            if (targetAI.preyTracker?.currentPrey.critRep.representedCreature == Source)
            {
                targetAI.preyTracker.frustration *= 1.15f;

                Main.Logger.LogDebug($"Pissed off a predator ({Target}); Frustration: {targetAI.preyTracker.frustration}");
            }
        }

        Instances[Target].Remove(this);

        Main.Logger.LogDebug($"{Target} is no longer being charmed by {Source}.");
    }

    private static ArtificialIntelligence? GetCreatureAI(AbstractCreature creature)
    {
        return creature.realizedCreature switch
        {
            Lizard lizor => lizor.AI,
            Scavenger scav => scav.AI,
            _ => creature.abstractAI?.RealAI,
        };
    }
}