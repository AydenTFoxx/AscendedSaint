using RainMeadow;

namespace AscendedSaint.Ascension
{
    /// <summary>
    /// A Meadow-compatible wrapper for the <c>ASUtils</c> class.
    /// </summary>
    public class ASUtilsMeadow : ASUtils
    {
        /// <summary>
        /// Ascends or returns an <c>OnlinePhysicalObject</c> back from life, depending on whether it was dead beforehand.
        /// </summary>
        /// <param name="onlinePhysicalObject">An <c>OnlinePhysicalObject</c> to be ascended or revived.</param>
        public static void AscendCreature(object onlinePhysicalObject)
        {
            OnlinePhysicalObject onlineObject = CastToModdedType<OnlinePhysicalObject>(onlinePhysicalObject);
            PhysicalObject physicalObject = onlineObject?.apo.realizedObject;

            if (onlineObject == default(OnlinePhysicalObject) || physicalObject == null) return;

            float revivalHealthFactor = ASOptions.REVIVAL_HEALTH_FACTOR.Value * 0.01f;

            if (ASUtils.AscendCreature(physicalObject) && onlineObject is OnlineCreature onlineCreature)
            {
                ReviveCreature(onlineCreature, revivalHealthFactor);
            }
        }

        /// <summary>
        /// Restores an <c>OnlineCreature</c>'s health and sets its state as "alive" once again.
        /// </summary>
        /// <param name="onlineCreature">The creature to be revived.</param>
        /// <param name="health">The health to be restored for the newly revived creature. Slugcats ignore this setting and are always restored to full health instead.</param>
        private static void ReviveCreature(object onlineCreature, float health = 1f)
        {
            OnlineCreature online = CastToModdedType<OnlineCreature>(onlineCreature);

            if (online == default(OnlineCreature)) return;

            ASUtils.ReviveCreature(online.realizedCreature, health);
        }
    }
}