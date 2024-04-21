using JetBrains.Annotations;

namespace Dalamud.Utility.Signatures;

/// <summary>
/// The main way to use SignatureHelper. Apply this attribute to any field/property
/// that should make use of a signature. See the field documentation for more
/// information.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[MeansImplicitUse(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Itself)]
// ReSharper disable once ClassNeverInstantiated.Global
public sealed class SignatureAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureAttribute"/> class.
    /// </summary>
    /// <param name="signature">signature to scan for, see <see cref="Signature"/>.</param>
    public SignatureAttribute(string signature)
    {
        this.Signature = signature;
    }

    /// <summary>
    /// Gets the memory signature for this field/property.
    /// </summary>
    public string Signature { get; init; }

    /// <summary>
    /// Gets the way this signature should be used. By default, this is guessed using
    /// simple heuristics, but it can be manually specified if SignatureHelper can't
    /// figure it out.
    ///
    /// <seealso cref="SignatureUseFlags"/>
    /// </summary>
    public SignatureUseFlags UseFlags { get; init; } = SignatureUseFlags.Auto;

    /// <summary>
    /// Gets the type of scan to perform. By default, this scans the text section of
    /// the executable, but this should be set to StaticAddress for static
    /// addresses.
    /// </summary>
    public ScanType ScanType { get; init; } = ScanType.Text;

    /// <summary>
    /// Gets the detour name if this signature is for a hook. SignatureHelper will search
    /// the type containing this field/property for a method that matches the
    /// hook's delegate type, but if it doesn't find one or finds more than one,
    /// it will fail. You can specify the name of the method here to avoid this.
    /// </summary>
    public string? DetourName { get; init; }

    /// <summary>
    /// Gets the offset from the signature to read memory from, when <see cref="UseFlags"/> is set to Offset.
    /// </summary>
    public int Offset { get; init; }

    /// <summary>
    /// Gets the fallibility of the signature.
    /// When a signature is fallible, any errors while resolving it will be
    /// logged in the Dalamud log and the field/property will not have its value
    /// set. When a signature is not fallible, any errors will be thrown as
    /// exceptions instead. If fallibility is not specified, it is inferred
    /// based on if the field/property is nullable.
    /// </summary>
    public Fallibility Fallibility { get; init; } = Fallibility.Auto;
}
