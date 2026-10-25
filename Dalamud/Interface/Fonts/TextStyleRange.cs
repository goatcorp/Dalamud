using System.Collections.Immutable;
using System.Runtime.InteropServices;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>A range of text with one field of its style set.</summary>
/// <remarks><ul>
/// <li>Offsets are in code units of the text. These are bytes of UTF-8, or chars of UTF-16, as are the offsets a
/// layout reports.</li>
/// <li>Ranges of different fields compose. For the same field, later ranges win.</li>
/// </ul></remarks>
/// <seealso cref="ImGuiTextExtensions.extension(ImGui).TextStyledRanges(ReadOnlySpan{byte},ReadOnlySpan{TextStyleRange},bool)"/>
public readonly record struct TextStyleRange
{
    // Factory methods normalize values through TextStyle setters so equal effective changes compare equal.
    private readonly Value value;
    private readonly object? reference;

    private TextStyleRange(int offset, int length, TextStyleField field, Value value, object? reference = null)
    {
        this.Offset = offset;
        this.Length = length;
        this.Field = field;
        this.value = value;
        this.reference = reference;
    }

    /// <summary>Gets the start, in code units of the text.</summary>
    public int Offset { get; }

    /// <summary>Gets the length, in code units of the text.</summary>
    public int Length { get; }

    /// <summary>Gets the field it sets.</summary>
    public TextStyleField Field { get; }

    /// <summary>Gets a range that sets <see cref="TextStyle.FamilyId"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="family">The family, or null for the default font.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange FamilyId(int offset, int length, IFontFamilyId? family) =>
        new(offset, length, TextStyleField.FamilyId, default, new TextStyle { FamilyId = family }.FamilyId);

    /// <summary>Gets a range that sets <see cref="TextStyle.Color"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="color">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Color(int offset, int length, ImTextColor color) =>
        new(offset, length, TextStyleField.Color, new() { Color = color });

    /// <summary>Gets a range that sets <see cref="TextStyle.Shadow"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="shadow">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Shadow(int offset, int length, TextShadow shadow) =>
        new(offset, length, TextStyleField.Shadow, new() { Shadow = new TextStyle { Shadow = shadow }.Shadow });

    /// <summary>Gets a range that sets <see cref="TextStyle.Edge"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="edge">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Edge(int offset, int length, TextEdge edge) =>
        new(offset, length, TextStyleField.Edge, new() { Edge = new TextStyle { Edge = edge }.Edge });

    /// <summary>Gets a range that sets <see cref="TextStyle.Background"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="color">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Background(int offset, int length, ImTextColor color) =>
        new(
            offset, length, TextStyleField.Background,
            new() { Color = new TextStyle { Background = color }.Background });

    /// <summary>Gets a range that sets <see cref="TextStyle.Lines"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="lines">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Lines(int offset, int length, TextLineDecoration lines) =>
        new(offset, length, TextStyleField.Lines, new() { Lines = new TextStyle { Lines = lines }.Lines });

    /// <summary>Gets a range that sets <see cref="TextStyle.ExtraDecoration"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="decoration">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange ExtraDecoration(int offset, int length, TextDecoration decoration) =>
        new(
            offset,
            length,
            TextStyleField.ExtraDecoration,
            new() { Decoration = new TextStyle { ExtraDecoration = decoration }.ExtraDecoration });

    /// <summary>Gets a range that sets <see cref="TextStyle.Bold"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="bold">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Bold(int offset, int length, bool bold = true) =>
        new(offset, length, TextStyleField.Bold, new() { Number = bold ? 1 : 0 });

    /// <summary>Gets a range that sets <see cref="TextStyle.Italic"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="italic">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Italic(int offset, int length, bool italic = true) =>
        new(offset, length, TextStyleField.Italic, new() { Number = italic ? 1 : 0 });

    /// <summary>Gets a range that sets <see cref="TextStyle.NoSubpixel"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="noSubpixel">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange NoSubpixel(int offset, int length, bool noSubpixel = true) =>
        new(offset, length, TextStyleField.NoSubpixel, new() { Number = noSubpixel ? 1 : 0 });

    /// <summary>Gets a range that sets <see cref="TextStyle.NoHinting"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="noHinting">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange NoHinting(int offset, int length, bool noHinting = true) =>
        new(offset, length, TextStyleField.NoHinting, new() { Number = noHinting ? 1 : 0 });

    /// <summary>Gets a range that sets <see cref="TextStyle.Weight"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="weight">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Weight(int offset, int length, int weight) =>
        new(offset, length, TextStyleField.Weight, new() { Number = new TextStyle { Weight = weight }.Weight });

    /// <summary>Gets a range that sets <see cref="TextStyle.Stretch"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="stretch">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Stretch(int offset, int length, int stretch) =>
        new(offset, length, TextStyleField.Stretch, new() { Number = new TextStyle { Stretch = stretch }.Stretch });

    /// <summary>Gets a range that sets <see cref="TextStyle.LetterSpacing"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="spacing">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange LetterSpacing(int offset, int length, StyleLength spacing) =>
        new(
            offset,
            length,
            TextStyleField.LetterSpacing,
            new() { Length = new TextStyle { LetterSpacing = spacing }.LetterSpacing });

    /// <summary>Gets a range that sets <see cref="TextStyle.Locale"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="locale">The value.</param>
    /// <returns>The range.</returns>
    /// <exception cref="System.Globalization.CultureNotFoundException">If the value is not a predefined culture.
    /// </exception>
    public static TextStyleRange Locale(int offset, int length, string? locale) =>
        new(offset, length, TextStyleField.Locale, default, new TextStyle { Locale = locale }.Locale);

    /// <summary>Gets a range that sets <see cref="TextStyle.Features"/>.</summary>
    /// <param name="offset">The start.</param>
    /// <param name="length">The length.</param>
    /// <param name="features">The value.</param>
    /// <returns>The range.</returns>
    public static TextStyleRange Features(int offset, int length, ImmutableArray<FontFeature> features) =>
        new(offset, length, TextStyleField.Features, default, new EquatableArray<FontFeature>(features));

    /// <inheritdoc/>
    public bool Equals(TextStyleRange other)
    {
        if (this.Offset != other.Offset || this.Length != other.Length || this.Field != other.Field)
            return false;

        return this.Field switch
        {
            TextStyleField.FamilyId or TextStyleField.Locale or TextStyleField.Features =>
                Equals(this.reference, other.reference),
            TextStyleField.Color or TextStyleField.Background => this.value.Color == other.value.Color,
            TextStyleField.Shadow => this.value.Shadow == other.value.Shadow,
            TextStyleField.Edge => this.value.Edge == other.value.Edge,
            TextStyleField.Lines => this.value.Lines == other.value.Lines,
            TextStyleField.ExtraDecoration => this.value.Decoration == other.value.Decoration,
            TextStyleField.LetterSpacing => this.value.Length == other.value.Length,
            _ => this.value.Number == other.value.Number,
        };
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var fieldHash = this.Field switch
        {
            TextStyleField.FamilyId or TextStyleField.Locale or TextStyleField.Features =>
                this.reference?.GetHashCode() ?? 0,
            TextStyleField.Color or TextStyleField.Background => this.value.Color.GetHashCode(),
            TextStyleField.Shadow => this.value.Shadow.GetHashCode(),
            TextStyleField.Edge => this.value.Edge.GetHashCode(),
            TextStyleField.Lines => this.value.Lines.GetHashCode(),
            TextStyleField.ExtraDecoration => this.value.Decoration.GetHashCode(),
            TextStyleField.LetterSpacing => this.value.Length.GetHashCode(),
            _ => this.value.Number.GetHashCode(),
        };
        return HashCode.Combine(this.Offset, this.Length, this.Field, fieldHash);
    }

    /// <summary>Gets this range at offset and length 0, as a key of the change alone.</summary>
    /// <returns>The range.</returns>
    internal TextStyleRange AtZero() => new(0, 0, this.Field, this.value, this.reference);

    /// <summary>Sets the field of a style.</summary>
    /// <param name="style">The style.</param>
    internal void ApplyTo(ref TextStyle style)
    {
        switch (this.Field)
        {
            case TextStyleField.FamilyId:
                style.FamilyId = (IFontFamilyId?)this.reference;
                break;
            case TextStyleField.Color:
                style.Color = this.value.Color;
                break;
            case TextStyleField.Shadow:
                style.Shadow = this.value.Shadow;
                break;
            case TextStyleField.Edge:
                style.Edge = this.value.Edge;
                break;
            case TextStyleField.Background:
                style.Background = this.value.Color;
                break;
            case TextStyleField.Lines:
                style.Lines = this.value.Lines;
                break;
            case TextStyleField.ExtraDecoration:
                style.ExtraDecoration = this.value.Decoration;
                break;
            case TextStyleField.Bold:
                style.Bold = this.value.Number != 0;
                break;
            case TextStyleField.Italic:
                style.Italic = this.value.Number != 0;
                break;
            case TextStyleField.NoSubpixel:
                style.NoSubpixel = this.value.Number != 0;
                break;
            case TextStyleField.NoHinting:
                style.NoHinting = this.value.Number != 0;
                break;
            case TextStyleField.Weight:
                style.Weight = this.value.Number;
                break;
            case TextStyleField.Stretch:
                style.Stretch = this.value.Number;
                break;
            case TextStyleField.LetterSpacing:
                style.LetterSpacing = this.value.Length;
                break;
            case TextStyleField.Locale:
                style.Locale = (string?)this.reference;
                break;
            case TextStyleField.Features:
                style.Features = ((EquatableArray<FontFeature>?)this.reference ?? default).Array;
                break;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Value
    {
        [FieldOffset(0)]
        public ImTextColor Color;

        [FieldOffset(0)]
        public TextShadow Shadow;

        [FieldOffset(0)]
        public TextEdge Edge;

        [FieldOffset(0)]
        public TextLineDecoration Lines;

        [FieldOffset(0)]
        public TextDecoration Decoration;

        [FieldOffset(0)]
        public StyleLength Length;

        [FieldOffset(0)]
        public int Number;
    }
}
