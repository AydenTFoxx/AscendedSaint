using MoreSlugcats;
using RainMeadow;
using RWCustom;
using UnityEngine;

namespace AscendedSaint.Ascension
{
    /// <summary>
    /// The Meadow-compatible implementation of Saint's new abilities. Does not require the mod to be enabled on the host's or other players' ends to work.
    /// </summary>
    /// <remarks>This is automatically enabled if the Rain Meadow mod is enabled.</remarks>
    /// <seealso cref="VanillaSaintMechanicsHook"/>
    public class MeadowSaintMechanicsHook : ASUtils.SaintMechanicsHook
    {
        public override void ClassMechanicsSaintHook(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            if (self.SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Saint ||
                !self.monkAscension || (self.killFac + 0.025f) < 1f)
            {
                orig.Invoke(self);
                return;
            }

            WorldSession worldSession = null;

            foreach (WorldSession session in OnlineManager.lobby.worldSessions.Values)
            {
                if (session.world == self.room.world)
                {
                    worldSession = session;
                    break;
                }
            }

            if (worldSession == null) return;

            bool didAscendPlayer = false;
            Vector2 vector2 = new Vector2(self.mainBodyChunk.pos.x + self.burstX,
                self.mainBodyChunk.pos.y + self.burstY + 60f);

            foreach (OnlineEntity entity in worldSession.activeEntities)
            {
                if (didAscendPlayer) break;

                if (!(entity is OnlinePhysicalObject onlineObject)) continue;

                bool shouldAscendCreature = false;
                PhysicalObject physicalObject = onlineObject.apo.realizedObject;

                if (physicalObject == null) continue;

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
                    Debug.Log("[ASM] Attempting to revive: " + physicalObject);

                    if (ASOptions.REQUIRE_KARMA_FLOWER.Value)
                    {
                        PhysicalObject karmaFlower = ASUtils.GetHeldKarmaFlower(self);

                        if (karmaFlower == null) continue;

                        karmaFlower.Destroy();
                    }

                    orig.Invoke(self);

                    ASUtilsMeadow.AscendCreature(onlineObject);

                    self.DeactivateAscension();
                    self.SaintStagger(80);

                    return;
                }
                else if (physicalObject == self && ASOptions.ALLOW_SELF_ASCENSION.Value)
                {
                    Debug.Log("[ASM] Attempting to ascend: " + self.SlugCatClass);

                    ASUtilsMeadow.AscendCreature(onlineObject);

                    didAscendPlayer = true;
                }
            }

            if (didAscendPlayer) self.voidSceneTimer = 1;

            orig.Invoke(self);

            if (didAscendPlayer) self.voidSceneTimer = 0;
        }
    }
}