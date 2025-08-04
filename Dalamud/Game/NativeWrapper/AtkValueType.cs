namespace Dalamud.Game.NativeWrapper;

/// <summary>
/// Represents the data type of the AtkValue.
/// </summary>
public enum AtkValueType
{
    /// <summary>
    /// The value is undefined or invalid.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The value is null.
    /// </summary>
    Null = 0x1,

    /// <summary>
    /// The value is a boolean.
    /// </summary>
    Bool = 0x2,

    /// <summary>
    /// The value is a 32-bit signed integer.
    /// </summary>
    Int = 0x3,

    /// <summary>
    /// The value is a 64-bit signed integer.
    /// </summary>
    Int64 = 0x4,

    /// <summary>
    /// The value is a 32-bit unsigned integer.
    /// </summary>
    UInt = 0x5,

    /// <summary>
    /// The value is a 64-bit unsigned integer.
    /// </summary>
    UInt64 = 0x6,

    /// <summary>
    /// The value is a 32-bit floating-point number.
    /// </summary>
    Float = 0x7,

    /// <summary>
    /// The value points to a null-terminated 8-bit character string (ASCII or UTF-8).
    /// </summary>
    String = 0x8,

    /// <summary>
    /// The value points to a null-terminated 16-bit character string (UTF-16 / wide string).
    /// </summary>
    WideString = 0x9,

    /// <summary>
    /// The value points to a constant null-terminated 8-bit character string (const char*).
    /// </summary>
    String8 = 0xA,

    /// <summary>
    /// The value is a vector.
    /// </summary>
    Vector = 0xB,

    /// <summary>
    /// The value is a pointer.
    /// </summary>
    Pointer = 0xC,

    /// <summary>
    /// The value is pointing to an array of AtkValue entries.
    /// </summary>
    AtkValues = 0xD,

    /// <summary>
    /// The value is a managed string. See <see cref="String"/>.
    /// </summary>
    ManagedString = 0x28,

    /// <summary>
    /// The value is a managed vector. See <see cref="Vector"/>.
    /// </summary>
    ManagedVector = 0x2B,
}
