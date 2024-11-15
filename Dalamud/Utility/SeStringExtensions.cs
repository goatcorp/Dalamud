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
    [Obsolete($"Use {nameof(LSeStringBuilder)}.{nameof(LSeStringBuilder.AppendMacroString)} directly instead.", true)]
    [Api11ToDo("Remove")]
    public static LSeStringBuilder AppendMacroString(this LSeStringBuilder ssb, ReadOnlySpan<byte> macroString) =>
        ssb.AppendMacroString(macroString, new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError });

    /// <summary>Compiles and appends a macro string.</summary>
    /// <param name="ssb">Target SeString builder.</param>
    /// <param name="macroString">Macro string in UTF-16 to compile and append to <paramref name="ssb"/>.</param>
    /// <returns><c>this</c> for method chaining.</returns>
    [Obsolete($"Use {nameof(LSeStringBuilder)}.{nameof(LSeStringBuilder.AppendMacroString)} directly instead.", true)]
    [Api11ToDo("Remove")]
    public static LSeStringBuilder AppendMacroString(this LSeStringBuilder ssb, ReadOnlySpan<char> macroString) =>
        ssb.AppendMacroString(macroString, new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError });

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
}
