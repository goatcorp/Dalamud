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
        PatchFrenchNumberSeparator();
    }

    private static void PatchFrenchNumberSeparator()
    {
        // Reset formatting specifier for the "digit grouping symbol" to an empty string
        // for cultures that use a narrow no-break space (U+202F).
        // This glyph is not present in any game fonts and not in the range for our Noto
        // so it will be rendered as a geta (=) instead. That's a hack, but it works and
        // doesn't look as weird.
        static CultureInfo PatchCulture(CultureInfo info)
        {
            var newCulture = (CultureInfo)info.Clone();

            const string invalidGroupSeparator = "\u202F";
            const string replacedGroupSeparator = " ";

            if (info.NumberFormat.NumberGroupSeparator == invalidGroupSeparator)
                newCulture.NumberFormat.NumberGroupSeparator = replacedGroupSeparator;

            if (info.NumberFormat.NumberDecimalSeparator == invalidGroupSeparator)
                newCulture.NumberFormat.NumberDecimalSeparator = replacedGroupSeparator;

            if (info.NumberFormat.CurrencyGroupSeparator == invalidGroupSeparator)
                newCulture.NumberFormat.CurrencyGroupSeparator = replacedGroupSeparator;

            if (info.NumberFormat.CurrencyDecimalSeparator == invalidGroupSeparator)
                newCulture.NumberFormat.CurrencyDecimalSeparator = replacedGroupSeparator;

            return newCulture;
        }

        CultureInfo.CurrentCulture = PatchCulture(CultureInfo.CurrentCulture);
        CultureInfo.CurrentUICulture = PatchCulture(CultureInfo.CurrentUICulture);
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentUICulture;
    }
}
