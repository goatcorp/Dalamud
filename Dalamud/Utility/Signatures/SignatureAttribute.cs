using System;

using JetBrains.Annotations;

namespace Dalamud.Utility.Signatures
{
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
        /// The memory signature for this field/property.
        /// </summary>
        public readonly string Signature;

        /// <summary>
        /// The way this signature should be used. By default, this is guessed using
        /// simple heuristics, but it can be manually specified if SignatureHelper can't
        /// figure it out.
        ///
        /// <seealso cref="SignatureUseFlags"/>
        /// </summary>
        public SignatureUseFlags UseFlags = SignatureUseFlags.Auto;

        /// <summary>
        /// The type of scan to perform. By default, this scans the text section of
        /// the executable, but this should be set to StaticAddress for static
        /// addresses.
        /// </summary>
        public ScanType ScanType = ScanType.Text;

        /// <summary>
        /// The detour name if this signature is for a hook. SignatureHelper will search
        /// the type containing this field/property for a method that matches the
        /// hook's delegate type, but if it doesn't find one or finds more than one,
        /// it will fail. You can specify the name of the method here to avoid this.
        /// </summary>
        public string? DetourName;

        /// <summary>
        /// When <see cref="UseFlags"/> is set to Offset, this is the offset from
        /// the signature to read memory from.
        /// </summary>
        public int Offset;

        /// <summary>
        /// When a signature is fallible, any errors while resolving it will be
        /// logged in the Dalamud log and the field/property will not have its value
        /// set. When a signature is not fallible, any errors will be thrown as
        /// exceptions instead. If fallibility is not specified, it is inferred
        /// based on if the field/property is nullable.
        /// </summary>
        public Fallibility Fallibility = Fallibility.Auto;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureAttribute"/> class.
        /// </summary>
        /// <param name="signature">signature to scan for, see <see cref="Signature"/></param>
        public SignatureAttribute(string signature)
        {
            this.Signature = signature;
        }
    }
}
