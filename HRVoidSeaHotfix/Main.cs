using System.Security.Permissions;
using BepInEx;
using BepInEx.Logging;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace HRVoidSeaHotfix;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class Main : BaseUnityPlugin
{
    public const string MOD_GUID = "ynhzrfxn.hr_voidsea_hotfix";
    public const string MOD_NAME = "Saint Void Sea Hotfix";
    public const string MOD_VERSION = "1.0";

    internal static new ManualLogSource? Logger;

    public Main()
    {
        Logger = base.Logger;
    }

    public void OnEnable() => Patches.AddHooks();

    public void OnDisable() => Patches.RemoveHooks();
}