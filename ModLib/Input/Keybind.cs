using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace ModLib.Input;

/// <summary>
///     An immutable representation of a player keybind, compatible with ImprovedInput's PlayerKeybind object.
/// </summary>
public sealed record Keybind
{
    private static readonly List<Keybind> _keybinds = [];
    private static readonly ReadOnlyCollection<Keybind> _readonlyKeybinds = new(_keybinds);

    private static readonly string _modId = ModPlugin.Assembly.GetModId().Split('.', '_', ' ').Last().ToLowerInvariant();

    private static global::Options Options => Custom.rainWorld.options;

    /// <summary>
    ///     The unique identifier of this Keybind.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     The name of the mod this Keybind belongs to.
    /// </summary>
    public string Mod { get; } = ModPlugin.Assembly.GetModName();

    /// <summary>
    ///     The user-friendly name of this Keybind.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with a keyboard.
    /// </summary>
    public KeyCode KeyboardPreset { get; }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with a gamepad.
    /// </summary>
    public KeyCode GamepadPreset { get; }

    /// <summary>
    ///     The KeyCode to be used for detecting inputs with an xbox.
    /// </summary>
    public KeyCode XboxPreset { get; }

    private Keybind(string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Id = $"{_modId}:{name.ToLowerInvariant()}";
        Name = name;
        KeyboardPreset = keyboardPreset;
        GamepadPreset = gamepadPreset;
        XboxPreset = xboxPreset;
    }

    /// <summary>
    ///     Determines whether this keybind is currently being pressed.
    /// </summary>
    /// <param name="playerNumber">The player index whose input will be queried.</param>
    /// <returns><c>true</c> if this keybind's bound key is being held, <c>false</c> otherwise.</returns>
    public bool IsDown(int playerNumber)
    {
        ValidatePlayerNumber(playerNumber);

        return Options.controls[playerNumber].controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
            ? UnityEngine.Input.GetKey(GamepadPreset)
            : UnityEngine.Input.GetKey(KeyboardPreset);
    }

    /// <summary>
    ///     Determines whether this keybind has just been pressed.
    /// </summary>
    /// <param name="playerNumber">The player index whose input will be queried.</param>
    /// <returns><c>true</c> if this keybind's bound key was just pressed, <c>false</c> otherwise.</returns>
    public bool JustPressed(int playerNumber)
    {
        ValidatePlayerNumber(playerNumber);

        return Options.controls[playerNumber].controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
            ? UnityEngine.Input.GetKeyDown(GamepadPreset)
            : UnityEngine.Input.GetKeyDown(KeyboardPreset);
    }

    /// <summary>
    ///     Returns a string that represents the Keybind object.
    /// </summary>
    /// <returns>A string that represents the Keybind object.</returns>
    public override string ToString() => $"{Name} ({Id}) [{KeyboardPreset}|{GamepadPreset}{(XboxPreset != GamepadPreset ? $"|{XboxPreset}" : "")}]";

    /// <summary>
    ///     Retrieves the Keybind with the given identifier.
    /// </summary>
    /// <param name="id">The identifier of the Keybind to be retrieved.</param>
    /// <returns>The <see cref="Keybind"/> object whose Id matches the provided argument, or <c>null</c> if none is found.</returns>
    public static Keybind? Get(string id) => _keybinds.Find(k => k.Id == id);

    /// <summary>
    ///     Returns a read-only list of all registered keybinds.
    /// </summary>
    /// <returns>A read-only list of all registered keybinds.</returns>
    public static IReadOnlyList<Keybind> Keybinds() => _readonlyKeybinds;

    /// <inheritdoc cref="Register(string, KeyCode, KeyCode, KeyCode)"/>
    public static Keybind Register(string name, KeyCode keyboardPreset, KeyCode gamepadPreset) =>
        Register(name, keyboardPreset, gamepadPreset, gamepadPreset);

    /// <summary>
    ///     Registers a new Keybind to ModLib's keybind registry with the provided arguments.
    /// </summary>
    /// <param name="name">The name of the new Keybind.</param>
    /// <param name="keyboardPreset">The key code for usage by keyboard devices.</param>
    /// <param name="gamepadPreset">The key code for usage by gamepad input devices.</param>
    /// <param name="xboxPreset">The key code for usage by Xbox input devices.</param>
    /// <returns>The newly registered <see cref="Keybind"/> object.</returns>
    public static Keybind Register(string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Keybind? gameKeybind = Get($"{_modId}:{name.ToLowerInvariant()}");

        if (gameKeybind is null)
        {
            gameKeybind = new(name, keyboardPreset, gamepadPreset, xboxPreset);

            _keybinds.Add(gameKeybind);
        }

        return gameKeybind;
    }

    /// <summary>
    ///     Converts the PlayerKeybind object to an equivalent Keybind instance. If none is found, a new one is registered with the PlayerKeybind's data.
    /// </summary>
    /// <param name="self">The PlayerKeybind object to be converted.</param>
    public static implicit operator Keybind(ImprovedInput.PlayerKeybind self)
    {
        return Get(self.Id) ?? Register(self.Name, self.KeyboardPreset, self.GamepadPreset, self.XboxPreset);
    }

    /// <summary>
    ///     Converts the Keybind object to an equivalent PlayerKeybind instance. If none is found, a new one is registered with the Keybind's data.
    /// </summary>
    /// <param name="self">The Keybind object to be converted.</param>
    public static explicit operator ImprovedInput.PlayerKeybind(Keybind self)
    {
        return ImprovedInput.PlayerKeybind.Get(self.Id) ?? ImprovedInput.PlayerKeybind.Register(self.Id, self.Mod, self.Name, self.KeyboardPreset, self.GamepadPreset, self.XboxPreset);
    }

    private static void ValidatePlayerNumber(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= Options.controls.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(playerNumber), $"Player number {playerNumber} is not valid.");
        }
    }
}