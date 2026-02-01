using System;
using System.Linq;
using ImprovedInput;
using UnityEngine;

namespace ControlLib.Utils;

internal static class ImprovedInputHandler
{
    public static PlayerKeybind GetPlayerKeybind(Keybind keybind, bool isRecursive = false)
    {
        PlayerKeybind playerKeybind = PlayerKeybind.Get(keybind.ID);

        if (playerKeybind is not null) return playerKeybind;

        if (isRecursive)
        {
            CLLogger.LogWarning("Failed to register PlayerKeybind; Disabling IIC compatibility layer.");

            CompatibilityManager.ToggleModCompatibility("improved-input-config", false);

            return PlayerKeybind.Special;
        }
        else
        {
            CLLogger.LogWarning($"Could not find PlayerKeybind {keybind.ID}; Attempting to register it to IIC...");

            RegisterPlayerKeybind(keybind);

            return GetPlayerKeybind(keybind, isRecursive: true);
        }
    }

    public static bool IsKeyPressed(Player self, Keybind keybind) => IsKeyPressed(self, GetPlayerKeybind(keybind));

    public static bool IsKeyPressed(Player self, PlayerKeybind playerKeybind) => self.RawInput()[playerKeybind];

    public static void RegisterPlayerKeybind(Keybind keybind) =>
        RegisterPlayerKeybind(keybind.ID, keybind.Name, keybind.KeyboardKey, keybind.GamepadKey);

    public static void RegisterPlayerKeybind(string id, string name, KeyCode keyboardKey, KeyCode gamepadKey)
    {
        try
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
        catch (Exception ex)
        {
            CLLogger.LogError($"Failed to register PlayerKeybind: {id}!", ex);
        }
    }
}