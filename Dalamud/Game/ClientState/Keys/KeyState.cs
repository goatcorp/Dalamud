using System;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Serilog;

namespace Dalamud.Game.ClientState.Keys;

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
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.BlockingEarlyLoadedService]
public class KeyState : IServiceType
{
    // The array is accessed in a way that this limit doesn't appear to exist
    // but there is other state data past this point, and keys beyond here aren't
    // generally valid for most things anyway
    private const int MaxKeyCode = 0xF0;
    private readonly IntPtr bufferBase;
    private readonly IntPtr indexBase;
    private VirtualKey[] validVirtualKeyCache = null;

    [ServiceManager.ServiceConstructor]
    private KeyState(SigScanner sigScanner, ClientState clientState)
    {
        var moduleBaseAddress = sigScanner.Module.BaseAddress;
        var addressResolver = clientState.AddressResolver;
        this.bufferBase = moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardState);
        this.indexBase = moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardStateIndexArray);

        Log.Verbose($"Keyboard state buffer address 0x{this.bufferBase.ToInt64():X}");
    }

    /// <summary>
    /// Get or set the key-pressed state for a given vkCode.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>Whether the specified key is currently pressed.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    public unsafe bool this[int vkCode]
    {
        get => this.GetRawValue(vkCode) != 0;
        set => this.SetRawValue(vkCode, value ? 1 : 0);
    }

    /// <inheritdoc cref="this[int]"/>
    public bool this[VirtualKey vkCode]
    {
        get => this[(int)vkCode];
        set => this[(int)vkCode] = value;
    }

    /// <summary>
    /// Gets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <returns>The raw value stored in the index array.</returns>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    public int GetRawValue(int vkCode)
        => this.GetRefValue(vkCode);

    /// <inheritdoc cref="GetRawValue(int)"/>
    public int GetRawValue(VirtualKey vkCode)
        => this.GetRawValue((int)vkCode);

    /// <summary>
    /// Sets the value in the index array.
    /// </summary>
    /// <param name="vkCode">The virtual key to change.</param>
    /// <param name="value">The raw value to set in the index array.</param>
    /// <exception cref="ArgumentException">If the vkCode is not valid. Refer to <see cref="IsVirtualKeyValid(int)"/> or <see cref="GetValidVirtualKeys"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the set value is non-zero.</exception>
    public void SetRawValue(int vkCode, int value)
    {
        if (value != 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Dalamud does not support pressing keys, only preventing them via zero or False. If you have a valid use-case for this, please contact the dev team.");

        this.GetRefValue(vkCode) = value;
    }

    /// <inheritdoc cref="SetRawValue(int, int)"/>
    public void SetRawValue(VirtualKey vkCode, int value)
        => this.SetRawValue((int)vkCode, value);

    /// <summary>
    /// Gets a value indicating whether the given VirtualKey code is regarded as valid input by the game.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>If the code is valid.</returns>
    public bool IsVirtualKeyValid(int vkCode)
        => this.ConvertVirtualKey(vkCode) != 0;

    /// <inheritdoc cref="IsVirtualKeyValid(int)"/>
    public bool IsVirtualKeyValid(VirtualKey vkCode)
        => this.IsVirtualKeyValid((int)vkCode);

    /// <summary>
    /// Gets an array of virtual keys the game considers valid input.
    /// </summary>
    /// <returns>An array of valid virtual keys.</returns>
    public VirtualKey[] GetValidVirtualKeys()
        => this.validVirtualKeyCache ??= Enum.GetValues<VirtualKey>().Where(vk => this.IsVirtualKeyValid(vk)).ToArray();

    /// <summary>
    /// Clears the pressed state for all keys.
    /// </summary>
    public void ClearAll()
    {
        foreach (var vk in this.GetValidVirtualKeys())
        {
            this[vk] = false;
        }
    }

    /// <summary>
    /// Converts a virtual key into the equivalent value that the game uses.
    /// Valid values are non-zero.
    /// </summary>
    /// <param name="vkCode">Virtual key.</param>
    /// <returns>Converted value.</returns>
    private unsafe byte ConvertVirtualKey(int vkCode)
    {
        if (vkCode <= 0 || vkCode >= MaxKeyCode)
            return 0;

        return *(byte*)(this.indexBase + vkCode);
    }

    /// <summary>
    /// Gets the raw value from the key state array.
    /// </summary>
    /// <param name="vkCode">Virtual key code.</param>
    /// <returns>A reference to the indexed array.</returns>
    private unsafe ref int GetRefValue(int vkCode)
    {
        vkCode = this.ConvertVirtualKey(vkCode);

        if (vkCode == 0)
            throw new ArgumentException($"Keycode state is only valid for certain values. Reference GetValidVirtualKeys for help.");

        return ref *(int*)(this.bufferBase + (4 * vkCode));
    }
}
