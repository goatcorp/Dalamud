using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.ManagedFontAtlas.Internals;

/// <summary>
/// Deals with TrueType.
/// </summary>
internal static partial class TrueTypeUtils
{
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Version name in enum value names")]
    private enum IsoEncodingId : ushort
    {
        Ascii = 0,
        Iso_10646 = 1,
        Iso_8859_1 = 2,
    }

    private enum MacintoshEncodingId : ushort
    {
        Roman = 0,
    }

    private enum NameId : ushort
    {
        CopyrightNotice = 0,
        FamilyName = 1,
        SubfamilyName = 2,
        UniqueId = 3,
        FullFontName = 4,
        VersionString = 5,
        PostScriptName = 6,
        Trademark = 7,
        Manufacturer = 8,
        Designer = 9,
        Description = 10,
        UrlVendor = 11,
        UrlDesigner = 12,
        LicenseDescription = 13,
        LicenseInfoUrl = 14,
        TypographicFamilyName = 16,
        TypographicSubfamilyName = 17,
        CompatibleFullMac = 18,
        SampleText = 19,
        PoscSriptCidFindFontName = 20,
        WwsFamilyName = 21,
        WwsSubfamilyName = 22,
        LightBackgroundPalette = 23,
        DarkBackgroundPalette = 24,
        VariationPostScriptNamePrefix = 25,
    }

    private enum PlatformId : ushort
    {
        Unicode = 0,
        Macintosh = 1, // discouraged
        Iso = 2,       // deprecated
        Windows = 3,
        Custom = 4, // OTF Windows NT compatibility mapping
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Version name in enum value names")]
    private enum UnicodeEncodingId : ushort
    {
        Unicode_1_0 = 0,  // deprecated
        Unicode_1_1 = 1,  // deprecated
        IsoIec_10646 = 2, // deprecated
        Unicode_2_0_Bmp = 3,
        Unicode_2_0_Full = 4,
        UnicodeVariationSequences = 5,
        UnicodeFullRepertoire = 6,
    }

    private enum WindowsEncodingId : ushort
    {
        Symbol = 0,
        UnicodeBmp = 1,
        ShiftJis = 2,
        Prc = 3,
        Big5 = 4,
        Wansung = 5,
        Johab = 6,
        UnicodeFullRepertoire = 10,
    }
}
