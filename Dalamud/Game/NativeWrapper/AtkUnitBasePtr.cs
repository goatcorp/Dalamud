using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.NativeWrapper;

/// <summary>
/// A readonly wrapper for AtkUnitBase.
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
    /// Gets a value indicating whether the OnSetup function has been called.
    /// </summary>
    public readonly bool IsReady => !this.IsNull && this.Struct->IsReady;

    /// <summary>
    /// Gets a value indicating whether the AtkUnitBase is visible.
    /// </summary>
    public readonly bool IsVisible => !this.IsNull && this.Struct->IsVisible;

    /// <summary>
    /// Gets the name.
    /// </summary>
    public readonly string Name => this.IsNull ? string.Empty : this.Struct->NameString;

    /// <summary>
    /// Gets the id.
    /// </summary>
    public readonly ushort Id => this.IsNull ? (ushort)0 : this.Struct->Id;

    /// <summary>
    /// Gets the parent id.
    /// </summary>
    public readonly ushort ParentId => this.IsNull ? (ushort)0 : this.Struct->ParentId;

    /// <summary>
    /// Gets the host id.
    /// </summary>
    public readonly ushort HostId => this.IsNull ? (ushort)0 : this.Struct->HostId;

    /// <summary>
    /// Gets the scale.
    /// </summary>
    public readonly float Scale => this.IsNull ? 0f : this.Struct->Scale;

    /// <summary>
    /// Gets the x-position.
    /// </summary>
    public readonly short X => this.IsNull ? (short)0 : this.Struct->X;

    /// <summary>
    /// Gets the y-position.
    /// </summary>
    public readonly short Y => this.IsNull ? (short)0 : this.Struct->Y;

    /// <summary>
    /// Gets the width.
    /// </summary>
    public readonly float Width => this.IsNull ? 0f : this.Struct->GetScaledWidth(false);

    /// <summary>
    /// Gets the height.
    /// </summary>
    public readonly float Height => this.IsNull ? 0f : this.Struct->GetScaledHeight(false);

    /// <summary>
    /// Gets the scaled width.
    /// </summary>
    public readonly float ScaledWidth => this.IsNull ? 0f : this.Struct->GetScaledWidth(true);

    /// <summary>
    /// Gets the scaled height.
    /// </summary>
    public readonly float ScaledHeight => this.IsNull ? 0f : this.Struct->GetScaledHeight(true);

    /// <summary>
    /// Gets the position.
    /// </summary>
    public readonly Vector2 Position => new(this.X, this.Y);

    /// <summary>
    /// Gets the size.
    /// </summary>
    public readonly Vector2 Size => new(this.Width, this.Height);

    /// <summary>
    /// Gets the scaled size.
    /// </summary>
    public readonly Vector2 ScaledSize => new(this.ScaledWidth, this.ScaledHeight);

    /// <summary>
    /// Gets the number of <see cref="AtkValue"/> entries.
    /// </summary>
    public readonly int AtkValuesCount => this.Struct->AtkValuesCount;

    /// <summary>
    /// Gets an enumerable collection of <see cref="AtkValuePtr"/> of the addons current AtkValues.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="AtkValuePtr"/> corresponding to the addons AtkValues.
    /// </returns>
    public IEnumerable<AtkValuePtr> AtkValues
    {
        get
        {
            for (var i = 0; i < this.AtkValuesCount; i++)
            {
                AtkValuePtr ptr;
                unsafe
                {
                    ptr = new AtkValuePtr((nint)this.Struct->AtkValuesSpan.GetPointer(i));
                }

                yield return ptr;
            }
        }
    }

    /// <summary>
    /// Gets the AtkUnitBase*.
    /// </summary>
    /// <remarks> Internal use only. </remarks>
    internal readonly AtkUnitBase* Struct => (AtkUnitBase*)this.Address;

    public static implicit operator nint(AtkUnitBasePtr wrapper) => wrapper.Address;

    public static implicit operator AtkUnitBasePtr(nint address) => new(address);

    public static implicit operator AtkUnitBasePtr(void* ptr) => new((nint)ptr);

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
