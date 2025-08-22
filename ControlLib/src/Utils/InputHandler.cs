using System.Collections.Generic;
using UnityEngine;
using static ControlLib.Utils.CompatibilityManager;

namespace ControlLib.Utils;

public static class InputHandler
{
    public static readonly List<Keybind> Keybinds = [];

    public static Keybind GetKeybind(string id) => Keybinds.Find(k => k.ID == id);

    public static Player.InputPackage GetVanillaInput(Player self) =>
        RWInput.PlayerInput(self.playerState.playerNumber);

    public static bool IsKeyPressed(Player self, Keybind keybind) =>
        IsIICEnabled()
        ? ImprovedInputHandler.IsKeyPressed(self, keybind)
        : IsKeyPressed(self, keybind.KeyboardKey, keybind.GamepadKey);

    public static bool IsKeyPressed(Player self, KeyCode keyboardKey, KeyCode gamepadKey) =>
        self.input[0].gamePad
            ? Input.GetKey(gamepadKey)
            : Input.GetKey(keyboardKey);

    public static Keybind RegisterKeybind(string id, string name, KeyCode keyboardKey, KeyCode gamepadKey)
    {
        Keybind keybind = new(id, name, keyboardKey, gamepadKey);

        if (Keybinds.Contains(keybind))
        {
            CLLogger.LogWarning($"Tried to register an existing keybind: {keybind.ID}");
        }
        else
        {
            Keybinds.Add(keybind);

            if (IsIICEnabled())
            {
                ImprovedInputHandler.RegisterPlayerKeybind(keybind);
            }

            CLLogger.LogInfo($"Registered new keybind! {keybind}");
        }

        return keybind;
    }

    public static class Keys
    {
        public static Keybind POSSESS { get; private set; }

        public static void RegisterKeybinds()
        {
            try
            {
                POSSESS = RegisterKeybind("possess", "Possess", KeyCode.C, KeyCode.Joystick1Button0);

                CLLogger.LogInfo("Successfully registered all keybinds.");
            }
            catch (System.Exception ex)
            {
                CLLogger.LogError("Failed to register keybinds!", ex);
            }
        }
    }
}

public record class Keybind(string ID, string Name, KeyCode KeyboardKey, KeyCode GamepadKey)
{
    public string ID { get; } = $"controllib:{ID}";
    public string Name { get; } = Name;
    public KeyCode KeyboardKey { get; } = KeyboardKey;
    public KeyCode GamepadKey { get; } = GamepadKey;

    public override string ToString() => $"{nameof(Keybind)}: {Name} [{ID}; {KeyboardKey}|{GamepadKey}]";
}