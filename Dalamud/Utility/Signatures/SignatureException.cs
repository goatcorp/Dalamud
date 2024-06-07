namespace Dalamud.Utility.Signatures;

/// <summary>
/// An exception for signatures.
/// </summary>
public class SignatureException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureException"/> class.
    /// </summary>
    /// <param name="message">Message.</param>
    internal SignatureException(string message)
        : base(message)
    {
    }
}
