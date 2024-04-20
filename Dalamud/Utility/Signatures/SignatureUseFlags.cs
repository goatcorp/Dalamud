using Dalamud.Hooking;

namespace Dalamud.Utility.Signatures;

/// <summary>
/// Use flags for a signature attribute. This tells SignatureHelper how to use the
/// result of the signature.
/// </summary>
public enum SignatureUseFlags
{
    /// <summary>
    /// SignatureHelper will use simple heuristics to determine the best signature
    /// use for the field/property.
    /// </summary>
    Auto,

    /// <summary>
    /// The signature should be used as a plain pointer. This is correct for
    /// static addresses, functions, or anything else that's an
    /// <see cref="IntPtr"/> at heart.
    /// </summary>
    Pointer,

    /// <summary>
    /// The signature should be used as a hook. This is correct for
    /// <see cref="Hook{T}"/> fields/properties.
    /// </summary>
    Hook,

    /// <summary>
    /// The signature should be used to determine an offset. This is the default
    /// for all primitive types. SignatureHelper will read from the memory at this
    /// signature and store the result in the field/property. An offset from the
    /// signature can be specified in the <see cref="SignatureAttribute"/>.
    /// </summary>
    Offset,
}
