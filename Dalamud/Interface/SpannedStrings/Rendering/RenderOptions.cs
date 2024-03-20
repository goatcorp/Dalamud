using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Spannables;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.SpannedStrings.Rendering;

/// <summary>Initial options for <see cref="ISpannableRenderer"/>.</summary>
/// <remarks>Unspecified options (<c>null</c>) will use the default values.</remarks>
[SuppressMessage(
    "ReSharper",
    "PropertyCanBeMadeInitOnly.Global",
    Justification = "This is effectively an extensible set of function arguments")]
public record struct RenderOptions
{
    /// <summary>Gets or sets the accepted line break types.</summary>
    /// <remarks>Default value is <see cref="NewLineType.All"/>.</remarks>
    public NewLineType? AcceptedNewLines { get; set; }

    /// <summary>Gets or sets the design parameters for displaying the invisible control characters.</summary>
    /// <remarks><c>null</c> specifies that it should not be displayed.</remarks>
    public SpanStyle? ControlCharactersStyle { get; set; }

    /// <summary>Gets or sets the graphic font icon mode.</summary>
    /// <remarks>
    /// <para><c>null</c> or <c>-1</c> will use the one configured from the game configuration.</para>
    /// <para>Numbers outside the supported range will roll over.</para>
    /// </remarks>
    public int? GraphicFontIconMode { get; set; }

    /// <summary>Gets or sets the initial style.</summary>
    /// <remarks>Specifying <c>null</c> will use a default value that will only have
    /// <see cref="SpanStyle.ForeColor"/> set to <c>ImGui.GetColorU32(ImGuiCol.Text)</c> multiplied by
    /// <c>ImGui.GetStyle().Alpha</c>.</remarks>
    public SpanStyle? InitialStyle { get; set; }

    /// <summary>Gets or sets the maximum size at which point line break or ellipsis should happen.</summary>
    /// <remarks>Default value is <c>new Vector2(ImGui.GetColumnWidth(), float.MaxValue)</c>.</remarks>
    public Vector2? MaxSize { get; set; }

    /// <summary>Gets or sets the tab size.</summary>
    /// <remarks>
    /// <para>
    /// <c>0</c> will treat tab characters as a whitespace character.<br />
    /// <b>Positive values</b> indicate the width in pixels.<br />
    /// <b>Negative values</b> indicate the width in the number of whitespace characters, multiplied by -1.
    /// </para>
    /// <para>
    /// <c>null</c> will use the width of a whitespace character multiplied by 4.
    /// This equals to specifying <c>-4</c>.
    /// </para>
    /// </remarks>
    public float? TabWidth { get; set; }

    /// <summary>Gets or sets the scale.</summary>
    /// <remarks>Defaults to <see cref="ImGuiHelpers.GlobalScale"/>.</remarks>
    public float? Scale { get; set; }

    /// <summary>Gets or sets a value indicating whether to handle links.</summary>
    /// <remarks>Default value is to enable link handling.</remarks>
    public bool? UseLinks { get; set; }

    /// <summary>Gets or sets the word break mode.</summary>
    /// <remarks>Default value is <see cref="WordBreakType.Normal"/>.</remarks>
    public WordBreakType? WordBreak { get; set; }

    /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
    /// <remarks>
    /// <para>Default value is <c>null</c>, indicating that wrap markers are disabled.</para>
    /// <para>Currently not fully supported with <see cref="Transformation"/>.</para>
    /// </remarks>
    public ISpannable? WrapMarker { get; set; }

    /// <summary>Gets or sets the screen offset.</summary>
    /// <remarks>Default value is <c>null</c>, specifying that it will use <see cref="ImGui.GetCursorScreenPos"/>
    /// at the point of rendering.</remarks>
    public Vector2? ScreenOffset { get; set; }

    /// <summary>Gets or sets the transformation matrix.</summary>
    public Matrix4x4? Transformation { get; set; }
}
