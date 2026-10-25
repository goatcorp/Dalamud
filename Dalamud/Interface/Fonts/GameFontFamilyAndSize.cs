using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>Enum of available game fonts in specific sizes.</summary>
public enum GameFontFamilyAndSize
{
    /// <summary>Placeholder meaning unused.</summary>
    Undefined,

    /// <summary>AXIS at 9.6pt.</summary>
    /// <seealso cref="GameFontFamily.Axis"/>
    [GameFontFamilyAndSize("common/font/AXIS_96.fdt", "common/font/font{0}.tex", -1)]
    Axis96,

    /// <summary>AXIS at 12pt.</summary>
    /// <seealso cref="GameFontFamily.Axis"/>
    [GameFontFamilyAndSize("common/font/AXIS_12.fdt", "common/font/font{0}.tex", -1)]
    Axis12,

    /// <summary>AXIS at 14pt.</summary>
    /// <seealso cref="GameFontFamily.Axis"/>
    [GameFontFamilyAndSize("common/font/AXIS_14.fdt", "common/font/font{0}.tex", -1)]
    Axis14,

    /// <summary>AXIS at 18pt.</summary>
    /// <seealso cref="GameFontFamily.Axis"/>
    [GameFontFamilyAndSize("common/font/AXIS_18.fdt", "common/font/font{0}.tex", -1)]
    Axis18,

    /// <summary>AXIS at 36pt.</summary>
    /// <seealso cref="GameFontFamily.Axis"/>
    [GameFontFamilyAndSize("common/font/AXIS_36.fdt", "common/font/font{0}.tex", -4)]
    Axis36,

    /// <summary>Jupiter at 16pt.</summary>
    /// <seealso cref="GameFontFamily.Jupiter"/>
    [GameFontFamilyAndSize("common/font/Jupiter_16.fdt", "common/font/font{0}.tex", -1)]
    Jupiter16,

    /// <summary>Jupiter at 20pt.</summary>
    /// <seealso cref="GameFontFamily.Jupiter"/>
    [GameFontFamilyAndSize("common/font/Jupiter_20.fdt", "common/font/font{0}.tex", -1)]
    Jupiter20,

    /// <summary>Jupiter at 23pt.</summary>
    /// <seealso cref="GameFontFamily.Jupiter"/>
    [GameFontFamilyAndSize("common/font/Jupiter_23.fdt", "common/font/font{0}.tex", -1)]
    Jupiter23,

    /// <summary>Jupiter at 45pt.</summary>
    /// <seealso cref="GameFontFamily.JupiterNumeric"/>
    [GameFontFamilyAndSize("common/font/Jupiter_45.fdt", "common/font/font{0}.tex", -2)]
    Jupiter45,

    /// <summary>Jupiter at 46pt.</summary>
    /// <seealso cref="GameFontFamily.Jupiter"/>
    [GameFontFamilyAndSize("common/font/Jupiter_46.fdt", "common/font/font{0}.tex", -2)]
    Jupiter46,

    /// <summary>Jupiter at 90pt.</summary>
    /// <seealso cref="GameFontFamily.JupiterNumeric"/>
    [GameFontFamilyAndSize("common/font/Jupiter_90.fdt", "common/font/font{0}.tex", -4)]
    Jupiter90,

    /// <summary>Meidinger at 16pt.</summary>
    /// <seealso cref="GameFontFamily.Meidinger"/>
    [GameFontFamilyAndSize("common/font/Meidinger_16.fdt", "common/font/font{0}.tex", -1)]
    Meidinger16,

    /// <summary>Meidinger at 20pt.</summary>
    /// <seealso cref="GameFontFamily.Meidinger"/>
    [GameFontFamilyAndSize("common/font/Meidinger_20.fdt", "common/font/font{0}.tex", -1)]
    Meidinger20,

    /// <summary>Meidinger at 40pt.</summary>
    /// <seealso cref="GameFontFamily.Meidinger"/>
    [GameFontFamilyAndSize("common/font/Meidinger_40.fdt", "common/font/font{0}.tex", -4)]
    Meidinger40,

    /// <summary>MiedingerMid at 10pt.</summary>
    /// <seealso cref="GameFontFamily.MiedingerMid"/>
    [GameFontFamilyAndSize("common/font/MiedingerMid_10.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid10,

    /// <summary>MiedingerMid at 12pt.</summary>
    /// <seealso cref="GameFontFamily.MiedingerMid"/>
    [GameFontFamilyAndSize("common/font/MiedingerMid_12.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid12,

    /// <summary>MiedingerMid at 14pt.</summary>
    /// <seealso cref="GameFontFamily.MiedingerMid"/>
    [GameFontFamilyAndSize("common/font/MiedingerMid_14.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid14,

    /// <summary>MiedingerMid at 18pt.</summary>
    /// <seealso cref="GameFontFamily.MiedingerMid"/>
    [GameFontFamilyAndSize("common/font/MiedingerMid_18.fdt", "common/font/font{0}.tex", -1)]
    MiedingerMid18,

    /// <summary>MiedingerMid at 36pt.</summary>
    /// <seealso cref="GameFontFamily.MiedingerMid"/>
    [GameFontFamilyAndSize("common/font/MiedingerMid_36.fdt", "common/font/font{0}.tex", -2)]
    MiedingerMid36,

    /// <summary>TrumpGothic at 18.4pt.</summary>
    /// <seealso cref="GameFontFamily.TrumpGothic"/>
    [GameFontFamilyAndSize("common/font/TrumpGothic_184.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic184,

    /// <summary>TrumpGothic at 23pt.</summary>
    /// <seealso cref="GameFontFamily.TrumpGothic"/>
    [GameFontFamilyAndSize("common/font/TrumpGothic_23.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic23,

    /// <summary>TrumpGothic at 34pt.</summary>
    /// <seealso cref="GameFontFamily.TrumpGothic"/>
    [GameFontFamilyAndSize("common/font/TrumpGothic_34.fdt", "common/font/font{0}.tex", -1)]
    TrumpGothic34,

    /// <summary>TrumpGothic at 68pt.</summary>
    /// <seealso cref="GameFontFamily.TrumpGothic"/>
    [GameFontFamilyAndSize("common/font/TrumpGothic_68.fdt", "common/font/font{0}.tex", -3)]
    TrumpGothic68,
}
