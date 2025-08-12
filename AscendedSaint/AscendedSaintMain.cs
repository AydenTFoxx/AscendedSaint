using System.Security.Permissions;
using System.Linq;
using BepInEx;
using AscendedSaint.Attunement;

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

        public static ASOptions.ClientOptions ClientOptions { get; set; }

        public void OnEnable()
        {
            if (isInitialized) return;

            isInitialized = true;

            options = new ASOptions();
            ClientOptions = new ASOptions.ClientOptions();

            On.RainWorld.OnModsInit += OnModsInitHook;
            On.RainWorld.PostModsInit += PostModsInitHook;

            On.Player.ClassMechanicsSaint += SaintMechanicsHooks.ClassMechanicsSaintHook;
        }

        public void OnDisable()
        {
            if (!isInitialized) return;

            isInitialized = false;

            On.Player.ClassMechanicsSaint -= SaintMechanicsHooks.ClassMechanicsSaintHook;

            On.RainWorld.OnModsInit -= OnModsInitHook;
            On.RainWorld.PostModsInit -= PostModsInitHook;

            On.GameSession.ctor -= DefaultGameSessionHook;

            if (Utils.IsMeadowEnabled())
            {
                ASMeadowUtils.RemoveMeadowHooks();
            }
        }

        private void OnModsInitHook(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);

            try
            {
                MachineConnector.SetRegisteredOI(PLUGIN_GUID, options);
            }
            catch (System.Exception ex)
            {
                ASLogger.LogError("Failed to register the mod's REMIX options!", ex);
            }
        }

        private void PostModsInitHook(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            ASLogger.CleanLogFile();

            ASLogger.LogInfo("Initialized Ascended Saint mod successfully.");

            if (Utils.IsMeadowEnabled())
            {
                ASLogger.LogInfo("Meadow is enabled! Applying hooks...");

                ASMeadowUtils.ApplyMeadowHooks();
            }

            On.GameSession.ctor += DefaultGameSessionHook;

            orig.Invoke(self);
        }

        internal static void DefaultGameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
        {
            orig.Invoke(self, game);

            ClientOptions.RefreshOptions();

            ASLogger.LogDebug($"Client options are: {ClientOptions.ToString()}");
        }

        /// <summary>
        /// Utility functions used throughout the Ascended Saint mod.
        /// </summary>
        internal static class Utils
        {
            private static bool isMeadowEnabled = false;
            private static bool cachedMeadowCheck = false;

            /// <summary>
            /// Determines whether the Rain Meadow mod is enabled.
            /// </summary>
            /// <returns><c>true</c> if Rain Meadow is enabled, <c>false</c> otherwise.</returns>
            /// <remarks>Note this only checks for the mod's presence, not if the player is in an online lobby.</remarks>
            public static bool IsMeadowEnabled()
            {
                if (!cachedMeadowCheck)
                {
                    isMeadowEnabled = ModManager.ActiveMods.Any(mod => mod.id == RAIN_MEADOW_ID);
                    cachedMeadowCheck = true;
                }

                return isMeadowEnabled;
            }
        }
    }
}