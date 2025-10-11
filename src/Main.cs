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

    private static readonly Options options = new();

    public Main()
        : base(options)
    {
        Registry.RegisterMod(this, typeof(Options));

        ModLib.Logger.CleanLogFile();
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

        SaintMechanicsHooks.AddHooks();
    }

    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        SaintMechanicsHooks.RemoveHooks();
    }

    protected override void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        base.GameUpdateHook(orig, self);

        AscensionHandler.UpdateCooldowns();
    }
}