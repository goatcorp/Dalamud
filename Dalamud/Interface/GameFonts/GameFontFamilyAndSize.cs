namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Enum of available game fonts in specific sizes.
/// </summary>
public enum GameFontFamilyAndSize
{
    /// <summary>
    /// Placeholder meaning unused.
    /// </summary>
    Undefined,

    /// <summary>
    /// AXIS (9.6pt)
    ///
    /// Contains Japanese characters in addition to Latin characters. Used in game for the whole UI.
    /// </summary>
    [GameFontFamilyAndSize("common/font/AXIS_96.fdt", "common/font/font{0}.tex", -1)]
    Axis96,

    /// <summary>
    /// AXIS (12pt)
    ///
    /// Contains Japanese characters in addition to Latin characters. Used in game for the whole UI.
    /// </summary>
    [GameFontFamilyAndSize("common/font/AXIS_12.fdt", "common/font/font{0}.tex", -1)]
    Axis12,

    /// <summary>
    /// AXIS (14pt)
    ///
    /// Contains Japanese characters in addition to Latin characters. Used in game for the whole UI.
    /// </summary>
    [GameFontFamilyAndSize("common/font/AXIS_14.fdt", "common/font/font{0}.tex", -1)]
    Axis14,

    /// <summary>
    /// AXIS (18pt)
    ///
    /// Contains Japanese characters in addition to Latin characters. Used in game for the whole UI.
    /// </summary>
    [GameFontFamilyAndSize("common/font/AXIS_18.fdt", "common/font/font{0}.tex", -1)]
    Axis18,

    /// <summary>
    /// AXIS (36pt)
    ///
    /// Contains Japanese characters in addition to Latin characters. Used in game for the whole UI.
    /// </summary>
    [GameFontFamilyAndSize("common/font/AXIS_36.fdt", "common/font/font{0}.tex", -4)]
    Axis36,

    /// <summary>
    /// Jupiter (16pt)
    ///
    /// Serif font. Contains mostly ASCII range. Used in game for job names.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_16.fdt", "common/font/font{0}.tex", -1)]
    Jupiter16,

    /// <summary>
    /// Jupiter (20pt)
    ///
    /// Serif font. Contains mostly ASCII range. Used in game for job names.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_20.fdt", "common/font/font{0}.tex", -1)]
    Jupiter20,

    /// <summary>
    /// Jupiter (23pt)
    ///
    /// Serif font. Contains mostly ASCII range. Used in game for job names.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_23.fdt", "common/font/font{0}.tex", -1)]
    Jupiter23,

    /// <summary>
    /// Jupiter (45pt)
    ///
    /// Serif font. Contains mostly numbers. Used in game for flying texts.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_45.fdt", "common/font/font{0}.tex", -2)]
    Jupiter45,

    /// <summary>
    /// Jupiter (46pt)
    ///
    /// Serif font. Contains mostly ASCII range. Used in game for job names.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_46.fdt", "common/font/font{0}.tex", -2)]
    Jupiter46,

    /// <summary>
    /// Jupiter (90pt)
    ///
    /// Serif font. Contains mostly numbers. Used in game for flying texts.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Jupiter_90.fdt", "common/font/font{0}.tex", -4)]
    Jupiter90,

    /// <summary>
    /// Meidinger (16pt)
    ///
    /// Horizontally wide. Contains mostly numbers. Used in game for HP/MP/IL stuff.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Meidinger_16.fdt", "common/font/font{0}.tex", -1)]
    Meidinger16,

    /// <summary>
    /// Meidinger (20pt)
    ///
    /// Horizontally wide. Contains mostly numbers. Used in game for HP/MP/IL stuff.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Meidinger_20.fdt", "common/font/font{0}.tex", -1)]
    Meidinger20,

    /// <summary>
    /// Meidinger (40pt)
    ///
    /// Horizontally wide. Contains mostly numbers. Used in game for HP/MP/IL stuff.
    /// </summary>
    [GameFontFamilyAndSize("common/font/Meidinger_40.fdt", "common/font/font{0}.tex", -4)]
    Meidinger40,

    /// <summary>
    /// MiedingerMid (10pt)
    ///
    /// Horizontally wide. Contains mostly ASCII range.
    /// </summary>
    [GameFontFamilyAndSize("common/font/MiedingerMid_10.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid10,

    /// <summary>
    /// MiedingerMid (12pt)
    ///
    /// Horizontally wide. Contains mostly ASCII range.
    /// </summary>
    [GameFontFamilyAndSize("common/font/MiedingerMid_12.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid12,

    /// <summary>
    /// MiedingerMid (14pt)
    ///
    /// Horizontally wide. Contains mostly ASCII range.
    /// </summary>
    [GameFontFamilyAndSize("common/font/MiedingerMid_14.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid14,

    /// <summary>
    /// MiedingerMid (18pt)
    ///
    /// Horizontally wide. Contains mostly ASCII range.
    /// </summary>
    [GameFontFamilyAndSize("common/font/MiedingerMid_18.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid18,

    /// <summary>
    /// MiedingerMid (36pt)
    ///
    /// Horizontally wide. Contains mostly ASCII range.
    /// </summary>
    [GameFontFamilyAndSize("common/font/MiedingerMid_36.fdt", "common/font/font{0}.tex", -2)]
    MiedingerMid36,

    /// <summary>
    /// TrumpGothic (18.4pt)
    ///
    /// Horizontally narrow. Contains mostly ASCII range. Used for addon titles.
    /// </summary>
    [GameFontFamilyAndSize("common/font/TrumpGothic_184.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic184,

    /// <summary>
    /// TrumpGothic (23pt)
    ///
    /// Horizontally narrow. Contains mostly ASCII range. Used for addon titles.
    /// </summary>
    [GameFontFamilyAndSize("common/font/TrumpGothic_23.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic23,

    /// <summary>
    /// TrumpGothic (34pt)
    ///
    /// Horizontally narrow. Contains mostly ASCII range. Used for addon titles.
    /// </summary>
    [GameFontFamilyAndSize("common/font/TrumpGothic_34.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic34,

    /// <summary>
    /// TrumpGothic (688pt)
    ///
    /// Horizontally narrow. Contains mostly ASCII range. Used for addon titles.
    /// </summary>
    [GameFontFamilyAndSize("common/font/TrumpGothic_68.fdt", "common/font/font{0}.tex", -3)]
    TrumpGothic68,
}
