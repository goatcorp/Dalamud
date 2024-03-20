using System.Collections.Generic;

using Dalamud.Game.ClientState.Keys;

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
public interface IKeyState
{
    /// <summary>
    /// Get or set the key-pressed state for a given vkCode.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>Whether the specified key is currently pressed.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    public bool this[int vkCode] { get; set; }
    
    /// <inheritdoc cref="this[int]"/>
    public bool this[VirtualKey vkCode] { get; set; }

    /// <summary>
    /// Gets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>The raw value stored in the index array.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    public int GetRawValue(int vkCode);

    /// <inheritdoc cref="GetRawValue(int)"/>
    public int GetRawValue(VirtualKey vkCode);

    /// <summary>
    /// Sets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <param name="value">The raw value to set in the index array.</param>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    public void SetRawValue(int vkCode, int value);

    /// <inheritdoc cref="SetRawValue(int, int)"/>
    public void SetRawValue(VirtualKey vkCode, int value);

    /// <summary>
    /// Gets a value indicating whether the given VirtualKey code is regarded as valid input by the game.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>If the code is valid.</returns>
    public bool IsVirtualKeyValid(int vkCode);

    /// <inheritdoc cref="IsVirtualKeyValid(int)"/>
    public bool IsVirtualKeyValid(VirtualKey vkCode);

    /// <summary>
    /// Gets an array of virtual keys the game considers valid input.
    /// </summary>
    /// <returns>An array of valid virtual keys.</returns>
    public IEnumerable<VirtualKey> GetValidVirtualKeys();

    /// <summary>
    /// Clears the pressed state for all keys.
    /// </summary>
    public void ClearAll();
}
