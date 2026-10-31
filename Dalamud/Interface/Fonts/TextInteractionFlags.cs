namespace Dalamud.Interface.Fonts;

/// <summary>Interactions with links of styled text.</summary>
/// <seealso cref="TextInteraction"/>
[Flags]
public enum TextInteractionFlags
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>The mouse is over the link.</summary>
    Hover = 1 << 0,

    /// <summary>Clicked with the left button.</summary>
    LeftClick = 1 << 1,

    /// <summary>Clicked with the right button.</summary>
    RightClick = 1 << 2,

    /// <summary>Clicked with the middle button.</summary>
    MiddleClick = 1 << 3,

    /// <summary>Clicked with the first extra button.</summary>
    X1Click = 1 << 4,

    /// <summary>Clicked with the second extra button.</summary>
    X2Click = 1 << 5,

    /// <summary>Clicked with any button.</summary>
    AnyClick = LeftClick | RightClick | MiddleClick | X1Click | X2Click,

    /// <summary>All.</summary>
    All = Hover | AnyClick,
}
