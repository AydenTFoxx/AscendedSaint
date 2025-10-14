using ImprovedInput;

namespace ModLib.Input;

internal static class ImprovedInputHelper
{
    public static bool IsKeyDown(Player player, Keybind keybind) => IsKeyDown(player, (PlayerKeybind)keybind);
    public static bool IsKeyDown(Player player, PlayerKeybind playerKeybind) => player.IsPressed(playerKeybind);

    public static bool WasKeyJustPressed(Player player, Keybind keybind) => WasKeyJustPressed(player, (PlayerKeybind)keybind);
    public static bool WasKeyJustPressed(Player player, PlayerKeybind playerKeybind) => player.JustPressed(playerKeybind);

    public static void RegisterKeybind(Keybind keybind)
    {
        if (PlayerKeybind.Get(keybind.Id) is not null) return;

        PlayerKeybind.Register(keybind.Id, keybind.Mod, keybind.Name, keybind.KeyboardPreset, keybind.GamepadPreset, keybind.XboxPreset);
    }
}