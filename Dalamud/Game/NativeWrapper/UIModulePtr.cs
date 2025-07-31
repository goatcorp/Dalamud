using System.Runtime.InteropServices;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Game.NativeWrapper;

/// <summary>
/// A readonly wrapper for UIModule.
/// </summary>
/// <param name="address">The address to the UIModule.</param>
[StructLayout(LayoutKind.Explicit, Size = 0x08)]
public readonly unsafe struct UIModulePtr(nint address) : IEquatable<UIModulePtr>
{
    /// <summary>
    /// The address to the UIModule.
    /// </summary>
    [FieldOffset(0x00)]
    public readonly nint Address = address;

    /// <summary>
    /// Gets a value indicating whether the underlying pointer is a nullptr.
    /// </summary>
    public readonly bool IsNull => this.Address == 0;

    /// <summary>
    /// Gets the UIModule*.
    /// </summary>
    /// <remarks> Internal use only. </remarks>
    internal readonly UIModule* Struct => (UIModule*)this.Address;

    public static implicit operator nint(UIModulePtr wrapper) => wrapper.Address;

    public static implicit operator UIModulePtr(nint address) => new(address);

    public static implicit operator UIModulePtr(void* ptr) => new((nint)ptr);

    public static bool operator ==(UIModulePtr left, UIModulePtr right) => left.Address == right.Address;

    public static bool operator !=(UIModulePtr left, UIModulePtr right) => left.Address != right.Address;

    /// <summary>Determines whether the specified UIModulePtr is equal to the current UIModulePtr.</summary>
    /// <param name="other">The UIModulePtr to compare with the current UIModulePtr.</param>
    /// <returns><c>true</c> if the specified UIModulePtr is equal to the current UIModulePtr; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(UIModulePtr other) => this.Address == other.Address;

    /// <inheritdoc cref="object.Equals(object?)"/>
    public override readonly bool Equals(object obj) => obj is UIModulePtr wrapper && this.Equals(wrapper);

    /// <inheritdoc cref="object.GetHashCode()"/>
    public override readonly int GetHashCode() => ((nuint)this.Address).GetHashCode();
}
