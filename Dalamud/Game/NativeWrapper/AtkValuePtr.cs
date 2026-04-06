using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.NativeWrapper;

/// <summary>
/// A readonly wrapper for AtkValue.
/// </summary>
/// <param name="address">The address to the AtkValue.</param>
[StructLayout(LayoutKind.Explicit, Size = 0x08)]
public readonly unsafe struct AtkValuePtr(nint address) : IEquatable<AtkValuePtr>
{
    /// <summary>
    /// The address to the AtkValue.
    /// </summary>
    [FieldOffset(0x00)]
    public readonly nint Address = address;

    /// <summary>
    /// Gets a value indicating whether the underlying pointer is a nullptr.
    /// </summary>
    public readonly bool IsNull => this.Address == 0;

    /// <summary>
    /// Gets the value type.
    /// </summary>
    public readonly AtkValueType ValueType => (AtkValueType)this.Struct->Type;

    /// <summary>
    /// Gets the AtkValue*.
    /// </summary>
    /// <remarks> Internal use only. </remarks>
    internal readonly AtkValue* Struct => (AtkValue*)this.Address;

    public static implicit operator nint(AtkValuePtr wrapper) => wrapper.Address;

    public static implicit operator AtkValuePtr(nint address) => new(address);

    public static implicit operator AtkValuePtr(void* ptr) => new((nint)ptr);

    public static bool operator ==(AtkValuePtr left, AtkValuePtr right) => left.Address == right.Address;

    public static bool operator !=(AtkValuePtr left, AtkValuePtr right) => left.Address != right.Address;

    /// <summary>
    /// Gets the value of the underlying <see cref="AtkValue"/> as a boxed object, based on its <see cref="AtkValueType"/>.
    /// </summary>
    /// <returns>
    /// The boxed value represented by this <see cref="AtkValuePtr"/>, or <c>null</c> if the value is null or undefined.
    /// </returns>
    /// <exception cref="NotImplementedException">
    /// Thrown if the value type is not currently handled by this implementation.
    /// </exception>
    public unsafe object? GetValue()
    {
        if (this.Struct == null)
            return null;

        return this.ValueType switch
        {
            AtkValueType.Undefined or AtkValueType.Null => null,
            AtkValueType.Bool => this.Struct->Bool,
            AtkValueType.Int => this.Struct->Int,
            AtkValueType.Int64 => this.Struct->Int64,
            AtkValueType.UInt => this.Struct->UInt,
            AtkValueType.UInt64 => this.Struct->UInt64,
            AtkValueType.Float => this.Struct->Float,
            AtkValueType.String or AtkValueType.String8 or AtkValueType.ManagedString => this.Struct->String.HasValue ? this.Struct->String.AsReadOnlySeString() : default,
            AtkValueType.WideString => this.Struct->WideString == null ? string.Empty : new string(this.Struct->WideString),
            AtkValueType.Pointer => (nint)this.Struct->Pointer,
            _ => throw new NotImplementedException($"AtkValueType {this.ValueType} is currently not supported"),
        };
    }

    /// <summary>
    /// Attempts to retrieve the value as a strongly-typed object if the underlying type matches.
    /// </summary>
    /// <typeparam name="T">The expected value type to extract.</typeparam>
    /// <param name="result">
    /// When this method returns <c>true</c>, contains the extracted value of type <typeparamref name="T"/>.
    /// Otherwise, contains the default value of type <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value was successfully extracted and matched <typeparamref name="T"/>; otherwise, <c>false</c>.
    /// </returns>
    public unsafe bool TryGet<T>(out T result) where T : struct
    {
        var value = this.GetValue();
        if (value is T typed)
        {
            result = typed;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>Determines whether the specified AtkValuePtr is equal to the current AtkValuePtr.</summary>
    /// <param name="other">The AtkValuePtr to compare with the current AtkValuePtr.</param>
    /// <returns><c>true</c> if the specified AtkValuePtr is equal to the current AtkValuePtr; otherwise, <c>false</c>.</returns>
    public readonly bool Equals(AtkValuePtr other) => this.Address == other.Address || this.Struct->EqualTo(other.Struct);

    /// <inheritdoc cref="object.Equals(object?)"/>
    public override readonly bool Equals(object obj) => obj is AtkValuePtr wrapper && this.Equals(wrapper);

    /// <inheritdoc cref="object.GetHashCode()"/>
    public override readonly int GetHashCode() => ((nuint)this.Address).GetHashCode();
}
