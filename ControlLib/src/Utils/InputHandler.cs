using System.Collections.Generic;
using UnityEngine;
using static ControlLib.Utils.CompatibilityManager;

namespace ControlLib.Utils;

public static class InputHandler
{
    public static readonly List<Keybind> Keybinds = [];

    public static Keybind GetKeybind(string id) => Keybinds.Find(k => k.ID == id);

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
        public static Keybind POSSESSION_KEY { get; private set; }

        public static void RegisterKeybinds()
        {
            try
            {
                POSSESSION_KEY = RegisterKeybind("possess", "Possess", KeyCode.C, KeyCode.Joystick1Button0);

                CLLogger.LogInfo("Successfully registered all keybinds.");
            }
            catch (System.Exception ex)
            {
                CLLogger.LogError("Failed to register keybinds!", ex);
            }
        }
    }
}

public struct Keybind(string id, string name, KeyCode keyboardKey, KeyCode gamepadKey)
{
    public string ID { get; private set; } = $"controllib:{id}";
    public string Name { get; private set; } = name;
    public KeyCode KeyboardKey { get; private set; } = keyboardKey;
    public KeyCode GamepadKey { get; private set; } = gamepadKey;

    public override readonly string ToString() => $"{nameof(Keybind)}: {Name} [{ID}; {KeyboardKey}|{GamepadKey}]";
}