using System.Security.Permissions;
using AscendedSaint.Attunement;
using BepInEx;
using ModLib;
using ModLib.Logging;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace AscendedSaint;

[BepInPlugin(MOD_GUID, MOD_NAME, MOD_VERSION)]
public sealed class Main : ModPlugin
{
    public const string MOD_GUID = "ynhzrfxn.ascendedsaint";
    public const string MOD_NAME = "Ascended Saint";
    public const string MOD_VERSION = "2.0.1";

#nullable disable warnings

    public static new ModLogger Logger { get; private set; }

#nullable restore warnings

    public Main() : base(new Options())
    {
        Logger = base.Logger;
    }

    protected override void ApplyHooks()
    {
        base.ApplyHooks();

        Hooks.ApplyHooks();

        On.GameSession.ctor += GameSessionHook;
        On.RainWorldGame.Update += GameUpdateHook;
    }

    protected override void RemoveHooks()
    {
        base.RemoveHooks();

        Hooks.RemoveHooks();

        On.GameSession.ctor -= GameSessionHook;
        On.RainWorldGame.Update -= GameUpdateHook;
    }

    private static void GameSessionHook(On.GameSession.orig_ctor orig, GameSession self, RainWorldGame game)
    {
        if (!Extras.InGameSession)
            AscensionHandler.InitAscensionImpl();

        orig.Invoke(self, game);
    }

    private static void GameUpdateHook(On.RainWorldGame.orig_Update orig, RainWorldGame self)
    {
        orig.Invoke(self);

        AscensionHandler.UpdateCooldowns();
    }
}