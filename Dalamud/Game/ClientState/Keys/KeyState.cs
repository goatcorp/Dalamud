using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Services;

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
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IKeyState>]
#pragma warning restore SA1015
internal class KeyState : IServiceType, IKeyState
{
    // The array is accessed in a way that this limit doesn't appear to exist
    // but there is other state data past this point, and keys beyond here aren't
    // generally valid for most things anyway
    private const int MaxKeyCode = 0xF0;
    private readonly IntPtr bufferBase;
    private readonly IntPtr indexBase;
    private VirtualKey[]? validVirtualKeyCache;

    [ServiceManager.ServiceConstructor]
    private KeyState(TargetSigScanner sigScanner, ClientState clientState)
    {
        var moduleBaseAddress = sigScanner.Module.BaseAddress;
        var addressResolver = clientState.AddressResolver;
        this.bufferBase = moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardState);
        this.indexBase = moduleBaseAddress + Marshal.ReadInt32(addressResolver.KeyboardStateIndexArray);

        Log.Verbose($"Keyboard state buffer address 0x{this.bufferBase.ToInt64():X}");
    }

    /// <inheritdoc/>
    public bool this[int vkCode]
    {
        get => this.GetRawValue(vkCode) != 0;
        set => this.SetRawValue(vkCode, value ? 1 : 0);
    }

    /// <inheritdoc/>
    public bool this[VirtualKey vkCode]
    {
        get => this[(int)vkCode];
        set => this[(int)vkCode] = value;
    }

    /// <inheritdoc/>
    public int GetRawValue(int vkCode)
        => this.GetRefValue(vkCode);

    /// <inheritdoc/>
    public int GetRawValue(VirtualKey vkCode)
        => this.GetRawValue((int)vkCode);

    /// <inheritdoc/>
    public void SetRawValue(int vkCode, int value)
    {
        if (value != 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Dalamud does not support pressing keys, only preventing them via zero or False. If you have a valid use-case for this, please contact the dev team.");

        this.GetRefValue(vkCode) = value;
    }

    /// <inheritdoc/>
    public void SetRawValue(VirtualKey vkCode, int value)
        => this.SetRawValue((int)vkCode, value);

    /// <inheritdoc/>
    public bool IsVirtualKeyValid(int vkCode)
        => this.ConvertVirtualKey(vkCode) != 0;

    /// <inheritdoc/>
    public bool IsVirtualKeyValid(VirtualKey vkCode)
        => this.IsVirtualKeyValid((int)vkCode);

    /// <inheritdoc/>
    public IEnumerable<VirtualKey> GetValidVirtualKeys()
        => this.validVirtualKeyCache ??= Enum.GetValues<VirtualKey>().Where(this.IsVirtualKeyValid).ToArray();

    /// <inheritdoc/>
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
