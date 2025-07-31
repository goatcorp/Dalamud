using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Dalamud.Game.NativeWrapper;

/// <summary>
/// A wrapper for AgentInterface.
/// </summary>
/// <param name="address">The address to the AgentInterface.</param>
[StructLayout(LayoutKind.Explicit, Size = 0x08)]
public readonly unsafe struct AgentInterfacePtr(nint address) : IEquatable<AgentInterfacePtr>
{
    /// <summary>
    /// The address to the AgentInterface.
    /// </summary>
    [FieldOffset(0x00)]
    public readonly nint Address = address;

    /// <summary>
    /// Gets a value indicating whether the underlying pointer is a nullptr.
    /// </summary>
    public readonly bool IsNull => this.Address == 0;

    /// <summary>
    /// Gets a value indicating whether the agents addon is visible.
    /// </summary>
    public readonly AtkUnitBasePtr Addon
    {
        get
        {
            if (this.IsNull)
                return 0;

            var raptureAtkUnitManager = RaptureAtkUnitManager.Instance();
            if (raptureAtkUnitManager == null)
                return 0;

            return (nint)raptureAtkUnitManager->GetAddonById(this.AddonId);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the agent is active.
    /// </summary>
    public readonly ushort AddonId => (ushort)(this.IsNull ? 0 : this.Struct->GetAddonId());

    /// <summary>
    /// Gets a value indicating whether the agent is active.
    /// </summary>
    public readonly bool IsAgentActive => !this.IsNull && this.Struct->IsAgentActive();

    /// <summary>
    /// Gets a value indicating whether the agents addon is ready.
    /// </summary>
    public readonly bool IsAddonReady => !this.IsNull && this.Struct->IsAddonReady();

    /// <summary>
    /// Gets a value indicating whether the agents addon is visible.
    /// </summary>
    public readonly bool IsAddonShown => !this.IsNull && this.Struct->IsAddonShown();

    /// <summary>
    /// Gets the AgentInterface*.
    /// </summary>
    /// <remarks> Internal use only. </remarks>
    internal readonly AgentInterface* Struct => (AgentInterface*)this.Address;

    public static implicit operator nint(AgentInterfacePtr wrapper) => wrapper.Address;

    public static implicit operator AgentInterfacePtr(nint address) => new(address);

    public static implicit operator AgentInterfacePtr(void* ptr) => new((nint)ptr);

    public static bool operator ==(AgentInterfacePtr left, AgentInterfacePtr right) => left.Address == right.Address;

    public static bool operator !=(AgentInterfacePtr left, AgentInterfacePtr right) => left.Address != right.Address;

    /// <summary>
    /// Focuses the AtkUnitBase.
    /// </summary>
    /// <returns> <c>true</c> when the addon was focused, <c>false</c> otherwise. </returns>
    public readonly bool FocusAddon() => this.IsNull && this.Struct->FocusAddon();

    /// <summary>Determines whether the specified AgentInterfacePtr is equal to the current AgentInterfacePtr.</summary>
    /// <param name="other">The AgentInterfacePtr to compare with the current AgentInterfacePtr.</param>
    /// <returns><c>true</c> if the specified AgentInterfacePtr is equal to the current AgentInterfacePtr; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(AgentInterfacePtr other) => this.Address == other.Address;

    /// <inheritdoc cref="object.Equals(object?)"/>
    public override readonly bool Equals(object obj) => obj is AgentInterfacePtr wrapper && this.Equals(wrapper);

    /// <inheritdoc cref="object.GetHashCode()"/>
    public override readonly int GetHashCode() => ((nuint)this.Address).GetHashCode();
}
