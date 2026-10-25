using System.Collections.Generic;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>
/// Represents a font family identifier.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontFamilyId
{
    /// <summary>Gets the name of the family.</summary>
    LocalizedName Name { get; }

    /// <summary>
    /// Gets the list of fonts under this family.
    /// </summary>
    /// <exception cref="System.IO.FileNotFoundException">If the family is a system family that is not installed.
    /// </exception>
    [JsonIgnore]
    IReadOnlyList<IFontFaceId> Fonts { get; }

    /// <summary>Gets a short name for debug display, such as in <see cref="ChainFamilyId.ToString"/>.</summary>
    internal string ShortName => this.ToString() ?? string.Empty;

    /// <summary>Gets a key used to deduplicate load warnings per font source.</summary>
    /// <remarks>Shared across faces and axis instances of the same file without retaining font data or assemblies.</remarks>
    internal string DiagnosticKey => this.ToString() ?? string.Empty;

    /// <summary>Gets a value indicating whether characters the family lacks already come from the default font.
    /// </summary>
    internal bool EndsWithDefault => false;

    /// <summary>Gets all <see cref="FontRegistration"/> instances referenced by this family, including nested wrapped
    /// fonts.</summary>
    internal IEnumerable<FontRegistration> Registrations => [];

    /// <summary>Finds the index of the font inside <see cref="Fonts"/> closest to a style.</summary>
    /// <param name="style">The style.</param>
    /// <returns>The index of the font. A valid index if <see cref="Fonts"/> is not empty, else 0.</returns>
    int FindBestMatch(FontStyle style);

    /// <summary>
    /// Gets the list of Dalamud-provided fonts.
    /// </summary>
    /// <returns>The list of fonts.</returns>
    static List<IFontFamilyId> ListDalamudFonts() =>
    [
        new DalamudAssetFontId(DalamudAsset.NotoSansCjkMedium),
        new DalamudAssetFontId(DalamudAsset.NotoSansCjkRegular),
        new DalamudAssetFontId(DalamudAsset.InconsolataRegular),
        new DalamudAssetFontId(DalamudAsset.FontAwesomeFreeSolid),
    ];

    /// <summary>
    /// Gets the list of Game-provided fonts.
    /// </summary>
    /// <returns>The list of fonts.</returns>
    static List<IFontFamilyId> ListGameFonts() =>
    [
        new GameFontId(GameFontFamily.Axis),
        new GameFontId(GameFontFamily.Jupiter),
        new GameFontId(GameFontFamily.JupiterNumeric),
        new GameFontId(GameFontFamily.Meidinger),
        new GameFontId(GameFontFamily.MiedingerMid),
        new GameFontId(GameFontFamily.TrumpGothic),
    ];

    /// <summary>
    /// Gets the list of System-provided fonts.
    /// </summary>
    /// <param name="refresh">If <c>true</c>, try to refresh the list.</param>
    /// <returns>The list of fonts.</returns>
    static List<IFontFamilyId> ListSystemFonts(bool refresh) => DirectWriteUtilities.ListFamilies(refresh);

    /// <summary>Gets the face that best matches a style.</summary>
    /// <param name="style">The style.</param>
    /// <returns>The face.</returns>
    internal IFontFaceId GetFace(FontStyle style) => this.Fonts[this.FindBestMatch(style)];

    /// <summary>Appends this family's font elements to <paramref name="composer"/>.</summary>
    /// <param name="composer">The target composer.</param>
    internal void AppendTo(FontComposer composer) => composer.AddFace(this);
}
