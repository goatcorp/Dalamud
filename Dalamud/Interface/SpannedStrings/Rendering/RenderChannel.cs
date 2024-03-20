namespace Dalamud.Interface.SpannedStrings.Rendering;

/// <summary>The channel to render to.</summary>
public enum RenderChannel
{
    /// <summary>The background channel.</summary>
    BackChannel,

    /// <summary>The shadow channel.</summary>
    ShadowChannel,

    /// <summary>The border channel.</summary>
    BorderChannel,

    /// <summary>The text decoration channel.</summary>
    TextDecorationOverUnderChannel,

    /// <summary>The foreground channel.</summary>
    ForeChannel,

    /// <summary>The text decoration channel.</summary>
    TextDecorationThroughChannel,

    /// <summary>The number of channels.</summary>
    /// <remarks>Not a channel.</remarks>
    Count,
}
