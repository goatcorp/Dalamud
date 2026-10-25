namespace Dalamud.Interface.Fonts;

/// <summary>A decoration of text.</summary>
/// <param name="Id">The kind of decoration. Dalamud gives ids to the decorations that plugins register.</param>
/// <param name="Context">The color for the built-in decorations.</param>
/// <param name="Data">The packed line bits for the built-in line decoration.</param>
/// <remarks>The meaning of both <paramref name="Context"/> and <paramref name="Data"/> is decoration-dependent.
/// </remarks>
/// <seealso cref="TextStyle.ExtraDecoration"/>
public readonly record struct TextDecoration(TextDecorationId Id, uint Context = 0, ulong Data = 0)
{
    /// <summary>Gets a value indicating whether there is no decoration.</summary>
    public bool IsNone => this.Id.IsNone;
}
