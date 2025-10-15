using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using RWCustom;
using UnityEngine;

namespace ModLib.Input;

/// <summary>
///     An immutable representation of a player keybind, compatible with ImprovedInput's PlayerKeybind object.
/// </summary>
public record Keybind
{
    private static readonly List<Keybind> _keybinds = [];
    private static readonly ReadOnlyCollection<Keybind> _readonlyKeybinds = new(_keybinds);

    private static global::Options? Options => Custom.rainWorld?.options;

    /// <summary>
    ///     The unique identifier of this Keybind.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     The name of the mod this Keybind belongs to.
    /// </summary>
    public string Mod { get; }

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

    private Keybind(string id, string mod, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        Id = id;
        Mod = mod;
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

        return Options?.controls[playerNumber].controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
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

        return Options?.controls[playerNumber].controlPreference == global::Options.ControlSetup.ControlToUse.SPECIFIC_GAMEPAD
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

    /// <inheritdoc cref="Register(string, string, KeyCode, KeyCode, KeyCode)"/>
    public static Keybind Register(string name, KeyCode keyboardPreset, KeyCode gamepadPreset) =>
        Register(null, name, keyboardPreset, gamepadPreset, gamepadPreset);

    /// <inheritdoc cref="Register(string, string, KeyCode, KeyCode, KeyCode)"/>
    public static Keybind Register(string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset) =>
        Register(null, name, keyboardPreset, gamepadPreset, xboxPreset);

    /// <inheritdoc cref="Register(string, string, KeyCode, KeyCode, KeyCode)"/>
    public static Keybind Register(string? id, string name, KeyCode keyboardPreset, KeyCode gamepadPreset) =>
        Register(id, name, keyboardPreset, gamepadPreset, gamepadPreset);

    /// <summary>
    ///     Registers a new Keybind with the provided arguments.
    /// </summary>
    /// <remarks>
    ///     If the Improved Input: Extended mod is present, an equivalent <c>PlayerKeybind</c> is also registered to the game.
    /// </remarks>
    /// <param name="id">
    ///     <para>
    ///         The identifier of this keybind. Must be an unique string not used by any other mod, or yourself.
    ///     </para>
    ///     <para>
    ///         If omitted, an unique identifier is generated with the format <c>"{ModId}.{KeybindName}"</c>
    ///     </para>
    /// </param>
    /// <param name="name">The name of the new Keybind. Will be displayed for players with IIC:E enabled.</param>
    /// <param name="keyboardPreset">The key code for usage by keyboard devices.</param>
    /// <param name="gamepadPreset">The key code for usage by gamepad input devices.</param>
    /// <param name="xboxPreset">The key code for usage by Xbox input devices.</param>
    /// <returns>The registered <see cref="Keybind"/> object.</returns>
    public static Keybind Register(string? id, string name, KeyCode keyboardPreset, KeyCode gamepadPreset, KeyCode xboxPreset)
    {
        BepInPlugin plugin = Registry.GetMod(Assembly.GetCallingAssembly()).Plugin;

        id ??= $"{GetModPrefix(plugin)}.{name.ToLowerInvariant()}";

        Keybind? gameKeybind = Get(id);

        if (gameKeybind is null)
        {
            gameKeybind = new(id, plugin.Name, name, keyboardPreset, gamepadPreset, xboxPreset);

            if (Extras.IsIICEnabled)
                ImprovedInputHelper.RegisterKeybind(gameKeybind);

            _keybinds.Add(gameKeybind);
        }

        return gameKeybind;

        static string GetModPrefix(BepInPlugin plugin)
        {
            return plugin.GUID.Split('.', '_', ' ').Last().ToLowerInvariant();
        }
    }

    /// <summary>
    ///     Retrieves the equivalent Keybind object of a PlayerKeybind instance. If none is found, a new Keybind is registered using the PlayerKeybind's values as arguments.
    /// </summary>
    /// <param name="self">The PlayerKeybind object to be converted.</param>
    public static implicit operator Keybind(ImprovedInput.PlayerKeybind self)
    {
        return Get(self.Id) ?? new(self.Id, self.Mod, self.Name, self.KeyboardPreset, self.GamepadPreset, self.XboxPreset);
    }

    /// <summary>
    ///     Retrieves the equivalent PlayerKeybind object registered with the Keybind object. If none is found, a new PlayerKeybind is registered using the Keybind's values as arguments.
    /// </summary>
    /// <param name="self">The Keybind object to be converted.</param>
    public static explicit operator ImprovedInput.PlayerKeybind(Keybind self)
    {
        return ImprovedInput.PlayerKeybind.Get(self.Id) ?? ImprovedInput.PlayerKeybind.Register(self.Id, self.Mod, self.Name, self.KeyboardPreset, self.GamepadPreset, self.XboxPreset);
    }

    private static void ValidatePlayerNumber(int playerNumber)
    {
        if (playerNumber < 0 || playerNumber >= Options?.controls?.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(playerNumber), $"Player number {playerNumber} is not valid.");
        }
    }
}