using System.Security.Permissions;
using System.Linq;
using BepInEx;
using UnityEngine;
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

        public static AscendedSaintMain instance;
        public ASOptions.SharedOptions clientOptions;

        public void OnEnable()
        {
            if (isInitialized) return;

            isInitialized = true;
            instance = this;

            options = new ASOptions();
            clientOptions = new ASOptions.SharedOptions();

            On.RainWorld.OnModsInit += OnModsInitHook;
            On.Player.ClassMechanicsSaint += SaintMechanicsHooks.ClassMechanicsSaintHook;

            if (Utils.IsMeadowEnabled())
            {
                ASMeadowUtils.ApplyMeadowHooks();
            }
        }

        public void OnDisable()
        {
            if (!isInitialized) return;

            isInitialized = false;

            On.Player.ClassMechanicsSaint -= SaintMechanicsHooks.ClassMechanicsSaintHook;
            On.RainWorld.OnModsInit -= OnModsInitHook;
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

        /// <summary>
        /// Utility functions used throughout the Ascended Saint mod.
        /// </summary>
        internal static class Utils
        {
            /// <summary>
            /// Determines whether the Rain Meadow mod is enabled.
            /// </summary>
            /// <returns><c>true</c> if Rain Meadow is enabled, <c>false</c> otherwise.</returns>
            /// <remarks>Note this only checks for the mod's presence, not if the player is in an online lobby.</remarks>
            /// <seealso cref="IsMultiplayerSession"/>
            public static bool IsMeadowEnabled()
            {
                return ModManager.ActiveMods.Any(mod => mod.id == RAIN_MEADOW_ID);
            }

            /// <summary>
            /// Determines if the player is currently in a lobby within Rain Meadow.
            /// </summary>
            /// <returns><c>true</c> if the player is in a lobby, <c>false</c> otherwise.</returns>
            public static bool IsOnlineMultiplayerSession()
            {
                return IsMeadowEnabled() && RainMeadow.OnlineManager.lobby != null;
            }

            /// <summary>
            /// Attempts to cast a given object to a type from another mod, without breaking this one.
            /// </summary>
            /// <typeparam name="T">The external type to be returned.</typeparam>
            /// <param name="obj">The object to be casted to <paramref name="obj"/>.</param>
            /// <returns>Either <paramref name="obj"/> cast to <typeparamref name="T"/>, or <typeparamref name="T"/>'s default value if the conversion fails.</returns>
            public static T CastToModdedType<T>(object obj)
            {
                T result = default;

                try
                {
                    result = (T)obj;
                }
                catch (System.InvalidCastException ex)
                {
                    Debug.LogError($"{nameof(obj)} should be a boxed {nameof(T)} object.");
                    Debug.LogError(ex);
                }

                return result;
            }
        }
    }
}