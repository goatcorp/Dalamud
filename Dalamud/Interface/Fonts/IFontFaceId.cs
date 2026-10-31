namespace Dalamud.Interface.Fonts;

/// <summary>Represents a font face, distinguished by its full name.<br />
/// Not intended for plugins to implement.</summary>
public interface IFontFaceId
{
    /// <summary>Gets the name of the face, such as Bold.</summary>
    LocalizedName Name { get; }

    /// <summary>
    /// Gets the associated font family.
    /// </summary>
    IFontFamilyId Family { get; }

    /// <summary>Gets the weight, slant, and stretch of the face.</summary>
    /// <remarks>For an installed font asked for by style, the style asked for. <see cref="FontStyle.Default"/> for
    /// fonts with a single face.</remarks>
    FontStyle Style { get; }

    /// <summary>Gets a value indicating whether the font can render its own name. <c>false</c> for Wingdings.</summary>
    internal bool PreviewsName => true;
}
