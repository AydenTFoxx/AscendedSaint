using Menu;
using Menu.Remix.MixedUI;
using ModLib.Options;
using UnityEngine;

namespace AscendedSaint;

/// <summary>
/// Holds definitions and raw values of the mod's REMIX options.
/// </summary>
/// <seealso cref="ServerOptions"/>
public sealed class Options : OptionInterface
{
#nullable disable warnings

    public static Configurable<bool> ALLOW_SELF_ASCENSION;
    public static Configurable<bool> ALLOW_REVIVAL;
    public static Configurable<bool> REQUIRE_KARMA_FLOWER;
    public static Configurable<int> REVIVAL_HEALTH_FACTOR;

    public static Configurable<bool> CUSTOM_ORACLE_REVIVAL;
    public static Configurable<int> REVIVAL_STUN_DURATION;

#nullable restore warnings

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
            new ConfigurableInfo("How much of a creature's health will be restored upon revival. For slugcats, this is always 100."));

        REVIVAL_STUN_DURATION = config.Bind(
            "revival_stun_duration",
            100,
            new ConfigurableInfo(
                "How long a creature will be stunned for after being revived. For slugcats and iterators, this value is halved.",
                new ConfigAcceptableRange<int>(0, 200)
            )
        );

        CUSTOM_ORACLE_REVIVAL = config.Bind(
            "custom_oracle_revival",
            false,
            new ConfigurableInfo("If enabled, non-vanilla Iterators can be revived with Saint's abilities. Untested and highly prone to bugs."));
    }

    public override void Initialize()
    {
        Main.Logger.LogInfo($"{nameof(Options)}: Initialized REMIX menu interface.");

        base.Initialize();

        Tabs = [
            new OptionBuilder(this, "Main Options")
                .CreateModHeader()
                .AddCheckBoxOption("Allow Self-Ascension", ALLOW_SELF_ASCENSION)
                .AddCheckBoxOption("Allow Creature Revival", ALLOW_REVIVAL)
                .AddCheckBoxOption("Require Karma Flower", REQUIRE_KARMA_FLOWER)
                .AddPadding(Vector2.up * 10f)
                .AddSliderOption("Revival Health Factor", REVIVAL_HEALTH_FACTOR)
                .AddSliderOption("Revival Stun Duration", REVIVAL_STUN_DURATION)
                .AddPadding(Vector2.up * 25f)
                .AddCheckBoxOption("Custom Iterator Revival", CUSTOM_ORACLE_REVIVAL, default, MenuColorEffect.rgbDarkRed)
                .Build()
        ];
    }
}

internal static class OptionBuilderExts
{
    public static OptionBuilder CreateModHeader(this OptionBuilder self) =>
        self.AddElements([
            new OpLabel(new Vector2(200f, 520f), new Vector2(200f, 40f), Main.MOD_NAME, bigText: true),
            new OpLabel(new Vector2(245f, 510f), new Vector2(200f, 15f), $"[v{Main.MOD_VERSION}]")
            {
                color = Color.gray
            }
        ]);
}