using System.Collections.Generic;
using System.Security.Permissions;
using BepInEx;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace AscendedSaint
{
    [BepInDependency("henpemaz_rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class AscendedSaintMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "ynhzrfxn.ascendedsaint";
        public const string PLUGIN_NAME = "Ascended Saint";
        public const string PLUGIN_VERSION = "1.1.0";

        private static bool isInitialized = false;
        private static ASOptions options;

        private const float KARMIC_BURST_RADIUS = 40f;

        public void OnEnable()
        {
            if (isInitialized) return;

            isInitialized = true;
            options = new ASOptions();

            On.Player.ClassMechanicsSaint += ClassMechanicsSaintHook;
            On.RainWorld.OnModsInit += OnModsInitHook;
        }

        public void OnDisable()
        {
            if (!isInitialized) return;

            isInitialized = false;

            On.Player.ClassMechanicsSaint -= ClassMechanicsSaintHook;
            On.RainWorld.OnModsInit -= OnModsInitHook;
        }

        public void ClassMechanicsSaintHook(On.Player.orig_ClassMechanicsSaint orig, Player self)
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
                        if (!Custom.DistLess(bodyChunk.pos, vector2, KARMIC_BURST_RADIUS + bodyChunk.rad) ||
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

        public void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);

            try
            {
                MachineConnector.SetRegisteredOI(PLUGIN_GUID, options);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
}