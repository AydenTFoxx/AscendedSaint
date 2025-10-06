using AscendedSaint.Attunement;
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

        IL.Player.ClassMechanicsSaint += Extras.WrapILHook(SaintMechanicsHooks.AscensionMechanicsILHook);
    }

    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        IL.Player.ClassMechanicsSaint -= Extras.WrapILHook(SaintMechanicsHooks.AscensionMechanicsILHook);
    }

    protected override void UpdateHook(On.RainWorld.orig_Update orig, RainWorld self)
    {
        base.UpdateHook(orig, self);

        SaintMechanicsHooks.UpdateCooldowns();
    }
}