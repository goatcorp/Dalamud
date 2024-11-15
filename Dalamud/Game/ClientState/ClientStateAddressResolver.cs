namespace Dalamud.Game.ClientState;

/// <summary>
/// Client state memory address resolver.
/// </summary>
internal sealed class ClientStateAddressResolver : BaseAddressResolver
{
    // Static offsets

    /// <summary>
    /// Gets the address of the keyboard state.
    /// </summary>
    public IntPtr KeyboardState { get; private set; }

    /// <summary>
    /// Gets the address of the keyboard state index array which translates the VK enumeration to the key state.
    /// </summary>
    public IntPtr KeyboardStateIndexArray { get; private set; }

    // Functions

    /// <summary>
    /// Gets the address of the method which sets up the player.
    /// </summary>
    public IntPtr ProcessPacketPlayerSetup { get; private set; }

    /// <summary>
    /// Gets the address of the method which polls the gamepads for data.
    /// Called every frame, even when `Enable Gamepad` is off in the settings.
    /// </summary>
    public IntPtr GamepadPoll { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.ProcessPacketPlayerSetup = sig.ScanText("40 53 48 83 EC 20 48 8D 0D ?? ?? ?? ?? 48 8B DA E8 ?? ?? ?? ?? 48 8B D3"); // not in cs struct

        // These resolve to fixed offsets only, without the base address added in, so GetStaticAddressFromSig() can't be used.
        // lea   rcx, ds:1DB9F74h[rax*4]          KeyboardState
        // movzx edx, byte ptr [rbx+rsi+1D5E0E0h] KeyboardStateIndexArray
        this.KeyboardState = sig.ScanText("48 8D 0C 85 ?? ?? ?? ?? 8B 04 31 85 C2 0F 85") + 0x4;
        this.KeyboardStateIndexArray = sig.ScanText("0F B6 94 33 ?? ?? ?? ?? 84 D2") + 0x4;

        this.GamepadPoll = sig.ScanText("40 55 53 57 41 54 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 44 0F 29 B4 24");  // unnamed in cs
    }
}
