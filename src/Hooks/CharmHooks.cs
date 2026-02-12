using AscendedSaint.Healing;

namespace AscendedSaint.Hooks;

public static class CharmHooks
{
    private static readonly CreatureTemplate.Relationship CharmRelationship = new(CreatureTemplate.Relationship.Type.Pack, 1f);

    public static void ApplyHooks()
    {
        On.ArtificialIntelligence.DynamicRelationship_CreatureRepresentation_AbstractCreature += ForceDynamicCharmRelationshipHook;
        On.ArtificialIntelligence.StaticRelationship += ForceStaticCharmRelationshipHook;

        On.Creature.RippleViolenceCheck += PreventCharmedViolenceHook;
    }

    public static void RemoveHooks()
    {
        On.ArtificialIntelligence.DynamicRelationship_CreatureRepresentation_AbstractCreature -= ForceDynamicCharmRelationshipHook;
        On.ArtificialIntelligence.StaticRelationship -= ForceStaticCharmRelationshipHook;

        On.Creature.RippleViolenceCheck -= PreventCharmedViolenceHook;
    }

    private static bool PreventCharmedViolenceHook(On.Creature.orig_RippleViolenceCheck orig, Creature self, BodyChunk source) =>
        (source.owner is not Creature crit || !self.abstractCreature.IsCharmedBy(crit.abstractCreature)) && orig.Invoke(self, source);

    private static CreatureTemplate.Relationship ForceDynamicCharmRelationshipHook(On.ArtificialIntelligence.orig_DynamicRelationship_CreatureRepresentation_AbstractCreature orig, ArtificialIntelligence self, Tracker.CreatureRepresentation rep, AbstractCreature absCrit) =>
        self.creature.IsCharmedBy(absCrit)
            ? CharmRelationship
            : orig.Invoke(self, rep, absCrit);

    private static CreatureTemplate.Relationship ForceStaticCharmRelationshipHook(On.ArtificialIntelligence.orig_StaticRelationship orig, ArtificialIntelligence self, AbstractCreature otherCreature) =>
        self.creature.IsCharmedBy(otherCreature)
            ? CharmRelationship
            : orig.Invoke(self, otherCreature);
}