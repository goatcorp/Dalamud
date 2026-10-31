namespace Dalamud.Interface.Fonts;

/// <summary>Thrown when a font family could not be loaded.</summary>
public sealed class FontLoadException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FontLoadException"/> class.</summary>
    /// <param name="family">The font family.</param>
    /// <param name="innerException">Inner exception.</param>
    public FontLoadException(IFontFamilyId family, Exception innerException)
        : base($"Failed to load the font {NameOf(family)}: {innerException.Message}", innerException) =>
        this.Family = family;

    /// <summary>Gets the font family that could not be loaded.</summary>
    public IFontFamilyId Family { get; }

    // A font id of a plugin may throw from ToString. The exception must still be made.
    private static string NameOf(IFontFamilyId family)
    {
        try
        {
            return family.ToString() ?? nameof(IFontFamilyId);
        }
        catch (Exception)
        {
            return nameof(IFontFamilyId);
        }
    }
}
