using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;

namespace Dalamud.Interface.Fonts;

/// <summary>A value of an axis of a variable font, such as <c>new("wght", 700)</c> for bold.</summary>
/// <param name="Tag">The OpenType axis tag, its first character in the lowest byte.</param>
/// <param name="Value">The value, in the units of the axis.</param>
public readonly record struct FontAxis(uint Tag, float Value)
{
    private static readonly string[] RegisteredAxes = ["ital", "opsz", "slnt", "wdth", "wght"];

    /// <summary>Initializes a new instance of the <see cref="FontAxis"/> struct.</summary>
    /// <param name="tag">Four printable ASCII characters, such as <c>"wght"</c>. Registered tags are normalized to
    /// lowercase.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">If <paramref name="tag"/> is not 4 printable ASCII characters.</exception>
    public FontAxis(string tag, float value)
        : this(
            TryParseTag(tag, out var t) ? t : throw new ArgumentException($"\"{tag}\" is not an axis tag.", nameof(tag)),
            value)
    {
    }

    /// <summary>Gets the tag as text.</summary>
    public string TagName => string.Create(4, this.Tag, static (s, t) =>
    {
        for (var i = 0; i < 4; i++)
            s[i] = (char)((t >> (8 * i)) & 0xFF);
    });

    /// <inheritdoc/>
    /// <remarks>The tag and the value, such as <c>wght=700</c>.</remarks>
    public override string ToString() =>
        $"{this.TagName}={this.Value.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Sorts axis values by tag, and keeps the last value of each tag and finite values only.</summary>
    /// <param name="axes">The values.</param>
    /// <returns>The values, so that equal sets compare equal.</returns>
    internal static ImmutableArray<FontAxis> Normalize(ImmutableArray<FontAxis> axes)
    {
        if (axes.IsDefaultOrEmpty)
            return [];
        var values = new SortedDictionary<string, FontAxis>(StringComparer.Ordinal);
        foreach (var a in axes)
        {
            if (float.IsFinite(a.Value))
                values[a.TagName] = a;
        }

        return [.. values.Values];
    }

    private static bool TryParseTag(ReadOnlySpan<char> text, out uint tag)
    {
        tag = 0;
        if (text.Length != 4)
            return false;
        foreach (var c in text)
        {
            if (c is < (char)0x20 or > (char)0x7E)
                return false;
        }

        // Tags are case sensitive, but the registered ones are only ever lowercase.
        foreach (var registered in RegisteredAxes)
        {
            if (registered.AsSpan().Equals(text, StringComparison.OrdinalIgnoreCase))
                text = registered;
        }

        tag = text[0] | ((uint)text[1] << 8) | ((uint)text[2] << 16) | ((uint)text[3] << 24);
        return true;
    }
}
