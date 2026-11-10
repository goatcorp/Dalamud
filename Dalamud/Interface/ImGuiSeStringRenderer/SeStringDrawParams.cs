using System.Numerics;

using Dalamud.Bindings.ImGui;

using Lumina.Text.Payloads;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Render styles for a SeString.</summary>
public record struct SeStringDrawParams
{
    /// <summary>Gets or sets the target draw list.</summary>
    /// <value>Target draw list, <c>default(ImDrawListPtr)</c> to not draw, or <c>null</c> to use
    /// <see cref="ImGui.GetWindowDrawList"/> (the default).</value>
    /// <remarks>
    /// If this value is set, <see cref="ImGui.Dummy"/> will not be called, and ImGui ID will be ignored.
    /// You <b>must</b> specify a valid draw list, a valid font via <see cref="Font"/> and <see cref="FontSize"/> if you set this value,
    /// since the renderer will not be able to retrieve them from ImGui context.
    /// Must be set when drawing off the main thread.
    /// </remarks>
    public ImDrawListPtr? TargetDrawList { get; set; }

    /// <summary>Gets or sets the function to be called on every codepoint and payload for the purpose of offering
    /// chances to draw something else instead of glyphs or SeString payload entities.</summary>
    public SeStringReplacementEntity.GetEntityDelegate? GetEntity { get; set; }

    /// <summary>Gets or sets the screen offset of the left top corner.</summary>
    /// <value>Screen offset to draw at, or <c>null</c> to use <see cref="ImGui.GetCursorScreenPos()"/>, if no <see cref="TargetDrawList"/>
    /// is specified. Otherwise, you must specify it (for example, by passing <see cref="ImGui.GetCursorScreenPos()"/> when passing the window
    /// draw list.</value>
    public Vector2? ScreenOffset { get; set; }

    /// <summary>Gets or sets the font to use.</summary>
    /// <value>Font to use, or <c>null</c> to use <see cref="ImGui.GetFont"/> (the default).</value>
    /// <remarks>Must be set when specifying a target draw-list or drawing off the main thread.</remarks>
    public ImFontPtr? Font { get; set; }

    /// <summary>Gets or sets the font size.</summary>
    /// <value>Font size in pixels, or <c>0</c> to use the current ImGui font size <see cref="ImGui.GetFontSize"/>.
    /// </value>
    /// <remarks>Must be set when specifying a target draw-list or drawing off the main thread.</remarks>
    public float? FontSize { get; set; }

    /// <summary>Gets or sets the line height ratio.</summary>
    /// <value><c>1</c> or <c>null</c> (the default) will use <see cref="FontSize"/> as the line height.
    /// <c>2</c> will make line height twice the <see cref="FontSize"/>.</value>
    public float? LineHeight { get; set; }

    /// <summary>Gets or sets the wrapping width.</summary>
    /// <value>Width in pixels, or <c>null</c> to wrap at the end of available content region from
    /// <see cref="ImGui.GetContentRegionAvail()"/> (the default).</value>
    public float? WrapWidth { get; set; }

    /// <summary>Gets or sets the thickness of underline under links.</summary>
    public float? LinkUnderlineThickness { get; set; }

    /// <summary>Gets or sets the opacity, commonly called &quot;alpha&quot;.</summary>
    /// <value>Opacity value ranging from 0(invisible) to 1(fully visible), or <c>null</c> to use the current ImGui
    /// opacity from <see cref="ImGuiStyle.Alpha"/> accessed using <see cref="ImGui.GetStyle"/>.</value>
    public float? Opacity { get; set; }

    /// <summary>Gets or sets the strength of the edge, which will have effects on the edge opacity.</summary>
    /// <value>Strength value ranging from 0(invisible) to 1(fully visible), or <c>null</c> to use the default value
    /// of <c>0.25f</c> that might be subject to change in the future.</value>
    public float? EdgeStrength { get; set; }

    /// <summary>Gets or sets the theme that will decide the colors to use for <see cref="MacroCode.ColorType"/>
    /// and <see cref="MacroCode.EdgeColorType"/>.</summary>
    /// <value><c>0</c> to use colors for Dark theme, <c>1</c> to use colors for Light theme, <c>2</c> to use colors
    /// for Classic FF theme, <c>3</c> to use colors for Clear Blue theme, or <c>null</c> to use the theme set from the
    /// game configuration.</value>
    public int? ThemeIndex { get; set; }

    /// <summary>Gets or sets the color of the rendered text.</summary>
    /// <value>Color in RGBA, or <c>null</c> to use <see cref="ImGuiCol.Text"/> (the default).</value>
    public uint? Color { get; set; }

    /// <summary>Gets or sets the color of the rendered text edge.</summary>
    /// <value>Color in RGBA, or <c>null</c> to use opaque black (the default).</value>
    public uint? EdgeColor { get; set; }

    /// <summary>Gets or sets the color of the rendered text shadow.</summary>
    /// <value>Color in RGBA, or <c>null</c> to use opaque black (the default).</value>
    public uint? ShadowColor { get; set; }

    /// <summary>Gets or sets the background color of a link when hovered.</summary>
    /// <value>Color in RGBA, or <c>null</c> to use <see cref="ImGuiCol.ButtonHovered"/> (the default).</value>
    public uint? LinkHoverBackColor { get; set; }

    /// <summary>Gets or sets the background color of a link when active.</summary>
    /// <value>Color in RGBA, or <c>null</c> to use <see cref="ImGuiCol.ButtonActive"/> (the default).</value>
    public uint? LinkActiveBackColor { get; set; }

    /// <summary>Gets or sets a value indicating whether to force the color of the rendered text edge.</summary>
    /// <remarks>If set, then <see cref="MacroCode.EdgeColor"/> and <see cref="MacroCode.EdgeColorType"/> will be
    /// ignored.</remarks>
    public bool ForceEdgeColor { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered bold.</summary>
    public bool Bold { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered with edge.</summary>
    /// <remarks>If an edge color is pushed with <see cref="MacroCode.EdgeColor"/> or
    /// <see cref="MacroCode.EdgeColorType"/>, it will be drawn regardless. Set <see cref="ForceEdgeColor"/> to
    /// <c>true</c> and set <see cref="EdgeColor"/> to <c>0</c> to fully disable edge.</remarks>
    public bool Edge { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered with shadow.</summary>
    public bool Shadow { get; set; }

    /// <summary>Gets the effective font.</summary>
    internal readonly unsafe ImFont* EffectiveFont =>
        (this.Font ?? ImGui.GetFont()) is var f && f.Handle is not null
            ? f.Handle
            : throw new ArgumentException("Specified font is empty.");

    /// <summary>Gets the effective line height in pixels.</summary>
    internal readonly float EffectiveLineHeight => (this.FontSize ?? ImGui.GetFontSize()) * (this.LineHeight ?? 1f);

    /// <summary>Gets the effective opacity.</summary>
    internal readonly float EffectiveOpacity => this.Opacity ?? ImGui.GetStyle().Alpha;
}
