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

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    private readonly KeyStateAddressResolver addressResolver;
    private readonly IntPtr bufferBase;
    private readonly IntPtr indexBase;

    // Buffer to store the state of extended virtual keys that do not map to a in-game keycode.
    private readonly int[] extendedKeyState = new int[0x100];

    // Stores the virtual key codes that have been pressed or released in the current frame.
    private readonly HashSet<int> ephemeralKeys = [];
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

        // Clear ephemeral key states at the end of each frame
        this.framework.Update += this.OnFrameworkUpdate;

        Log.Verbose($"Keyboard state buffer address {Util.DescribeAddress(this.bufferBase)}");
    }

    /// <inheritdoc/>
    public bool this[int vkCode]
    {
        get => (this.GetRawValue(vkCode) & (int)KeyStateFlags.Down) != 0;
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
    public bool IsExtendedVirtualKeyValid(int vkCode)
    {
        var vk = (VirtualKey)vkCode;
        return vkCode > 0 && vkCode < this.extendedKeyState.Length
            && Enum.IsDefined(typeof(VirtualKey), vk)
            && !IsExcludedKey(vk)
            && !this.IsVirtualKeyValid(vkCode);
    }

    /// <inheritdoc/>
    public bool IsExtendedVirtualKeyValid(VirtualKey vkCode)
        => this.IsExtendedVirtualKeyValid((int)vkCode);

    /// <inheritdoc/>
    public IEnumerable<VirtualKey> GetValidVirtualKeys()
        => this.validVirtualKeyCache ??= Enum.GetValues<VirtualKey>().Where(this.IsVirtualKeyValid).ToArray();

    /// <inheritdoc/>
    public IEnumerable<VirtualKey> GetExtendedVirtualKeys()
        => this.extendedVirtualKeyCache ??= [.. Enum.GetValues<VirtualKey>().Where(this.IsExtendedVirtualKeyValid)];

    /// <inheritdoc/>
    public bool TryGetSeVirtualKey(int vkCode, out int seVkCode)
    {
        if (!this.IsVirtualKeyValid(vkCode))
        {
            seVkCode = 0;
            return false;
        }

        seVkCode = this.ConvertVirtualKey(vkCode);
        return true;
    }

    /// <inheritdoc/>
    public bool TryGetSeVirtualKey(VirtualKey vkCode, out int seVkCode)
        => this.TryGetSeVirtualKey((int)vkCode, out seVkCode);

    /// <inheritdoc/>
    public void ClearAll()
    {
        foreach (var vk in this.GetValidVirtualKeys())
        {
            this[vk] = false;
        }

        Array.Clear(this.extendedKeyState);
        this.ephemeralKeys.Clear();
    }

    /// <summary>
    /// Unhooks WndProc and Framework and clears the extended key state/ephemeral key buffer.
    /// </summary>
    public void DisposeService()
    {
        this.wndProcHookManager.PreWndProc -= this.OnPreWndProc;
        this.framework.Update -= this.OnFrameworkUpdate;
        Array.Clear(this.extendedKeyState);
        this.ephemeralKeys.Clear();
    }

    /// <summary>
    /// Determines whether the given virtual key is excluded from extended key state tracking.
    /// </summary>
    /// <param name="key">The virtual key to check.</param>
    /// <returns>True if the key is excluded, false otherwise.</returns>
    private static bool IsExcludedKey(VirtualKey key)
        => key is VirtualKey.LBUTTON or VirtualKey.RBUTTON or VirtualKey.NO_KEY;

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

        if (this.IsExtendedVirtualKeyValid(vkCode))
        {
            return ref this.extendedKeyState[vkCode];
        }

        throw new ArgumentException($"Keycode {vkCode} does not map to a valid VirtualKey. Refer to GetValidVirtualKeys() or GetExtendedVirtualKeys() for valid keycodes.", nameof(vkCode));
    }

    private void SetKeyDown(int vkCode, bool isRepeat)
    {
        ref var state = ref this.GetRefValue(vkCode);
        var flags = KeyStateFlags.Down | (isRepeat ? KeyStateFlags.Held : KeyStateFlags.Pressed);
        state = (int)flags;
        this.ephemeralKeys.Add(vkCode);
    }

    private void SetKeyUp(int vkCode)
    {
        ref var state = ref this.GetRefValue(vkCode);
        state = (int)KeyStateFlags.Released;
        this.ephemeralKeys.Add(vkCode);
    }

    private void UpdateShiftStates()
    {
        // Get the current state of the left and right shift keys
        var isLeftDown = (Windows.Win32.PInvoke.GetKeyState((int)VirtualKey.LSHIFT) & 0x8000) != 0;
        var isRightDown = (Windows.Win32.PInvoke.GetKeyState((int)VirtualKey.RSHIFT) & 0x8000) != 0;

        // Get the previous state of the left and right shift keys
        var wasLeftDown = (this.GetRefValue((int)VirtualKey.LSHIFT) & (int)KeyStateFlags.Down) != 0;
        var wasRightDown = (this.GetRefValue((int)VirtualKey.RSHIFT) & (int)KeyStateFlags.Down) != 0;

        if (isLeftDown)
            this.SetKeyDown((int)VirtualKey.LSHIFT, wasLeftDown);
        else if (wasLeftDown)
            this.SetKeyUp((int)VirtualKey.LSHIFT);

        if (isRightDown)
            this.SetKeyDown((int)VirtualKey.RSHIFT, wasRightDown);
        else if (wasRightDown)
            this.SetKeyUp((int)VirtualKey.RSHIFT);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (this.ephemeralKeys.Count == 0) return;

        foreach (var vkCode in this.ephemeralKeys)
        {
            ref var state = ref this.GetRefValue(vkCode);

            // Clear one-frame ephemeral flags
            state &= ~(int)(KeyStateFlags.Pressed | KeyStateFlags.Held);

            // If the key was released, clear the down flag as well
            state &= ~(int)KeyStateFlags.Released;
        }

        this.ephemeralKeys.RemoveWhere(vk => (this.GetRefValue(vk) & (int)(KeyStateFlags.Pressed |
            KeyStateFlags.Released | KeyStateFlags.Held)) == 0);
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
            case WM.WM_MBUTTONDOWN:
            case WM.WM_MBUTTONDBLCLK:
                this.SetKeyDown((int)VirtualKey.MBUTTON, args.Message == WM.WM_MBUTTONDBLCLK);
                break;
            case WM.WM_MBUTTONUP:
                this.SetKeyUp((int)VirtualKey.MBUTTON);
                break;

            case WM.WM_XBUTTONDOWN:
            case WM.WM_XBUTTONDBLCLK:
            {
                var xButton = TerraFX.Interop.Windows.Windows.GET_XBUTTON_WPARAM(args.WParam) ==
                    TerraFX.Interop.Windows.Windows.XBUTTON1 ? VirtualKey.XBUTTON1 : VirtualKey.XBUTTON2;
                this.SetKeyDown((int)xButton, args.Message == WM.WM_XBUTTONDBLCLK);
                break;
            }

            case WM.WM_XBUTTONUP:
            {
                var xButton = TerraFX.Interop.Windows.Windows.GET_XBUTTON_WPARAM(args.WParam) ==
                    TerraFX.Interop.Windows.Windows.XBUTTON1 ? VirtualKey.XBUTTON1 : VirtualKey.XBUTTON2;
                this.SetKeyUp((int)xButton);
                break;
            }

            case WM.WM_KEYDOWN:
            case WM.WM_SYSKEYDOWN:
            {
                var vkCode = (int)args.WParam;
                var isRepeat = (args.LParam & (1 << 30)) != 0;

                switch (vkCode)
                {
                    // Process individual keys for Ctrl, Alt, and Shift for extended key state tracking
                    case (int)VirtualKey.SHIFT:
                        this.UpdateShiftStates();
                        break;
                    case (int)VirtualKey.CONTROL:
                        var ctrlKey = (args.LParam & (1 << 24)) != 0 ? VirtualKey.RCONTROL : VirtualKey.LCONTROL;
                        this.SetKeyDown((int)ctrlKey, isRepeat);
                        break;
                    case (int)VirtualKey.MENU: // Alt
                        var altKey = (args.LParam & (1 << 24)) != 0 ? VirtualKey.RMENU : VirtualKey.LMENU;
                        this.SetKeyDown((int)altKey, isRepeat);
                        break;
                    default:
                        // Only handle keys that are not valid in the game
                        if (vkCode >= 0 && vkCode < this.extendedKeyState.Length
                            && !this.IsVirtualKeyValid(vkCode))
                        {
                            this.SetKeyDown(vkCode, isRepeat);
                        }

                        break;
                }

                break;
            }

            case WM.WM_KEYUP:
            case WM.WM_SYSKEYUP:
            {
                var vkCode = (int)args.WParam;

                switch (vkCode)
                {
                    case (int)VirtualKey.SHIFT:
                        this.UpdateShiftStates();
                        break;
                    case (int)VirtualKey.CONTROL:
                        var ctrlKey = (args.LParam & (1 << 24)) != 0 ? VirtualKey.RCONTROL : VirtualKey.LCONTROL;
                        this.SetKeyUp((int)ctrlKey);
                        break;
                    case (int)VirtualKey.MENU: // Alt
                        var altKey = (args.LParam & (1 << 24)) != 0 ? VirtualKey.RMENU : VirtualKey.LMENU;
                        this.SetKeyUp((int)altKey);
                        break;
                    default:
                        if (vkCode >= 0 && vkCode < this.extendedKeyState.Length
                            && !this.IsVirtualKeyValid(vkCode))
                        {
                            this.SetKeyUp(vkCode);
                        }

                        break;
                }

                break;
            }

            case WM.WM_KILLFOCUS:
                // Reset the state of modifier keys when the window loses focus
                this.GetRefValue((int)VirtualKey.LSHIFT) = 0;
                this.GetRefValue((int)VirtualKey.RSHIFT) = 0;
                this.GetRefValue((int)VirtualKey.LCONTROL) = 0;
                this.GetRefValue((int)VirtualKey.RCONTROL) = 0;
                this.GetRefValue((int)VirtualKey.LMENU) = 0;
                this.GetRefValue((int)VirtualKey.RMENU) = 0;

                // Clear the extended key state when the window loses focus
                Array.Clear(this.extendedKeyState);
                this.ephemeralKeys.Clear();
                break;
        }
    }
}
