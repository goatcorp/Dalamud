using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;

using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Wrapper around the game keystate buffer, which contains the pressed state for all keyboard keys, indexed by virtual vkCode.
/// </summary>
/// <remarks>
/// The stored key state is actually a combination field, however the below ephemeral states are consumed each frame. Setting
/// the value may be mildly useful, however retrieving the value is largely pointless. In testing, it wasn't possible without
/// setting the statue manually.
/// index &amp; 0 = key pressed.
/// index &amp; 1 = key down (ephemeral).
/// index &amp; 2 = key up (ephemeral).
/// index &amp; 3 = short key press (ephemeral).
/// </remarks>
public interface IKeyState : IDalamudService
{
    /// <summary>
    /// Get or set the key-pressed state for a given vkCode.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>Whether the specified key is currently pressed.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/> or <see cref="GetExtendedVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    bool this[int vkCode] { get; set; }

    /// <inheritdoc cref="this[int]"/>
    bool this[VirtualKey vkCode] { get; set; }

    /// <summary>
    /// Gets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>The raw value stored in the index array.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/> or <see cref="GetExtendedVirtualKeys"/>.</exception>
    int GetRawValue(int vkCode);

    /// <inheritdoc cref="GetRawValue(int)"/>
    int GetRawValue(VirtualKey vkCode);

    /// <summary>
    /// Sets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <param name="value">The raw value to set in the index array.</param>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/> or <see cref="GetExtendedVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    void SetRawValue(int vkCode, int value);

    /// <inheritdoc cref="SetRawValue(int, int)"/>
    void SetRawValue(VirtualKey vkCode, int value);

    /// <summary>
    /// Gets a value indicating whether the given VirtualKey code is regarded as valid input by the game.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>If the code is valid.</returns>
    bool IsVirtualKeyValid(int vkCode);

    /// <inheritdoc cref="IsVirtualKeyValid(int)"/>
    bool IsVirtualKeyValid(VirtualKey vkCode);

    /// <summary>
    /// Attempts to get the SeVirtualKey equivalent of a given virtual key code.
    /// </summary>
    /// <param name="vkCode">The virtual key code to convert.</param>
    /// <param name="seVkCode">The resulting SeVirtualKey, if the conversion is successful.</param>
    /// <returns>Whether the virtual key code has a valid SeVirtualKey equivalent.</returns>
    bool TryGetSeVirtualKey(int vkCode, out SeVirtualKey? seVkCode);

    /// <inheritdoc cref="TryGetSeVirtualKey(int, out SeVirtualKey?)"/>
    bool TryGetSeVirtualKey(VirtualKey vkCode, out SeVirtualKey? seVkCode);

    /// <summary>
    /// Gets an array of virtual keys the game considers valid input.
    /// </summary>
    /// <returns>An array of valid virtual keys.</returns>
    IEnumerable<VirtualKey> GetValidVirtualKeys();

    /// <summary>
    /// Gets an array of virtual keys the game considers invalid input, but are tracked by Dalamud for plugin use.
    /// </summary>
    /// <returns>An array of extended virtual keys.</returns>
    IEnumerable<VirtualKey> GetExtendedVirtualKeys();

    /// <summary>
    /// Clears the pressed state for all keys.
    /// </summary>
    void ClearAll();
}
