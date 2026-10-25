namespace Dalamud.Interface.Fonts;

/// <summary>How a line of a text is drawn.</summary>
/// <seealso cref="TextLineDecoration"/>
public enum TextLineStyle : byte
{
    /// <summary>None.</summary>
    None,

    /// <summary>Solid, as in <c>---------------</c>.</summary>
    Solid,

    /// <summary>Dotted, as in <c>. . . . . . . .</c>.</summary>
    Dotted,

    /// <summary>Dashed, as in <c>-- -- -- -- --</c>.</summary>
    Dashed,

    /// <summary>Wavy, as in <c>^v^v^v^v^v^v^v</c>.</summary>
    Wavy,
}
