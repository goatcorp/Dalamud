namespace Dalamud.Interface.Fonts;

/// <summary>Interaction state of a link in styled text.</summary>
/// <seealso cref="TextLinkContext.State"/>
public enum TextLinkState
{
    /// <summary>Neither hovered nor held. Queried during layout and rendering.</summary>
    Normal,

    /// <summary>Hovered by the mouse without being held down. Queried during rendering.</summary>
    Hovered,

    /// <summary>Pressed and held down. Queried during rendering.</summary>
    Held,
}
