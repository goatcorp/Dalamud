namespace Dalamud.Interface.Fonts;

/// <summary>A kind of text decoration.</summary>
/// <param name="Value">The value, or 0 for none.</param>
/// <remarks>Dalamud's decorations are also set through <see cref="TextStyle.Background"/> and
/// <see cref="TextStyle.Lines"/>.</remarks>
/// <seealso cref="TextDecoration"/>
public readonly record struct TextDecorationId(uint Value)
{
    /// <summary>The first ID assigned to plugin-registered decorations.</summary>
    public const uint FirstRegistered = 0x100;

    /// <summary>Gets the ID representing no decoration.</summary>
    public static TextDecorationId None => default;

    /// <summary>Gets the built-in background fill decoration ID.</summary>
    /// <remarks>The context is the color, and alpha 0 is no background.</remarks>
    /// <seealso cref="TextStyle.Background"/>
    public static TextDecorationId Background => new(1);

    /// <summary>Gets the built-in underline, overline, and strikethrough decoration ID.</summary>
    /// <seealso cref="TextLineDecoration"/>
    public static TextDecorationId Line => new(2);

    /// <summary>Gets a value indicating whether this is no decoration.</summary>
    public bool IsNone => this.Value == 0;

    /// <summary>Gets a value indicating whether this is one of Dalamud's decorations.</summary>
    public bool IsBuiltIn => this.Value is > 0 and < FirstRegistered;
}
