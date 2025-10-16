using System.IO;
using System.Security.Permissions;
using AscendedSaint.Attunement;
using AscendedSaint.Meadow;
using BepInEx;
using ModLib;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


namespace AscendedSaint;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public class Main : ModPlugin
{
    public const string MOD_GUID = "ynhzrfxn.ascendedsaint";
    public const string MOD_NAME = "Ascended Saint";
    public const string MOD_VERSION = "2.0.0";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public static new LogUtils.Logger Logger { get; private set; }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public Main() : base(new Options())
    {
        Logger = base.Logger;

        string path = Registry.MyMod.LogID?.Properties.CurrentFilePath ?? Path.Combine(Registry.DefaultLogsPath, "AscendedSaint.log");

        if (File.Exists(path))
        {
            File.WriteAllText(path, "");
        }
    }

    public override void OnEnable()
    {
        if (IsModEnabled) return;

        base.OnEnable();

        AscensionHandler.AscensionImpl = Extras.IsMeadowEnabled
            ? new MeadowAscensionImpl()
            : new VanillaAscensionImpl();
    }

    public override void OnDisable()
    {
        if (!IsModEnabled) return;

        base.OnDisable();

        AscensionHandler.AscensionImpl = null;
    }

    protected override void ApplyHooks()
    {
        base.ApplyHooks();

        On.RainWorldGame.Update += GameUpdateHook;

        SaintMechanicsHooks.AddHooks();
    }

    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        On.RainWorldGame.Update -= GameUpdateHook;

        SaintMechanicsHooks.RemoveHooks();
    }

    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        AscensionHandler.UpdateCooldowns();
    }
}