using System.Security.Permissions;
using BepInEx;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace ControlLib;

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

        Logger.LogInfo("Mod loaded successfully.");
    }
}
