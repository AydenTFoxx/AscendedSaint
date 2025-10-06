using Menu;
using Menu.Remix.MixedUI;
using ModLib.Options;
using UnityEngine;

namespace AscendedSaint;

/// <summary>
/// Holds definitions and raw values of the mod's REMIX options.
/// </summary>
/// <seealso cref="ServerOptions"/>
public class Options : OptionInterface
{
    public static Configurable<bool>? ALLOW_SELF_ASCENSION;
    public static Configurable<bool>? ALLOW_REVIVAL;
    public static Configurable<bool>? REQUIRE_KARMA_FLOWER;
    public static Configurable<int>? REVIVAL_HEALTH_FACTOR;
    public static Configurable<bool>? CUSTOM_ORACLE_REVIVAL;

    [ClientOption]
    public static Configurable<bool>? DYNAMIC_ENDINGS;

    public Options()
    {
        ALLOW_SELF_ASCENSION = config.Bind(
            "allow_self_ascension",
            true,
            new ConfigurableInfo("If enabled, Saint's Ascension ability can affect themselves."));

        ALLOW_REVIVAL = config.Bind(
            "allow_revival",
            true,
            new ConfigurableInfo("If enabled, using the Ascension ability on a corpse whilst holding a Karma Flower will revive it."));

        REQUIRE_KARMA_FLOWER = config.Bind(
            "require_karma_flower",
            true,
            new ConfigurableInfo("Whether or not reviving a creature will require a Karma Flower."));

        REVIVAL_HEALTH_FACTOR = config.Bind(
            "revival_health_factor",
            50,
            new ConfigurableInfo("How much of a creature's health will be restored upon revival. For Slugcats, this is always 100."));

        CUSTOM_ORACLE_REVIVAL = config.Bind(
            "custom_oracle_revival",
            false,
            new ConfigurableInfo("If enabled, Iterators other than FP and LttM can revived. Untested and prone to bugs."));

        DYNAMIC_ENDINGS = config.Bind(
            "dynamic_endings",
            true,
            new ConfigurableInfo("Enables the mod's alternate endings for Saint."));
    }

    public override void Initialize()
    {
        ModLib.Logger.LogInfo($"{nameof(Options)}: Initialized REMIX menu interface.");
        base.Initialize();

        Tabs = new OpTab[2];

        Tabs[0] = new OptionBuilder(this, "Main Options")
            .AddCheckBoxOption("Allow Self-Ascension", ALLOW_SELF_ASCENSION!)
            .AddCheckBoxOption("Allow Creature Revival", ALLOW_REVIVAL!)
            .AddCheckBoxOption("Require Karma Flower", REQUIRE_KARMA_FLOWER!)
            .AddSliderOption("Revival Health Factor", REVIVAL_HEALTH_FACTOR!)
            .Build();

        Tabs[1] = new OptionBuilder(this, "Experiments", MenuColorEffect.rgbDarkRed)
            .AddText("These options are experimental and may cause unexpected behavior. Thread with care.", new Vector2(48f, 32f), false, MenuColorEffect.rgbDarkGrey)
            .AddPadding(Vector2.up * 24f)
            .AddCheckBoxOption("Custom Iterator Revival", CUSTOM_ORACLE_REVIVAL!)
            .AddPadding(Vector2.up * 10f)
            .AddCheckBoxOption("Enable Dynamic Endings", DYNAMIC_ENDINGS!)
            .Build();
    }
}