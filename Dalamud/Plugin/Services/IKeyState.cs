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
/// 
/// <para>
/// Virtual keys are tracked through two methods:
/// <list type="bullet">
/// <item>
/// <term><see cref="IsVirtualKeyValid(int)"/> and <see cref="GetValidVirtualKeys"/></term>
/// <description>The game's own keystate buffer.</description>
/// </item>
/// <item>
/// <term><see cref="IsExtendedVirtualKeyValid(int)"/> and <see cref="GetExtendedVirtualKeys"/></term>
/// <description>Dalamud's extended virtual key support.</description>
/// </item>
/// </list>
/// A given <see cref="VirtualKey"/> is only ever one or the other, never both.
/// </para>
/// <para>
/// Directional modifier keys (<see cref="VirtualKey.LSHIFT"/>, <see cref="VirtualKey.RSHIFT"/>, <see cref="VirtualKey.LCONTROL"/>,
/// <see cref="VirtualKey.RCONTROL"/>, <see cref="VirtualKey.LMENU"/>, <see cref="VirtualKey.RMENU"/>) are tracked by Dalamud as <em>extended</em> virtual keys.
/// The game only tracks generic modifiers (<see cref="VirtualKey.SHIFT"/>, <see cref="VirtualKey.CONTROL"/>, <see cref="VirtualKey.MENU"/>). Pressing a
/// directional modifier key will activate both the generic game modifier in <see cref="GetValidVirtualKeys"/> and the extended key in <see cref="GetExtendedVirtualKeys"/>.
/// </para>
/// <para>
/// Most do not need to distinguish between the two: <see cref="this[int]"/> and <see cref="GetRawValue(int)"/>/<see cref="SetRawValue(int, int)"/>
/// transparently handle both. The sole distinction is in <see cref="TryGetSeVirtualKey(int, out int)"/> which only succeeds for
/// valid game's keys; a valid extended key and an invalid key will both return <see langword="false"/>. Use <see cref="IsExtendedVirtualKeyValid(int)"/>
/// to distinguish between the two.
/// </para>
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
    /// Gets a value indicating whether the given VirtualKey code is natively tracked by the game.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>If the code is valid.</returns>
    /// <remarks>
    /// Directional modifier keys (<see cref="VirtualKey.LSHIFT"/>, <see cref="VirtualKey.RSHIFT"/>, etc.) return
    /// <see langword="false" /> here as they are tracked by Dalamud as extended virtual keys. Use <see cref="IsExtendedVirtualKeyValid(int)"/> to check for those.
    /// </remarks>
    bool IsVirtualKeyValid(int vkCode);

    /// <inheritdoc cref="IsVirtualKeyValid(int)"/>
    bool IsVirtualKeyValid(VirtualKey vkCode);

    /// <summary>
    /// Gets a value indicating whether the given VirtualKey code is regarded as a valid extended key by Dalamud.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>If the code is a valid extended key.</returns>
    /// <remarks>
    /// Extended keys include keys the game does not track natively (<see cref="VirtualKey.F13"/>) as well as
    /// directional modifier keys (<see cref="VirtualKey.LSHIFT"/>, <see cref="VirtualKey.RSHIFT"/>, etc.).
    /// </remarks>
    bool IsExtendedVirtualKeyValid(int vkCode);

    /// <inheritdoc cref="IsExtendedVirtualKeyValid(int)"/>
    bool IsExtendedVirtualKeyValid(VirtualKey vkCode);

    /// <summary>
    /// Attempts to get the game's virtual key code equivalent of a given virtual key code.
    /// </summary>
    /// <param name="vkCode">The virtual key code to convert.</param>
    /// <param name="seVkCode">The resulting game's virtual key code, if the conversion is successful.</param>
    /// <returns>Whether the virtual key code has a valid game's virtual key code equivalent.</returns>
    /// <remarks>
    /// Returns <see langword="false"/> both when <paramref name="vkCode"/> is not a valid <see cref="VirtualKey"/>
    /// and when it is a valid <em>extended</em> key that the game does not recognize.
    /// If you need to distinguish between the two, use <see cref="IsExtendedVirtualKeyValid(int)"/>.
    /// </remarks>
    bool TryGetSeVirtualKey(int vkCode, out int seVkCode);

    /// <inheritdoc cref="TryGetSeVirtualKey(int, out int)"/>
    bool TryGetSeVirtualKey(VirtualKey vkCode, out int seVkCode);

    /// <summary>
    /// Gets an array of virtual keys the game considers valid input.
    /// </summary>
    /// <returns>An array of valid virtual keys.</returns>
    /// <remarks>
    /// This list contains generic modifiers (<see cref="VirtualKey.SHIFT"/>, <see cref="VirtualKey.CONTROL"/>,
    /// <see cref="VirtualKey.MENU"/>). See <see cref="GetExtendedVirtualKeys"/> for directional modifiers
    /// (<see cref="VirtualKey.LSHIFT"/>, <see cref="VirtualKey.RSHIFT"/>, etc.).
    /// </remarks>
    IEnumerable<VirtualKey> GetValidVirtualKeys();

    /// <summary>
    /// Gets an array of virtual keys tracked by Dalamud for plugin use.
    /// </summary>
    /// <returns>An array of extended virtual keys.</returns>
    /// <remarks>
    /// This list contains directional modifiers (<see cref="VirtualKey.LSHIFT"/>, <see cref="VirtualKey.RSHIFT"/>, etc.)
    /// and keys the game does not natively recognize.
    /// </remarks>
    IEnumerable<VirtualKey> GetExtendedVirtualKeys();

    /// <summary>
    /// Clears the pressed state for all keys.
    /// </summary>
    void ClearAll();
}
