namespace Dalamud.Interface.Spannables.Internal;

/// <summary>Short names for enums.</summary>
[AttributeUsage(AttributeTargets.Field)]
internal class SpannedParseShortNameAttribute : Attribute
{
    private readonly string[] names;

    /// <summary>Initializes a new instance of the <see cref="SpannedParseShortNameAttribute"/> class.</summary>
    /// <param name="names">The short names.</param>
    public SpannedParseShortNameAttribute(params string[] names)
    {
        this.names = names;
    }

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
