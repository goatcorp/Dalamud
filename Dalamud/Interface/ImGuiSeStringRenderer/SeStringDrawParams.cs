using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

using ImGuiNET;

using Lumina.Text.Payloads;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Render styles for a SeString.</summary>
public record struct SeStringDrawParams
{
    /// <summary>Gets or sets the target draw list.</summary>
    /// <value>Target draw list, <c>default(ImDrawListPtr)</c> to not draw, or <c>null</c> to use
    /// <see cref="ImGui.GetWindowDrawList"/> (the default).</value>
    /// <remarks>If this value is set, <see cref="ImGui.Dummy"/> will not be called, and ImGui ID will be ignored.
    /// </remarks>
    public ImDrawListPtr? TargetDrawList { get; set; }

    /// <summary>Gets or sets the font to use.</summary>
    /// <value>Font to use, or <c>null</c> to use <see cref="ImGui.GetFont"/> (the default).</value>
    public ImFontPtr? Font { get; set; }

    /// <summary>Gets or sets the screen offset of the left top corner.</summary>
    /// <value>Screen offset to draw at, or <c>null</c> to use <see cref="ImGui.GetCursorScreenPos"/>.</value>
    public Vector2? ScreenOffset { get; set; }

    /// <summary>Gets or sets the font size.</summary>
    /// <value>Font size in pixels, or <c>0</c> to use the current ImGui font size <see cref="ImGui.GetFontSize"/>.
    /// </value>
    public float? FontSize { get; set; }

    /// <summary>Gets or sets the line height ratio.</summary>
    /// <value><c>1</c> or <c>null</c> (the default) will use <see cref="FontSize"/> as the line height.
    /// <c>2</c> will make line height twice the <see cref="FontSize"/>.</value>
    public float? LineHeight { get; set; }

    /// <summary>Gets or sets the wrapping width.</summary>
    /// <value>Width in pixels, or <c>null</c> to wrap at the end of available content region from
    /// <see cref="ImGui.GetContentRegionAvail"/> (the default).</value>
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
    public bool Edge { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered with shadow.</summary>
    public bool Shadow { get; set; }

    private readonly unsafe ImFont* EffectiveFont =>
        (this.Font ?? ImGui.GetFont()) is var f && f.NativePtr is not null
            ? f.NativePtr
            : throw new ArgumentException("Specified font is empty.");

    private readonly float EffectiveLineHeight => (this.FontSize ?? ImGui.GetFontSize()) * (this.LineHeight ?? 1f);

    private readonly float EffectiveOpacity => this.Opacity ?? ImGui.GetStyle().Alpha;

    /// <summary>Calculated values from <see cref="SeStringDrawParams"/> using ImGui styles.</summary>
    [SuppressMessage(
        "StyleCop.CSharp.OrderingRules",
        "SA1214:Readonly fields should appear before non-readonly fields",
        Justification = "Matching the above order.")]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct Resolved(in SeStringDrawParams ssdp)
    {
        /// <inheritdoc cref="SeStringDrawParams.TargetDrawList"/>
        public readonly ImDrawList* DrawList = ssdp.TargetDrawList ?? ImGui.GetWindowDrawList();

        /// <inheritdoc cref="SeStringDrawParams.Font"/>
        public readonly ImFont* Font = ssdp.EffectiveFont;

        /// <inheritdoc cref="SeStringDrawParams.ScreenOffset"/>
        public readonly Vector2 ScreenOffset = ssdp.ScreenOffset ?? ImGui.GetCursorScreenPos();

        /// <inheritdoc cref="SeStringDrawParams.FontSize"/>
        public readonly float FontSize = ssdp.FontSize ?? ImGui.GetFontSize();

        /// <inheritdoc cref="SeStringDrawParams.LineHeight"/>
        public readonly float LineHeight = MathF.Round(ssdp.EffectiveLineHeight);

        /// <inheritdoc cref="SeStringDrawParams.WrapWidth"/>
        public readonly float WrapWidth = ssdp.WrapWidth ?? ImGui.GetContentRegionAvail().X;

        /// <inheritdoc cref="SeStringDrawParams.LinkUnderlineThickness"/>
        public readonly float LinkUnderlineThickness = ssdp.LinkUnderlineThickness ?? 0f;

        /// <inheritdoc cref="SeStringDrawParams.Opacity"/>
        public readonly float Opacity = ssdp.EffectiveOpacity;

        /// <inheritdoc cref="SeStringDrawParams.EdgeStrength"/>
        public readonly float EdgeOpacity = (ssdp.EdgeStrength ?? 0.25f) * ssdp.EffectiveOpacity;

        /// <inheritdoc cref="SeStringDrawParams.Color"/>
        public uint Color = ssdp.Color ?? ImGui.GetColorU32(ImGuiCol.Text);

        /// <inheritdoc cref="SeStringDrawParams.EdgeColor"/>
        public uint EdgeColor = ssdp.EdgeColor ?? 0xFF000000;

        /// <inheritdoc cref="SeStringDrawParams.ShadowColor"/>
        public uint ShadowColor = ssdp.ShadowColor ?? 0xFF000000;

        /// <inheritdoc cref="SeStringDrawParams.LinkHoverBackColor"/>
        public readonly uint LinkHoverBackColor = ssdp.LinkHoverBackColor ?? ImGui.GetColorU32(ImGuiCol.ButtonHovered);

        /// <inheritdoc cref="SeStringDrawParams.LinkActiveBackColor"/>
        public readonly uint LinkActiveBackColor = ssdp.LinkActiveBackColor ?? ImGui.GetColorU32(ImGuiCol.ButtonActive);

        /// <inheritdoc cref="SeStringDrawParams.ForceEdgeColor"/>
        public readonly bool ForceEdgeColor = ssdp.ForceEdgeColor;

        /// <inheritdoc cref="SeStringDrawParams.Bold"/>
        public bool Bold = ssdp.Bold;

        /// <inheritdoc cref="SeStringDrawParams.Italic"/>
        public bool Italic = ssdp.Italic;

        /// <inheritdoc cref="SeStringDrawParams.Edge"/>
        public bool Edge = ssdp.Edge;

        /// <inheritdoc cref="SeStringDrawParams.Shadow"/>
        public bool Shadow = ssdp.Shadow;
    }
}
