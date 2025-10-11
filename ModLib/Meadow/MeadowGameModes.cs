using RainMeadow;

namespace ModLib.Meadow;

/// <summary>
///     Wrapper enum for representing potential <see cref="OnlineGameMode"/> types.
/// </summary>
public enum MeadowGameModes
{
    /// <summary>
    ///     The current online lobby's game mode is of type <see cref="MeadowGameMode"/>.
    /// </summary>
    Meadow,
    /// <summary>
    ///     The current online lobby's game mode is of type <see cref="StoryGameMode"/>.
    /// </summary>
    Story,
    /// <summary>
    ///     The current online lobby's game mode is of type <see cref="ArenaOnlineGameMode"/>.
    /// </summary>
    Arena,
    /// <summary>
    ///     The current online lobby's game mode is of an unknown type (possibly another mod's custom game mode).
    /// </summary>
    Custom = -1
}