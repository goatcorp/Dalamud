using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Gui.NativeWrapper;

/// <summary>
/// A wrapper for AtkUnitBase.
/// </summary>
/// <param name="address">The address to the AtkUnitBase.</param>
[StructLayout(LayoutKind.Explicit, Size = 0x08)]
public readonly unsafe struct AtkUnitBasePtr(nint address) : IEquatable<AtkUnitBasePtr>
{
    /// <summary>
    /// The address to the AtkUnitBase.
    /// </summary>
    [FieldOffset(0x00)]
    public readonly nint Address = address;

    /// <summary>
    /// Gets a value indicating whether the underlying pointer is a nullptr.
    /// </summary>
    public readonly bool IsNull => this.Address == 0;

    /// <summary>
    /// Gets a value indicating whether the OnSetup function of the AtkUnitBase has been called.
    /// </summary>
    public readonly bool IsReady => !this.IsNull && this.Struct->IsReady;

    /// <summary>
    /// Gets a value indicating whether the AtkUnitBase is visible.
    /// </summary>
    public readonly bool IsVisible => !this.IsNull && this.Struct->IsVisible;

    /// <summary>
    /// Gets the name of the AtkUnitBase.
    /// </summary>
    public readonly string Name => this.IsNull ? string.Empty : this.Struct->NameString;

    /// <summary>
    /// Gets the id of the AtkUnitBase.
    /// </summary>
    public readonly ushort Id => this.IsNull ? (ushort)0 : this.Struct->Id;

    /// <summary>
    /// Gets the parent id of the AtkUnitBase.
    /// </summary>
    public readonly ushort ParentId => this.IsNull ? (ushort)0 : this.Struct->ParentId;

    /// <summary>
    /// Gets the host id of the AtkUnitBase.
    /// </summary>
    public readonly ushort HostId => this.IsNull ? (ushort)0 : this.Struct->HostId;

    /// <summary>
    /// Gets the scale of the AtkUnitBase.
    /// </summary>
    public readonly float Scale => this.IsNull ? 0f : this.Struct->Scale;

    /// <summary>
    /// Gets the x-position of the AtkUnitBase.
    /// </summary>
    public readonly short X => this.IsNull ? (short)0 : this.Struct->X;

    /// <summary>
    /// Gets the y-position of the AtkUnitBase.
    /// </summary>
    public readonly short Y => this.IsNull ? (short)0 : this.Struct->Y;

    /// <summary>
    /// Gets the width of the AtkUnitBase.
    /// </summary>
    public readonly float Width => this.IsNull ? 0f : this.Struct->GetScaledWidth(false);

    /// <summary>
    /// Gets the height of the AtkUnitBase.
    /// </summary>
    public readonly float Height => this.IsNull ? 0f : this.Struct->GetScaledHeight(false);

    /// <summary>
    /// Gets the scaled width of the AtkUnitBase.
    /// </summary>
    public readonly float ScaledWidth => this.IsNull ? 0f : this.Struct->GetScaledWidth(true);

    /// <summary>
    /// Gets the scaled height of the AtkUnitBase.
    /// </summary>
    public readonly float ScaledHeight => this.IsNull ? 0f : this.Struct->GetScaledHeight(true);

    /// <summary>
    /// Gets the position of the AtkUnitBase.
    /// </summary>
    public readonly Vector2 Position => new(this.X, this.Y);

    /// <summary>
    /// Gets the size of the AtkUnitBase.
    /// </summary>
    public readonly Vector2 Size => new(this.Width, this.Height);

    /// <summary>
    /// Gets the scaled size of the AtkUnitBase.
    /// </summary>
    public readonly Vector2 ScaledSize => new(this.ScaledWidth, this.ScaledHeight);

    /// <summary>
    /// Gets the AtkUnitBase*.
    /// </summary>
    /// <remarks> Internal use only. </remarks>
    internal readonly AtkUnitBase* Struct => (AtkUnitBase*)this.Address;

    public static implicit operator nint(AtkUnitBasePtr wrapper) => wrapper.Address;

    public static implicit operator AtkUnitBasePtr(nint address) => new(address);

    public static bool operator ==(AtkUnitBasePtr left, AtkUnitBasePtr right) => left.Address == right.Address;

    public static bool operator !=(AtkUnitBasePtr left, AtkUnitBasePtr right) => left.Address != right.Address;

    /// <summary>
    /// Focuses the AtkUnitBase.
    /// </summary>
    public readonly void Focus()
    {
        if (!this.IsNull)
            this.Struct->Focus();
    }

    /// <summary>Determines whether the specified AtkUnitBasePtr is equal to the current AtkUnitBasePtr.</summary>
    /// <param name="other">The AtkUnitBasePtr to compare with the current AtkUnitBasePtr.</param>
    /// <returns><c>true</c> if the specified AtkUnitBasePtr is equal to the current AtkUnitBasePtr; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(AtkUnitBasePtr other) => this.Address == other.Address;

    /// <inheritdoc cref="object.Equals(object?)"/>
    public override readonly bool Equals(object obj) => obj is AtkUnitBasePtr wrapper && this.Equals(wrapper);

    /// <inheritdoc cref="object.GetHashCode()"/>
    public override readonly int GetHashCode() => ((nuint)this.Address).GetHashCode();
}
