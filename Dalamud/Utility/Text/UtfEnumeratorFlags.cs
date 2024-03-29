namespace Dalamud.Utility.Text;

/// <summary>Flags on enumerating a unicode sequence.</summary>
[Flags]
public enum UtfEnumeratorFlags
{
    /// <summary>Use the default configuration of <see cref="Utf8"/> and <see cref="ReplaceErrors"/>.</summary>
    Default = default,

    /// <summary>Enumerate as UTF-8 (the default.)</summary>
    Utf8 = Default,

    /// <summary>Enumerate as UTF-16.</summary>
    Utf16 = 1 << 1,

    /// <summary>Enumerate as UTF-32.</summary>
    Utf32 = 1 << 2,

    /// <summary>Bitmask for specifying the encoding.</summary>
    UtfMask = Utf8 | Utf16 | Utf32,

    /// <summary>On error, replace to U+FFFD (REPLACEMENT CHARACTER, the default.)</summary>
    ReplaceErrors = Default,

    /// <summary>On error, drop the invalid byte.</summary>
    IgnoreErrors = 1 << 3,

    /// <summary>On error, stop the handling.</summary>
    TerminateOnFirstError = 1 << 4,

    /// <summary>On error, throw an exception.</summary>
    ThrowOnFirstError = 1 << 5,

    /// <summary>Bitmask for specifying the error handling mode.</summary>
    ErrorHandlingMask = ReplaceErrors | IgnoreErrors | TerminateOnFirstError | ThrowOnFirstError,

    /// <summary>Use the current system native endiannedness from <see cref="BitConverter.IsLittleEndian"/>
    /// (the default.)</summary>
    NativeEndian = Default,

    /// <summary>Use little endiannedness.</summary>
    LittleEndian = 1 << 6,

    /// <summary>Use big endiannedness.</summary>
    BigEndian = 1 << 7,

    /// <summary>Bitmask for specifying endianness.</summary>
    EndiannessMask = NativeEndian | LittleEndian | BigEndian,

    /// <summary>Disrespect byte order mask.</summary>
    DisrespectByteOrderMask = 1 << 8,

    /// <summary>Yield byte order masks, if it shows up.</summary>
    YieldByteOrderMask = 1 << 9,
}
