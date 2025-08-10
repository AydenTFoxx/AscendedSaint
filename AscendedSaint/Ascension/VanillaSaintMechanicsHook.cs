using System.Collections.Generic;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace AscendedSaint.Ascension
{
    /// <summary>
    /// The default, vanilla implementation for Saint's new abilities.
    /// </summary>
    /// <remarks>This variant is automatically overriden to ensure compatibility with supported mods.</remarks>
    /// <seealso cref="MeadowSaintMechanicsHook"/>
    public class VanillaSaintMechanicsHook : ASUtils.SaintMechanicsHook
    {
        public override void ClassMechanicsSaintHook(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            if (self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Saint ||
                !self.monkAscension || (self.killFac + 0.025f) < 1f)
            {
                orig.Invoke(self);
                return;
            }

            bool didAscendPlayer = false;
            Vector2 vector2 = new Vector2(self.mainBodyChunk.pos.x + self.burstX,
                self.mainBodyChunk.pos.y + self.burstY + 60f);

            foreach (List<PhysicalObject> objects in self.room.physicalObjects)
            {
                if (didAscendPlayer) break;

                for (int i = 0; i < objects.Count; i++)
                {
                    if (didAscendPlayer) break;

                    PhysicalObject physicalObject = objects[i];
                    bool shouldAscendCreature = false;

                    if (!(physicalObject is Creature) && !(physicalObject is Oracle)) continue;

                    foreach (BodyChunk bodyChunk in physicalObject.bodyChunks)
                    {
                        if (!Custom.DistLess(bodyChunk.pos, vector2, karmicBurstRadius + bodyChunk.rad) ||
                            !self.room.VisualContact(bodyChunk.pos, vector2)) continue;

                        shouldAscendCreature = true;

                        if (physicalObject == self)
                            bodyChunk.vel += Custom.RNV() * 36f;
                        else break;
                    }

                    if (!shouldAscendCreature) continue;

                    if (ASUtils.CanReviveCreature(physicalObject) && ASOptions.ALLOW_REVIVAL.Value)
                    {
                        Debug.Log("Attempting to revive: " + physicalObject);

                        if (ASOptions.REQUIRE_KARMA_FLOWER.Value)
                        {
                            PhysicalObject karmaFlower = ASUtils.GetHeldKarmaFlower(self);

                            if (karmaFlower == null) continue;

                            karmaFlower.Destroy();
                        }

                        orig.Invoke(self);

                        ASUtils.AscendCreature(physicalObject);

                        self.DeactivateAscension();
                        self.SaintStagger(80);

                        return;
                    }
                    else if (physicalObject == self && ASOptions.ALLOW_SELF_ASCENSION.Value)
                    {
                        Debug.Log("Attempting to ascend: " + self.SlugCatClass);

                        ASUtils.AscendCreature(physicalObject as Player);

                        didAscendPlayer = true;
                    }
                }
            }

            if (didAscendPlayer) self.voidSceneTimer = 1;

            orig.Invoke(self);

            if (didAscendPlayer) self.voidSceneTimer = 0;
        }
    }
}