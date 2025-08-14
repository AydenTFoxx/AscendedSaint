using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static AscendedSaint.AscendedSaintMain;

namespace AscendedSaint.Attunement
{
    /// <summary>
    /// All hooks relating to Saint's unique abilities.
    /// </summary>
    public static class SaintMechanicsHooks
    {
        private const float KARMIC_BURST_RADIUS = 60f;

        public static void AscensionMechanicsHook(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            if (self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Saint ||
                !self.monkAscension || (self.killFac + 0.025f) < 1f)
            {
                orig.Invoke(self);
                return;
            }

            Vector2 vector2 = new Vector2(self.mainBodyChunk.pos.x + self.burstX,
                self.mainBodyChunk.pos.y + self.burstY + 60f);

            foreach (List<PhysicalObject> objects in self.room.physicalObjects)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    PhysicalObject physicalObject = objects[i];
                    bool shouldAscendCreature = false;

                    if (!(physicalObject is Creature || physicalObject is Oracle)) continue;

                    foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
                    {
                        if (!Custom.DistLess(bodyChunk.pos, vector2, KARMIC_BURST_RADIUS + bodyChunk.rad) ||
                            !self.room.VisualContact(bodyChunk.pos, vector2)) continue;

                        shouldAscendCreature = true;

                        if (physicalObject == self)
                            bodyChunk.vel += Custom.RNV() * 36f;
                        else break;
                    }

                    if (!shouldAscendCreature) continue;

                    ApplyNewSaintMechanics(self, physicalObject);
                }
            }

            orig.Invoke(self);
        }

        public static void ApplyNewSaintMechanics(Player self, PhysicalObject physicalObject)
        {
            if (!(physicalObject is Creature || physicalObject is Oracle)) return;

            if (ASUtils.CanReviveCreature(physicalObject) && ClientOptions.allowRevival)
            {
                ASLogger.LogDebug("Attempting to revive: " + physicalObject);

                if (ClientOptions.requireKarmaFlower)
                {
                    PhysicalObject karmaFlower = ASUtils.GetHeldKarmaFlower(self);

                    if (karmaFlower is null)
                    {
                        ASLogger.LogDebug("Player has no Karma Flower, ignoring.");
                        return;
                    }

                    karmaFlower.Destroy();
                }

                self.killFac = 0f;

                ASUtils.AscendCreature(physicalObject, self);

                self.DeactivateAscension();
                self.SaintStagger(80);
            }
            else if (physicalObject == self && ClientOptions.allowSelfAscension)
            {
                ASLogger.LogDebug("Attempting to ascend: " + self.SlugCatClass);

                ASUtils.AscendCreature(physicalObject as Player, self);

                self.voidSceneTimer = 1;
            }
        }
    }
}