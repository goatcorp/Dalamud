namespace Dalamud.Interface.Fonts;

/// <summary>What is happening to a link of styled text being drawn.</summary>
/// <seealso cref="TextLinkContext.State"/>
public enum TextLinkState
{
    /// <summary>Nothing. Asked as the layout is made.</summary>
    Normal,

    /// <summary>The mouse is over the link. Asked as the text is drawn.</summary>
    Hovered,

    /// <summary>The link is held down. Asked as the text is drawn.</summary>
    Held,
}
