using System.Security.Permissions;
using BepInEx;
using UnityEngine;
using AscendedSaint.Ascension;
using System.Linq;
using System.Collections.Generic;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace AscendedSaint
{
    [BepInDependency(RAIN_MEADOW_ID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class AscendedSaintMain : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "ynhzrfxn.ascendedsaint";
        public const string PLUGIN_NAME = "Ascended Saint";
        public const string PLUGIN_VERSION = "1.2.0";

        private const string RAIN_MEADOW_ID = "henpemaz_rainmeadow";

        private static bool isInitialized = false;

        private static ASOptions options;

        private static readonly List<ASUtils.SaintMechanicsHook> saintMechanicsHooks = new List<ASUtils.SaintMechanicsHook>
        {
            new VanillaSaintMechanicsHook(),
            new MeadowSaintMechanicsHook()
        };

        public void OnEnable()
        {
            if (isInitialized) return;

            isInitialized = true;
            options = new ASOptions();

            On.RainWorld.OnModsInit += OnModsInitHook;
            On.Player.ClassMechanicsSaint += ClassMechanicsSaintHook;
        }

        public void OnDisable()
        {
            if (!isInitialized) return;

            isInitialized = false;

            On.Player.ClassMechanicsSaint -= ClassMechanicsSaintHook;
            On.RainWorld.OnModsInit -= OnModsInitHook;
        }

        void ClassMechanicsSaintHook(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            int index = 0;

            if (IsMeadowEnabled())
            {
                if (RainMeadow.OnlineManager.lobby != null) index = 1;
            }
            
            saintMechanicsHooks[index].ClassMechanicsSaintHook(orig, self);
        }

        void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
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

        internal static bool IsMeadowEnabled()
        {
            return ModManager.ActiveMods.Any(mod => mod.id == RAIN_MEADOW_ID);
        }
    }
}