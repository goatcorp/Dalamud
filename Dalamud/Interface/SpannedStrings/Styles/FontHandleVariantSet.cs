using System.Runtime.CompilerServices;

using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Styles;

/// <summary>Describes how to draw a segment of text.</summary>
public struct FontHandleVariantSet : IEquatable<FontHandleVariantSet>
{
    /// <summary>The font ID.</summary>
    public IFontFamilyId? FontFamilyId;

    /// <summary>The normal font.</summary>
    /// <remarks>If not set, one will be created from <see cref="FontFamilyId"/> if set. Otherwise, the font from the
    /// current ImGui context will be used (=<see cref="ImGui.GetFont"/>).
    /// </remarks>
    public IFontHandle? Normal;

    /// <summary>The italic font.</summary>
    /// <remarks>If not set, one will be created from <see cref="FontFamilyId"/> if set. Otherwise, faux italic font of
    /// <see cref="Normal"/> will be used.</remarks>
    public IFontHandle? Italic;

    /// <summary>The bold font.</summary>
    /// <remarks>If not set, one will be created from <see cref="FontFamilyId"/> if set. Otherwise, faux bold font of
    /// <see cref="Normal"/> will be used.</remarks>
    public IFontHandle? Bold;

    /// <summary>The bold and italic font.</summary>
    /// <remarks>If not set, one will be created from <see cref="FontFamilyId"/> if set. Otherwise, faux versions of
    /// other fonts will be used.</remarks>
    public IFontHandle? ItalicBold;

    /// <summary>Initializes a new instance of the <see cref="FontHandleVariantSet"/> struct.</summary>
    /// <param name="fontFamilyId">The optional font family ID.</param>
    /// <param name="normal">The optional normal font.</param>
    /// <param name="italic">The optional italic font.</param>
    /// <param name="bold">The optional bold font.</param>
    /// <param name="italicBold">The optional italic bold font.</param>
    public FontHandleVariantSet(
        IFontFamilyId? fontFamilyId = null,
        IFontHandle? normal = null,
        IFontHandle? italic = null,
        IFontHandle? bold = null,
        IFontHandle? italicBold = null)
    {
        this.FontFamilyId = fontFamilyId;
        this.Normal = normal;
        this.Italic = italic;
        this.Bold = bold;
        this.ItalicBold = italicBold;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(FontHandleVariantSet left, FontHandleVariantSet right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(FontHandleVariantSet left, FontHandleVariantSet right) => !left.Equals(right);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(FontHandleVariantSet other) =>
        Equals(this.FontFamilyId, other.FontFamilyId)
        && this.Normal == other.Normal
        && this.Italic == other.Italic
        && this.Bold == other.Bold
        && this.ItalicBold == other.ItalicBold;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly bool Equals(object? obj) => obj is FontHandleVariantSet other && this.Equals(other);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode() => HashCode.Combine(
        this.Normal,
        this.Italic,
        this.Bold,
        this.ItalicBold);

    /// <summary>Gets the effective font.</summary>
    /// <param name="italic">Whether to use an italic font.</param>
    /// <param name="bold">Whether to use a bold font.</param>
    /// <param name="fauxItalic">Whether to skew the returned font.</param>
    /// <param name="fauxBold">Whether to thicken the returned font.</param>
    /// <returns>The font to use.</returns>
    internal readonly IFontHandle? GetEffectiveFont(bool italic, bool bold, out bool fauxItalic, out bool fauxBold)
    {
        if (!italic && !bold)
        {
            fauxItalic = fauxBold = false;
            return this.Normal;
        }

        if (italic && !bold)
        {
            fauxItalic = this.Italic?.Available is not true;
            fauxBold = false;
            return fauxItalic ? this.Normal : this.Italic;
        }

        if (!italic)
        {
            fauxItalic = false;
            fauxBold = this.Bold?.Available is not true;
            return fauxBold ? this.Normal : this.Bold;
        }

        if (this.ItalicBold?.Available is true)
        {
            fauxItalic = fauxBold = false;
            return this.ItalicBold;
        }

        if (this.Italic?.Available is true)
        {
            fauxItalic = false;
            fauxBold = true;
            return this.Italic;
        }

        if (this.Bold?.Available is true)
        {
            fauxItalic = true;
            fauxBold = false;
            return this.Bold;
        }

        fauxItalic = fauxBold = true;
        return this.Normal;
    }
}
