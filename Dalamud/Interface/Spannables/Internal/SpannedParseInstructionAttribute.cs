using Dalamud.Interface.Spannables.Elements.Strings;

namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Instructions on how to parse a string into a <see cref="SpannedString"/>.</summary>
[AttributeUsage(AttributeTargets.Method)]
internal class SpannedParseInstructionAttribute : Attribute
{
    private readonly string[] names;

    /// <summary>Initializes a new instance of the <see cref="SpannedParseInstructionAttribute"/> class.</summary>
    /// <param name="recordType">The relevant record type.</param>
    /// <param name="isRevert">Whether it's for reverting.</param>
    /// <param name="names">The short names.</param>
    public SpannedParseInstructionAttribute(SpannedRecordType recordType, bool isRevert, params string[] names)
    {
        this.RecordType = recordType;
        this.IsRevert = isRevert;
        this.names = names;
    }

    /// <summary>Gets the record type.</summary>
    public SpannedRecordType RecordType { get; }

    /// <summary>Gets a value indicating whether this is for reverting.</summary>
    public bool IsRevert { get; }

    /// <summary>Gets ther first name.</summary>
    public string Name => this.names[0];

    /// <summary>Tests if a name matches.</summary>
    /// <param name="name">The name to test.</param>
    /// <returns><c>true</c> if it matches.</returns>
    public bool Matches(ReadOnlySpan<char> name)
    {
        foreach (var n in this.names)
        {
            if (name.Equals(n, StringComparison.InvariantCultureIgnoreCase))
                return true;
        }

        return false;
    }
}
