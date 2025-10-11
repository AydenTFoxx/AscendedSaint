using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    public static bool IsKeyDown(Player player, Keybind keybind) => IsKeyDown(player, (PlayerKeybind)keybind);
    public static bool IsKeyDown(Player player, PlayerKeybind playerKeybind) => player.IsPressed(playerKeybind);

    public static bool WasKeyJustPressed(Player player, Keybind keybind) => WasKeyJustPressed(player, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(Player player, PlayerKeybind playerKeybind) => player.JustPressed(playerKeybind);
}