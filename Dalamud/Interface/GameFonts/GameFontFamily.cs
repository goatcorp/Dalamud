namespace Dalamud.Interface.GameFonts;

/// <summary>
/// Enum of available game font families.
/// </summary>
public enum GameFontFamily
{
    /// <summary>
    /// Placeholder meaning unused.
    /// </summary>
    Undefined,

    /// <summary>
    /// Sans-serif fonts used for the whole UI. Contains Japanese characters in addition to Latin characters.
    /// </summary>
    Axis,

    /// <summary>
    /// Serif fonts used for job names. Contains Latin characters.
    /// </summary>
    Jupiter,

    /// <summary>
    /// Digit-only serif fonts used for flying texts. Contains numbers.
    /// </summary>
    JupiterNumeric,

    /// <summary>
    /// Digit-only sans-serif horizontally wide fonts used for HP/MP/IL numbers.
    /// </summary>
    Meidinger,

    /// <summary>
    /// Sans-serif horizontally wide font used for names of gauges. Contains Latin characters.
    /// </summary>
    MiedingerMid,

    /// <summary>
    /// Sans-serif horizontally narrow font used for addon titles. Contains Latin characters.
    /// </summary>
    TrumpGothic,
}
