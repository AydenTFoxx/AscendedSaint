using System;
using System.Collections.Generic;
using UnityEngine;
using static ControlLib.Utils.CompatibilityManager;

namespace ControlLib.Utils;

/// <summary>
/// General functions for retrieving player input, as well as registering keybinds.
/// </summary>
public static class InputHandler
{
    private static readonly List<Keybind> Keybinds = [];

    /// <summary>
    /// Retrieves a registered keybind by its ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The keybind registered with that ID, or <c>null</c> if none is found.</returns>
    public static Keybind GetKeybind(string id) => Keybinds.Find(k => k.ID == id);

    /// <summary>
    /// Retrieves the raw player input directly from the game itself.
    /// </summary>
    /// <param name="self">The player itself.</param>
    /// <returns>A <c>Player.InputPackage</c> with the player's inputs.</returns>
    public static Player.InputPackage GetVanillaInput(Player self) =>
        RWInput.PlayerInput(self.playerState.playerNumber);

    /// <summary>
    /// Determines if a given keybind is currently being pressed.
    /// </summary>
    /// <param name="self">The player itself.</param>
    /// <param name="keybind">The keybind to be tested.</param>
    /// <returns><c>true</c> if the keybind's key is currently being pressed, <c>false</c> otherwise.</returns>
    public static bool IsKeyPressed(Player self, Keybind keybind) =>
        IsIICEnabled()
            ? ImprovedInputHandler.IsKeyPressed(self, keybind)
            : IsKeyPressed(self, keybind.KeyboardKey, keybind.GamepadKey);

    /// <summary>
    /// Determines if one of either keys is currently being pressed.
    /// </summary>
    /// <param name="self">The player itself.</param>
    /// <param name="keyboardKey">If the player uses a keyboard, the key to be tested.</param>
    /// <param name="gamepadKey">If the player uses a gamepad, the key to be tested.</param>
    /// <returns><c>true</c> if the respective key is being pressed, <c>false</c> otherwise.</returns>
    public static bool IsKeyPressed(Player self, KeyCode keyboardKey, KeyCode gamepadKey) =>
        self.input[0].gamePad
            ? Input.GetKey(gamepadKey)
            : Input.GetKey(keyboardKey);

    /// <summary>
    /// Registers a new keybind to this mod's keybinds system, and to IIC/IIC:E if it is detected to be enabled.
    /// </summary>
    /// <param name="id">The ID of the new keybind.</param>
    /// <param name="name">The name of the keybind. Used by IIC/IIC:E for identification.</param>
    /// <param name="keyboardKey">The bound key for keyboard users.</param>
    /// <param name="gamepadKey">The bound key for gamepad users.</param>
    /// <returns>The newly created <c>Keybind</c> object.</returns>
    /// <remarks>If the given keybind is already registered, this simply returns an exact copy of what is in the mod's registry.</remarks>
    public static Keybind RegisterKeybind(string id, string name, KeyCode keyboardKey, KeyCode gamepadKey)
    {
        Keybind keybind = new(id, name, keyboardKey, gamepadKey);

        if (Keybinds.Contains(keybind))
        {
            CLLogger.LogWarning($"Tried to register an existing keybind: {keybind.ID}");
        }
        else
        {
            try
            {
                Keybinds.Add(keybind);

                if (IsIICEnabled())
                {
                    ImprovedInputHandler.RegisterPlayerKeybind(keybind);
                }

                CLLogger.LogInfo($"Registered new keybind! {keybind}");
            }
            catch (Exception ex)
            {
                CLLogger.LogError($"Failed to register keybind: {keybind}!", ex);
            }
        }

        return keybind;
    }

    /// <summary>
    /// Static handler for this mod's keybinds.
    /// </summary>
    public static class Keys
    {
        public static Keybind POSSESS { get; private set; }

        static Keys()
        {
            POSSESS = RegisterKeybind("possess", "Possess", KeyCode.V, KeyCode.Joystick1Button0);
        }

        /// <summary>
        /// Initializes all keybinds of the mod. A dummy method for the sake of registering keybinds as early as possible.
        /// </summary>
        public static void InitKeybinds() =>
            CLLogger.LogInfo($"Initialized keybinds successfully. IIC support enabled? {IsIICEnabled()}");
    }
}

/// <summary>
/// An IIC/IIC:E-compatible keybind object; Can be used by either systems to determine if a given key is being held.
/// </summary>
/// <param name="ID">The identifier of this keybind.</param>
/// <param name="Name">The name of this keybind. Used only by IIC/IIC:E for display purposes.</param>
/// <param name="KeyboardKey">The default key for keyboard users. Can only be edited in-game with IIC/IIC:E.</param>
/// <param name="GamepadKey">The default key for gamepad users. Can only be edited in-game with IIC/IIC:E.</param>
public record class Keybind(string ID, string Name, KeyCode KeyboardKey, KeyCode GamepadKey)
{
    public string ID { get; } = $"controllib:{ID}";
    public string Name { get; } = Name;
    public KeyCode KeyboardKey { get; } = KeyboardKey;
    public KeyCode GamepadKey { get; } = GamepadKey;

    public override string ToString() => $"{nameof(Keybind)}: {Name} [{ID}; {KeyboardKey}|{GamepadKey}]";
}