namespace Dalamud.Interface.Fonts;

/// <summary>Identifies a face within a font family.<br />
/// Not intended for plugins to implement.</summary>
public interface IFontFaceId
{
    /// <summary>Gets the name of the face, such as Bold.</summary>
    LocalizedName Name { get; }

    /// <summary>
    /// Gets the associated font family.
    /// </summary>
    IFontFamilyId Family { get; }

    /// <summary>Gets the weight, italic, and stretch of the face.</summary>
    /// <remarks>For a <see cref="SystemFontFaceId"/>, this is the requested or listed style.
    /// <see cref="FontStyle.Default"/> for single-face fonts.</remarks>
    FontStyle Style { get; }

    /// <summary>Gets a value indicating whether the font can render its own name. <see langword="false"/> for symbol or
    /// digit-only fonts.</summary>
    internal bool PreviewsName => true;
}
