namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Predefined channels for drawing onto, for out-of-order drawing.</summary>
// Notes: values must be consecutively increasing, starting from 0. Higher values has higher priority.
public enum SeStringDrawChannel
{
    /// <summary>Next draw operation on the draw list will be put below <see cref="Background"/>.</summary>
    BelowBackground,

    /// <summary>Next draw operation on the draw list will be put onto the background channel.</summary>
    Background,

    /// <summary>Next draw operation on the draw list will be put above <see cref="Background"/>.</summary>
    AboveBackground,

    /// <summary>Next draw operation on the draw list will be put below <see cref="Shadow"/>.</summary>
    BelowShadow,

    /// <summary>Next draw operation on the draw list will be put onto the shadow channel.</summary>
    Shadow,

    /// <summary>Next draw operation on the draw list will be put above <see cref="Shadow"/>.</summary>
    AboveShadow,

    /// <summary>Next draw operation on the draw list will be put below <see cref="Edge"/>.</summary>
    BelowEdge,

    /// <summary>Next draw operation on the draw list will be put onto the edge channel.</summary>
    Edge,

    /// <summary>Next draw operation on the draw list will be put above <see cref="Edge"/>.</summary>
    AboveEdge,

    /// <summary>Next draw operation on the draw list will be put below <see cref="Foreground"/>.</summary>
    BelowForeground,

    /// <summary>Next draw operation on the draw list will be put onto the foreground channel.</summary>
    Foreground,

    /// <summary>Next draw operation on the draw list will be put above <see cref="Foreground"/>.</summary>
    AboveForeground,
}
