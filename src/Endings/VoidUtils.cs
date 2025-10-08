using ModLib.Options;

namespace AscendedSaint.Endings;

/// <summary>
/// Utility functions for managing the mod's custom ascension endings.
/// </summary>
public static class VoidUtils
{
    /// <summary>
    /// Whether or not the player has ascended Looks to the Moon in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if this Iterator was ascended by the player, <c>false</c> otherwise.</returns>
    public static bool DidAscendMoon(GameSession? gameSession) => gameSession is StoryGameSession storySession && storySession.saveState.deathPersistentSaveData.ripMoon;

    /// <summary>
    /// Whether or not the player has ascended Five Pebbles in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if this Iterator was ascended by the player, <c>false</c> otherwise.</returns>
    public static bool DidAscendPebbles(GameSession? gameSession) => gameSession is StoryGameSession storySession && storySession.saveState.deathPersistentSaveData.ripPebbles;

    /// <summary>
    /// Determines if exactly one Iterator was ascended in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if only one Iterator is recorded to be ascended, <c>false</c> otherwise.</returns>
    /// <remarks>No further records of Iterator ascension are kept beyond what is in the vanilla game. Reviving an Iterator will count as not having ascended it at all.</remarks>
    public static bool AscendedOneIterator(GameSession? gameSession) => DidAscendMoon(gameSession) != DidAscendPebbles(gameSession);

    /// <summary>
    /// Determines if both Iterator were ascended in the given game session.
    /// </summary>
    /// <param name="gameSession">The game session to be tested.</param>
    /// <returns><c>true</c> if both Iterators are recorded to be ascended, <c>false</c> otherwise.</returns>
    /// <remarks>No further records of Iterator ascension are kept beyond what is in the vanilla game. Reviving an Iterator will count as not having ascended it at all.</remarks>
    public static bool AscendedAllIterators(GameSession? gameSession) => DidAscendMoon(gameSession) && DidAscendPebbles(gameSession);

    /// <summary>
    /// Determines if Saint's ascension ending should be overriden by this mod.
    /// </summary>
    /// <param name="player">The player to be tested.</param>
    /// <returns><c>true</c> if the mod's alternate endings should play, <c>false</c> otherwise.</returns>
    public static bool ShouldPlayAlternateEnding(Player? player) =>
        Region.IsRubiconRegion(player?.room?.world.region.name ?? "")
            && !AscendedOneIterator(player?.room?.game.session)
            && OptionUtils.IsClientOptionEnabled(Options.DYNAMIC_ENDINGS);
}