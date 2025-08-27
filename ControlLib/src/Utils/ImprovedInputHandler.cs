using System.Linq;
using ImprovedInput;
using UnityEngine;

namespace ControlLib.Utils;

internal static class ImprovedInputHandler
{
    public static PlayerKeybind GetPlayerKeybind(string id) => PlayerKeybind.Get(id);

    public static bool IsKeyPressed(Player self, Keybind keybind) => IsKeyPressed(self, GetPlayerKeybind(keybind.ID));

    public static bool IsKeyPressed(Player self, PlayerKeybind playerKeybind) => self.RawInput()[playerKeybind];

    public static void RegisterPlayerKeybind(Keybind keybind) =>
        RegisterPlayerKeybind(keybind.ID, keybind.Name, keybind.KeyboardKey, keybind.GamepadKey);

    public static void RegisterPlayerKeybind(string id, string name, KeyCode keyboardKey, KeyCode gamepadKey)
    {
        if (PlayerKeybind.Keybinds().Any(key => key.Id == id))
        {
            CLLogger.LogWarning($"A {nameof(PlayerKeybind)} is already registered with that ID: {id}");
        }
        else
        {
            PlayerKeybind.Register(id, ControlLibMain.PLUGIN_NAME, name, keyboardKey, gamepadKey);

            CLLogger.LogInfo($"Registered new {nameof(PlayerKeybind)}! {id}");
        }
    }
}