using AscendedSaint.Attunement;
//using AscendedSaint.Endings;
using BepInEx;
using ModLib;

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

    protected override void ApplyHooks()
    {
        base.ApplyHooks();

        SaintMechanicsHooks.AddHooks();

        //VoidSeaHooks.AddHooks();
    }

    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        SaintMechanicsHooks.RemoveHooks();

        //VoidSeaHooks.RemoveHooks();
    }

    protected override void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        base.GameUpdateHook(orig, self);

        SaintMechanicsHooks.UpdateCooldowns();
    }
}