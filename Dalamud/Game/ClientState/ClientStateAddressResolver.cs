using System;

namespace Dalamud.Game.ClientState;

/// <summary>
/// Client state memory address resolver.
/// </summary>
internal sealed class ClientStateAddressResolver : BaseAddressResolver
{
    // Static offsets

    /// <summary>
    /// Gets the address of the actor table.
    /// </summary>
    public IntPtr ObjectTable { get; private set; }

    /// <summary>
    /// Gets the address of the buddy list.
    /// </summary>
    public IntPtr BuddyList { get; private set; }

    /// <summary>
    /// Gets the address of a pointer to the fate table.
    /// </summary>
    /// <remarks>
    /// This is a static address to a pointer, not the address of the table itself.
    /// </remarks>
    public IntPtr FateTablePtr { get; private set; }

    /// <summary>
    /// Gets the address of the Group Manager.
    /// </summary>
    public IntPtr GroupManager { get; private set; }

    /// <summary>
    /// Gets the address of the local content id.
    /// </summary>
    public IntPtr LocalContentId { get; private set; }

    /// <summary>
    /// Gets the address of job gauge data.
    /// </summary>
    public IntPtr JobGaugeData { get; private set; }

    /// <summary>
    /// Gets the address of the keyboard state.
    /// </summary>
    public IntPtr KeyboardState { get; private set; }

    /// <summary>
    /// Gets the address of the keyboard state index array which translates the VK enumeration to the key state.
    /// </summary>
    public IntPtr KeyboardStateIndexArray { get; private set; }

    /// <summary>
    /// Gets the address of the target manager.
    /// </summary>
    public IntPtr TargetManager { get; private set; }

    /// <summary>
    /// Gets the address of the condition flag array.
    /// </summary>
    public IntPtr ConditionFlags { get; private set; }

    // Functions

    /// <summary>
    /// Gets the address of the method which sets the territory type.
    /// </summary>
    public IntPtr SetupTerritoryType { get; private set; }

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
        this.ObjectTable = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F B6 83 ?? ?? ?? ?? C6 83 ?? ?? ?? ?? ??");

        this.BuddyList = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 45 84 E4 75 1A F6 45 12 04");

        this.FateTablePtr = sig.GetStaticAddressFromSig("48 8B 15 ?? ?? ?? ?? 48 8B F9 44 0F B7 41 ??");

        this.GroupManager = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 80 B8 ?? ?? ?? ?? ?? 77 71");

        this.LocalContentId = sig.GetStaticAddressFromSig("48 0F 44 05 ?? ?? ?? ?? 48 39 07");
        this.JobGaugeData = sig.GetStaticAddressFromSig("48 8B 3D ?? ?? ?? ?? 33 ED") + 0x8;

        this.SetupTerritoryType = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 66 89 91 ?? ?? ?? ??");

        // These resolve to fixed offsets only, without the base address added in, so GetStaticAddressFromSig() can't be used.
        // lea   rcx, ds:1DB9F74h[rax*4]          KeyboardState
        // movzx edx, byte ptr [rbx+rsi+1D5E0E0h] KeyboardStateIndexArray
        this.KeyboardState = sig.ScanText("48 8D 0C 85 ?? ?? ?? ?? 8B 04 31 85 C2 0F 85") + 0x4;
        this.KeyboardStateIndexArray = sig.ScanText("0F B6 94 33 ?? ?? ?? ?? 84 D2") + 0x4;

        this.ConditionFlags = sig.GetStaticAddressFromSig("48 8D 0D ?? ?? ?? ?? 41 8D 50 77 E8 ?? ?? ?? ?? 48 8B 5C 24");

        this.TargetManager = sig.GetStaticAddressFromSig("48 8B 05 ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? FF 50 ?? 48 85 DB");

        this.GamepadPoll = sig.ScanText("40 ?? 57 41 ?? 48 81 EC ?? ?? ?? ?? 44 0F ?? ?? ?? ?? ?? ?? ?? 48 8B");
    }
}
