using System.Linq;

using InteropGenerator.Runtime;

using Lumina.Text.Parse;

using Lumina.Text.ReadOnly;

using DSeString = Dalamud.Game.Text.SeStringHandling.SeString;
using DSeStringBuilder = Dalamud.Game.Text.SeStringHandling.SeStringBuilder;
using LSeString = Lumina.Text.SeString;
using LSeStringBuilder = Lumina.Text.SeStringBuilder;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for SeStrings.
/// </summary>
public static class SeStringExtensions
{
    /// <summary>
    /// Convert a Lumina SeString into a Dalamud SeString.
    /// This conversion re-parses the string.
    /// </summary>
    /// <param name="originalString">The original Lumina SeString.</param>
    /// <returns>The re-parsed Dalamud SeString.</returns>
    public static DSeString ToDalamudString(this LSeString originalString) => DSeString.Parse(originalString.RawData);

    /// <summary>
    /// Convert a Lumina ReadOnlySeString into a Dalamud SeString.
    /// This conversion re-parses the string.
    /// </summary>
    /// <param name="originalString">The original Lumina ReadOnlySeString.</param>
    /// <returns>The re-parsed Dalamud SeString.</returns>
    public static DSeString ToDalamudString(this ReadOnlySeString originalString) => DSeString.Parse(originalString.Data.Span);

    /// <summary>
    /// Convert a Lumina ReadOnlySeStringSpan into a Dalamud SeString.
    /// This conversion re-parses the string.
    /// </summary>
    /// <param name="originalString">The original Lumina ReadOnlySeStringSpan.</param>
    /// <returns>The re-parsed Dalamud SeString.</returns>
    public static DSeString ToDalamudString(this ReadOnlySeStringSpan originalString) => DSeString.Parse(originalString.Data);

    /// <summary>Compiles and appends a macro string.</summary>
    /// <param name="ssb">Target SeString builder.</param>
    /// <param name="macroString">Macro string in UTF-8 to compile and append to <paramref name="ssb"/>.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    public static DSeStringBuilder AppendMacroString(this DSeStringBuilder ssb, ReadOnlySpan<byte> macroString)
    {
        var lssb = LSeStringBuilder.SharedPool.Get();
        lssb.AppendMacroString(macroString, new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError });
        ssb.Append(DSeString.Parse(lssb.ToReadOnlySeString().Data.Span));
        LSeStringBuilder.SharedPool.Return(lssb);
        return ssb;
    }

    /// <summary>Compiles and appends a macro string.</summary>
    /// <param name="ssb">Target SeString builder.</param>
    /// <param name="macroString">Macro string in UTF-16 to compile and append to <paramref name="ssb"/>.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    public static DSeStringBuilder AppendMacroString(this DSeStringBuilder ssb, ReadOnlySpan<char> macroString)
    {
        var lssb = LSeStringBuilder.SharedPool.Get();
        lssb.AppendMacroString(macroString, new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError });
        ssb.Append(DSeString.Parse(lssb.ToReadOnlySeString().Data.Span));
        LSeStringBuilder.SharedPool.Return(lssb);
        return ssb;
    }

    /// <summary>
    /// Validate if character name is valid.
    /// Both forename and surname must be between 2 and 15 characters and not total more than 20 characters combined.
    /// Only letters, hyphens, and apostrophes can be used.
    /// The first character of either name must be a letter.
    /// Hyphens cannot be used in succession or placed immediately before or after apostrophes.
    /// </summary>
    /// <param name="value">character name to validate.</param>
    /// <returns>indicator if character is name is valid.</returns>
    public static bool IsValidCharacterName(this DSeString value) => value.ToString().IsValidCharacterName();

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeString"/> contains only text payloads.
    /// </summary>
    /// <param name="ross">The <see cref="ReadOnlySeString"/> to check.</param>
    /// <returns><c>true</c> if the string contains only text payloads; otherwise, <c>false</c>.</returns>
    public static bool IsTextOnly(this ReadOnlySeString ross)
    {
        return ross.AsSpan().IsTextOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeStringSpan"/> contains only text payloads.
    /// </summary>
    /// <param name="rosss">The <see cref="ReadOnlySeStringSpan"/> to check.</param>
    /// <returns><c>true</c> if the span contains only text payloads; otherwise, <c>false</c>.</returns>
    public static bool IsTextOnly(this ReadOnlySeStringSpan rosss)
    {
        foreach (var payload in rosss)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeString"/> contains the specified text.
    /// </summary>
    /// <param name="ross">The <see cref="ReadOnlySeString"/> to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this ReadOnlySeString ross, ReadOnlySpan<byte> needle)
    {
        return ross.AsSpan().ContainsText(needle);
    }

    /// <summary>
    /// Determines whether the <see cref="ReadOnlySeStringSpan"/> contains the specified text.
    /// </summary>
    /// <param name="rosss">The <see cref="ReadOnlySeStringSpan"/> to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this ReadOnlySeStringSpan rosss, ReadOnlySpan<byte> needle)
    {
        foreach (var payload in rosss)
        {
            if (payload.Type != ReadOnlySePayloadType.Text)
                continue;

            if (payload.Body.IndexOf(needle) != -1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the <see cref="LSeStringBuilder"/> contains the specified text.
    /// </summary>
    /// <param name="builder">The builder to search.</param>
    /// <param name="needle">The text to find.</param>
    /// <returns><c>true</c> if the text is found; otherwise, <c>false</c>.</returns>
    public static bool ContainsText(this LSeStringBuilder builder, ReadOnlySpan<byte> needle)
    {
        return builder.ToReadOnlySeString().ContainsText(needle);
    }

    /// <summary>
    /// Replaces occurrences of a specified text in a <see cref="ReadOnlySeString"/> with another text.
    /// </summary>
    /// <param name="ross">The original string.</param>
    /// <param name="toFind">The text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>A new <see cref="ReadOnlySeString"/> with the replacements made.</returns>
    public static ReadOnlySeString ReplaceText(
        this ReadOnlySeString ross,
        ReadOnlySpan<byte> toFind,
        ReadOnlySpan<byte> replacement)
    {
        if (ross.IsEmpty)
            return ross;

        var sb = LSeStringBuilder.SharedPool.Get();

        foreach (var payload in ross)
        {
            if (payload.Type == ReadOnlySePayloadType.Invalid)
                continue;

            if (payload.Type != ReadOnlySePayloadType.Text)
            {
                sb.Append(payload);
                continue;
            }

            var index = payload.Body.Span.IndexOf(toFind);
            if (index == -1)
            {
                sb.Append(payload);
                continue;
            }

            var lastIndex = 0;
            while (index != -1)
            {
                sb.Append(payload.Body.Span[lastIndex..index]);

                if (!replacement.IsEmpty)
                {
                    sb.Append(replacement);
                }

                lastIndex = index + toFind.Length;
                index = payload.Body.Span[lastIndex..].IndexOf(toFind);

                if (index != -1)
                    index += lastIndex;
            }

            sb.Append(payload.Body.Span[lastIndex..]);
        }

        var output = sb.ToReadOnlySeString();
        LSeStringBuilder.SharedPool.Return(sb);
        return output;
    }

    /// <summary>
    /// Replaces occurrences of a specified text in an <see cref="LSeStringBuilder"/> with another text.
    /// </summary>
    /// <param name="builder">The builder to modify.</param>
    /// <param name="toFind">The text to find.</param>
    /// <param name="replacement">The replacement text.</param>
    public static void ReplaceText(
        this LSeStringBuilder builder,
        ReadOnlySpan<byte> toFind,
        ReadOnlySpan<byte> replacement)
    {
        if (toFind.IsEmpty)
            return;

        var str = builder.ToReadOnlySeString();
        if (str.IsEmpty)
            return;

        var replaced = ReplaceText(new ReadOnlySeString(builder.GetViewAsMemory()), toFind, replacement);
        builder.Clear().Append(replaced);
    }

    public static unsafe ReadOnlySeStringSpan AsReadOnlySeStringSpan(this CStringPointer ptr)
    {
        return new ReadOnlySeStringSpan(ptr.Value);
    }
}
