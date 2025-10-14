using System.Security.Permissions;
using BepInEx;
using ModLib;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace HRVoidSeaHotfix;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class Main : ModPlugin
{
    public const string MOD_GUID = "ynhzrfxn.hr_voidsea_hotfix";
    public const string MOD_NAME = "Saint Void Sea Hotfix";
    public const string MOD_VERSION = "1.0";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public static new LogUtils.Logger Logger { get; private set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public Main()
    {
        Logger = base.Logger;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        Patches.AddHooks();
    }

    public override void OnDisable()
    {
        base.OnDisable();

        Patches.RemoveHooks();
    }
}