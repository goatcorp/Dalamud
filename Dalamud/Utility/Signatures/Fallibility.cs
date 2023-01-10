namespace Dalamud.Utility.Signatures;

/// <summary>
/// The fallibility of a signature.
/// </summary>
public enum Fallibility
{
    /// <summary>
    /// The fallibility of the signature is determined by the field/property's
    /// nullability.
    /// </summary>
    Auto,

    /// <summary>
    /// The signature is fallible.
    /// </summary>
    Fallible,

    /// <summary>
    /// The signature is infallible.
    /// </summary>
    Infallible,
}
