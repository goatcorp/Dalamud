using System.Globalization;

namespace Dalamud.Utility;

/// <summary>
/// Class containing fixes for culture-specific issues.
/// </summary>
internal static class CultureFixes
{
    /// <summary>
    /// Apply all fixes.
    /// </summary>
    public static void Apply()
    {
        PatchNumberSeparator();
    }

    private static void PatchNumberSeparator()
    {
        // Reset formatting specifier for the "digit grouping symbol" to an empty string
        // for cultures that use a narrow no-break space (U+202F).
        // This glyph is not present in any game fonts and not in the range for our Noto
        // so it will be rendered as a geta (=) instead. That's a hack, but it works and
        // doesn't look as weird.
        void PatchCulture(CultureInfo info)
        {
            const string invalidGroupSeparator = "\u202F";
            const string replacedGroupSeparator = " ";
            if (info.NumberFormat.NumberGroupSeparator == invalidGroupSeparator)
                info.NumberFormat.NumberGroupSeparator = replacedGroupSeparator;
                
            if (info.NumberFormat.NumberDecimalSeparator == invalidGroupSeparator)
                info.NumberFormat.NumberDecimalSeparator = replacedGroupSeparator;

            if (info.NumberFormat.CurrencyGroupSeparator == invalidGroupSeparator)
                info.NumberFormat.CurrencyGroupSeparator = replacedGroupSeparator;
                
            if (info.NumberFormat.CurrencyDecimalSeparator == invalidGroupSeparator)
                info.NumberFormat.CurrencyDecimalSeparator = replacedGroupSeparator;
        }
            
        PatchCulture(CultureInfo.CurrentCulture);
        PatchCulture(CultureInfo.CurrentUICulture);
    }
}
