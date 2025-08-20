using System.Security.Permissions;
using BepInEx;
using ControlLib.Possession;
using ImprovedInput;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace ControlLib;

[BepInDependency("com.dual.improved-input-config")]
[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
public class ControlLibMain : BaseUnityPlugin
{
    public const string PLUGIN_NAME = "ControlLib";
    public const string PLUGIN_GUID = "ynhzrfxn.controllib";
    public const string PLUGIN_VERSION = "1.0.0";

    private bool isInitialized;

    public void OnEnable()
    {
        if (isInitialized) return;

        isInitialized = true;

        PossessionHooks.ApplyHooks();

        On.Player.Update += PlayerUpdate;

        CLLogger.CleanLogFile();

        Logger.LogInfo("Enabled ControlLib successfully.");
    }

    public void OnDisable()
    {
        if (!isInitialized) return;

        isInitialized = false;

        PossessionHooks.RemoveHooks();

        On.Player.Update -= PlayerUpdate;

        Logger.LogInfo("Disabled ControlLib successfully.");
    }

    private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig.Invoke(self, eu);

        PossessionManager manager = self.GetPossessionManager();

        if (manager.CanPossessCreature()
            && PlayerKeybind.Special.CheckRawPressed(self.playerState.playerNumber))
        {
            CLLogger.LogDebug("Checking creatures!");

            self.room.abstractRoom.creatures.ForEach(crit =>
            {
                if (manager.IsPossessing) return;

                if (manager.CanPossessCreature(crit.realizedCreature) && Random.value < 0.33)
                {
                    try
                    {
                        manager.StartPossession(crit.realizedCreature);
                        CLLogger.LogInfo($"{self} has possessed {crit}!");
                    }
                    catch (System.Exception ex)
                    {
                        CLLogger.LogError($"Failed to possess {crit}!", ex);
                    }
                }

                CLLogger.LogInfo($"Skipping: {crit}");
            });
        }
        else if (manager.IsPossessing && manager.PossessionCooldown == 0
            && PlayerKeybind.Special.CheckRawPressed(self.playerState.playerNumber))
        {
            CLLogger.LogInfo($"Removing all possessions of {self}!");

            manager.ResetAllPossessions();
        }
    }
}