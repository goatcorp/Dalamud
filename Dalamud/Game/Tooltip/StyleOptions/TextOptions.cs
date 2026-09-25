using Dalamud.Game.Tooltip.TooltipArgTypes;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Common.Math;

namespace Dalamud.Game.Tooltip.StyleOptions;

/// <summary>
/// Object representing various properties of a <see cref="TextNode"/> for use with <see cref="TooltipArgs.AddText"/>.
/// </summary>
public class TextOptions
{
    /// <summary>
    /// Gets or sets the text color.
    /// </summary>
    /// <remarks>
    /// Expects a value between 0.0f and 1.0f.
    /// </remarks>
    public Vector4 TextColor { get; set; } = NativeThemeColorHelper.GetColor(1);

    /// <summary>
    /// Gets or sets the text outline color.
    /// </summary>
    /// <remarks>
    /// Expects a value between 0.0f and 1.0f.
    /// </remarks>
    public Vector4 TextOutlineColor { get; set; } = NativeThemeColorHelper.GetColor(7);

    /// <summary>
    /// Gets or sets the text alignment.
    /// </summary>
    public AlignmentType TextAlignment { get; set; } = AlignmentType.Left;

    /// <summary>
    /// Gets or sets the used font.
    /// </summary>
    public FontType FontType { get; set; } = FontType.Axis;

    /// <summary>
    /// Gets or sets the text flags. Defaults to WordWrap + MultiLine.
    /// </summary>
    /// <remarks>
    /// This is things like italics, multi-line, ellipsis.
    /// </remarks>
    public TextFlags TextFlags { get; set; } = TextFlags.WordWrap | TextFlags.MultiLine;

    /// <summary>
    /// Gets or sets the text size.
    /// </summary>
    public uint TextSize { get; set; } = 12;

    /// <summary>
    /// Gets or sets the spacing used for multiline, this needs to be slightly
    /// larger than <see cref="TextSize"/> to prevent the lines from bunching together.
    /// </summary>
    public uint LineSpacing { get; set; } = 14;

    /// <summary>
    /// Gets or sets the additional spacing used between characters.
    /// </summary>
    public uint CharacterSpacing { get; set; } = 0;
}
