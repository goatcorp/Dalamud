using System.Numerics;

using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>Represents a render state.</summary>
public struct RenderState
{
    /// <summary>The first drawing screen offset.</summary>
    /// <remarks>This is an offset obtained from <see cref="ImGui.GetCursorScreenPos"/>.</remarks>
    public Vector2 StartScreenOffset;

    /// <summary>The final drawing relative offset.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 Offset;

    /// <summary>The left top relative offset of the text rendered so far.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 BoundsLeftTop;

    /// <summary>The right bottom relative offset of the text rendered so far.</summary>
    /// <remarks>Relativity begins from the cursor position at the construction of this struct.</remarks>
    public Vector2 BoundsRightBottom;

    /// <summary>The index of the last line, including new lines from word wrapping.</summary>
    public int LastLineIndex;

    /// <summary>The mouse button that has been clicked.</summary>
    /// <remarks>As <c>0</c> is <see cref="ImGuiMouseButton.Left"/>, if no mouse button is detected clicked, then it
    /// will be set to an invalid enum value.</remarks>
    public ImGuiMouseButton ClickedMouseButton;

    /// <summary>The final spannable param state.</summary>
    internal SpanStyle LastStyle;

    /// <summary>The latest measurement.</summary>
    internal MeasuredLine LastMeasurement;
}
