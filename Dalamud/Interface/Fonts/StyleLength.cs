using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>A length in a <see cref="TextStyle"/>, with its unit.</summary>
/// <remarks>The value is rounded to the nearest <see cref="float"/> whose two lowest bits are 0.</remarks>
[JsonConverter(typeof(Converter))]
public readonly record struct StyleLength : ISpanParsable<StyleLength>
{
    private readonly uint packed;

    /// <summary>Initializes a new instance of the <see cref="StyleLength"/> struct.</summary>
    /// <param name="value">The value. It is 0 for <see cref="StyleUnit.Default"/>.</param>
    /// <param name="unit">The unit.</param>
    public StyleLength(float value, StyleUnit unit) =>
        this.packed = unit == StyleUnit.Default ? 0 : ((BitConverter.SingleToUInt32Bits(value) + 2) & ~3u) | (uint)unit;

    /// <summary>Gets the value.</summary>
    public float Value => BitConverter.UInt32BitsToSingle(this.packed & ~3u);

    /// <summary>Gets the unit.</summary>
    public StyleUnit Unit => (StyleUnit)(this.packed & 3);

    /// <summary>Gets a length in pixels before global scaling.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The length.</returns>
    public static StyleLength Px(float value) => new(value, StyleUnit.Px);

    /// <summary>Gets a length in ems.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The length.</returns>
    public static StyleLength Em(float value) => new(value, StyleUnit.Em);

    /// <summary>Parses a length written by <see cref="ToString"/>.</summary>
    /// <param name="s">The text, such as <c>"18px"</c> or <c>"1.2em"</c>. Empty, null, or <c>"default"</c> is the
    /// default.</param>
    /// <param name="provider">Not used. Lengths are always in the invariant culture.</param>
    /// <returns>The length.</returns>
    /// <exception cref="FormatException">If the text is not a length.</exception>
    public static StyleLength Parse(string? s, IFormatProvider? provider = null) => Parse(s.AsSpan(), provider);

    /// <inheritdoc cref="Parse(string?, IFormatProvider?)"/>
    public static StyleLength Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException($"\"{s}\" is not a length.");

    /// <summary>Tries to parse a length written by <see cref="ToString"/>.</summary>
    /// <param name="s">The text, such as <c>"18px"</c> or <c>"1.2em"</c>. Empty or <c>"default"</c> is the
    /// default.</param>
    /// <param name="provider">Not used. Lengths are always in the invariant culture.</param>
    /// <param name="result">The length, or the default if the text is not a length.</param>
    /// <returns>Whether the text is a length.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out StyleLength result)
    {
        result = default;
        return s is not null && TryParse(s.AsSpan(), provider, out result);
    }

    /// <inheritdoc cref="TryParse(string?, IFormatProvider?, out StyleLength)"/>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out StyleLength result)
    {
        result = default;
        if (s is "" or "default")
            return true;
        var unit = s.EndsWith("px") ? StyleUnit.Px : s.EndsWith("em") ? StyleUnit.Em : StyleUnit.Default;
        if (unit == StyleUnit.Default ||
            !float.TryParse(s[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return false;
        result = new(value, unit);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        this.Unit switch
        {
            StyleUnit.Px => string.Create(CultureInfo.InvariantCulture, $"{this.Value:G6}px"),
            StyleUnit.Em => string.Create(CultureInfo.InvariantCulture, $"{this.Value:G6}em"),
            _ => "default",
        };

    /// <summary>Gets a length in pixels.</summary>
    /// <param name="v">The length.</param>
    /// <param name="emPx">The em in pixels.</param>
    /// <param name="globalScale">The global scale.</param>
    /// <param name="defaultPx">The pixels for <see cref="StyleUnit.Default"/>.</param>
    /// <returns>The pixels.</returns>
    internal static float Resolve(in StyleLength v, float emPx, float globalScale, float defaultPx) =>
        v.Unit switch
        {
            StyleUnit.Px => v.Value * globalScale,
            StyleUnit.Em => v.Value * emPx,
            _ => defaultPx,
        };

    private sealed class Converter : JsonConverter<StyleLength>
    {
        public override void WriteJson(JsonWriter writer, StyleLength value, JsonSerializer serializer)
        {
            if (value.Unit == StyleUnit.Default)
                writer.WriteNull();
            else
                writer.WriteValue(value.ToString());
        }

        public override StyleLength ReadJson(
            JsonReader reader, Type objectType, StyleLength existingValue, bool hasExistingValue,
            JsonSerializer serializer) =>
            Parse(reader.Value as string);
    }
}
