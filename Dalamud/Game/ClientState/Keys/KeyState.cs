using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Hooking.WndProcHook;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Input;

using TerraFX.Interop.Windows;

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
[ServiceManager.EarlyLoadedService]
#pragma warning disable SA1015
[ResolveVia<IKeyState>]
#pragma warning restore SA1015
internal class KeyState : IServiceType, IKeyState, IInternalDisposableService
{
    // The array is accessed in a way that this limit doesn't appear to exist
    // but there is other state data past this point, and keys beyond here aren't
    // generally valid for most things anyway
    private const int MaxKeyCode = 0xF0;

    private static readonly ModuleLog Log = ModuleLog.Create<KeyState>();
    private readonly WndProcHookManager wndProcHookManager;

    private readonly KeyStateAddressResolver addressResolver;
    private readonly IntPtr bufferBase;
    private readonly IntPtr indexBase;

    // Buffer to store the state of extended virtual keys that do not map to a in-game keycode.
    private readonly int[] extendedKeyState = new int[0x100];
    private VirtualKey[]? validVirtualKeyCache;

    // Cache of valid virtual keys that do not map to a in-game keycode.
    private VirtualKey[]? extendedVirtualKeyCache;

    [ServiceManager.ServiceConstructor]
    private KeyState(TargetSigScanner sigScanner, WndProcHookManager wndProcHookManager)
    {
        this.addressResolver = new KeyStateAddressResolver();
        this.addressResolver.Setup(sigScanner);

        var moduleBaseAddress = sigScanner.Module.BaseAddress;
        this.bufferBase = moduleBaseAddress + Marshal.ReadInt32(this.addressResolver.KeyboardState);
        this.indexBase = moduleBaseAddress + Marshal.ReadInt32(this.addressResolver.KeyboardStateIndexArray);

        // Hook into the WndProc to track extended virtual key states
        this.wndProcHookManager = wndProcHookManager;
        this.wndProcHookManager.PreWndProc += this.OnPreWndProc;

        Log.Verbose($"Keyboard state buffer address {Util.DescribeAddress(this.bufferBase)}");
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

    /// <inheritdoc />
    public IEnumerable<VirtualKey> GetExtendedVirtualKeys()
        => this.extendedVirtualKeyCache ??= [.. Enum.GetValues<VirtualKey>().Where(vk => vk != VirtualKey.NO_KEY && !this.IsVirtualKeyValid(vk))];

    /// <inheritdoc/>
    public bool TryGetSeVirtualKey(int vkCode, out SeVirtualKey? seVkCode)
    {
        if (!this.IsVirtualKeyValid(vkCode))
        {
            seVkCode = null;
            return false;
        }

        seVkCode = (SeVirtualKey)this.ConvertVirtualKey(vkCode);
        return true;
    }

    /// <inheritdoc/>
    public bool TryGetSeVirtualKey(VirtualKey vkCode, out SeVirtualKey? seVkCode)
        => this.TryGetSeVirtualKey((int)vkCode, out seVkCode);

    /// <inheritdoc/>
    public void ClearAll()
    {
        foreach (var vk in this.GetValidVirtualKeys())
        {
            this[vk] = false;
        }

        Array.Clear(this.extendedKeyState);
    }

    /// <summary>
    /// Unhooks the WndProc and clears the extended key state buffer.
    /// </summary>
    public void Dispose()
    {
        this.wndProcHookManager.PreWndProc -= this.OnPreWndProc;
        Array.Clear(this.extendedKeyState);
    }

    /// <summary>
    /// Disposes this instance and its hooks.
    /// </summary>
    public void DisposeService() => this.Dispose();

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

        // This is the game's internal virtual key value
        return *(byte*)(this.indexBase + vkCode);
    }

    /// <summary>
    /// Gets the raw value from either the game's key state buffer or the extended key state array.
    /// </summary>
    /// <remarks>
    /// The game only recognizes certain virtual keys as valid input. If the virtual key is not valid,
    /// it will be stored in the extended key state array instead.
    /// </remarks>
    /// <param name="vkCode">The virtual key code to process.</param>
    /// <returns>A reference to the indexed array from either the game's key state buffer or the extended
    /// key state array.
    /// </returns>
    private unsafe ref int GetRefValue(int vkCode)
    {
        var gameVkCode = this.ConvertVirtualKey(vkCode);
        if (gameVkCode != 0) // Return the game's key state buffer
        {
            return ref *(int*)(this.bufferBase + (4 * gameVkCode));
        }

        if (vkCode >= 0 && vkCode < this.extendedKeyState.Length &&
            Enum.IsDefined(typeof(VirtualKey), (VirtualKey)vkCode))
        {
            return ref this.extendedKeyState[vkCode];
        }

        throw new ArgumentException($"Keycode {vkCode} does not map to a valid VirtualKey. Refer to GetValidVirtualKeys() or GetExtendedVirtualKeys() for valid keycodes.", nameof(vkCode));
    }

    /// <summary>
    /// Processes WndProc messages to track the state of extended virtual keys that do not map to a
    /// valid in-game keycode.
    /// </summary>
    /// <param name="args">The WndProc event arguments.</param>
    private void OnPreWndProc(WndProcEventArgs args)
    {
        switch (args.Message)
        {
            case WM.WM_KEYDOWN:
            case WM.WM_SYSKEYDOWN:
            {
                var vkCode = (int)args.WParam;

                // Only handle keys that are not valid in the game
                if (vkCode >= 0 && vkCode < this.extendedKeyState.Length
                    && !this.IsVirtualKeyValid(vkCode))
                {
                    this.extendedKeyState[vkCode] = 1; // Set the extended key state to pressed
                }

                break;
            }

            case WM.WM_KEYUP:
            case WM.WM_SYSKEYUP:
            {
                var vkCode = (int)args.WParam;
                if (vkCode >= 0 && vkCode < this.extendedKeyState.Length
                    && !this.IsVirtualKeyValid(vkCode))
                {
                    this.extendedKeyState[vkCode] = 0; // Set the extended key state to released
                }

                break;
            }

            case WM.WM_KILLFOCUS:
                // Clear the extended key state when the window loses focus
                Array.Clear(this.extendedKeyState);
                break;
        }
    }
}
