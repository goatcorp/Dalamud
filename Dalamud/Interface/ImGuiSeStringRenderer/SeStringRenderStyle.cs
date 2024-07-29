using ImGuiNET;

using Lumina.Text.Payloads;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Render styles for a SeString.</summary>
public record struct SeStringRenderStyle
{
    private uint color;
    private uint edgeColor;
    private uint shadowColor;
    private uint linkHoverColor;
    private uint linkActiveColor;
    private bool hasValidColorValue;
    private bool hasValidEdgeColorValue;
    private bool hasValidShadowColorValue;
    private bool hasValidLinkHoverColorValue;
    private bool hasValidLinkActiveColorValue;

    /// <summary>Gets or sets a value indicating whether to force the color of the rendered text edge.</summary>
    /// <remarks>If not <c>null</c>, then <see cref="MacroCode.EdgeColor"/> and <see cref="MacroCode.EdgeColorType"/>
    /// will be ignored.</remarks>
    public bool ForceEdgeColor { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered bold.</summary>
    public bool Bold { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered with edge.</summary>
    public bool Edge { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is rendered with shadow.</summary>
    public bool Shadow { get; set; }

    /// <summary>Gets or sets a value indicating whether to underline links.</summary>
    public bool LinkUnderline { get; set; }

    /// <summary>Gets or sets the color of the rendered text.</summary>
    public uint Color
    {
        readonly get => this.hasValidColorValue ? this.color : ImGui.GetColorU32(ImGuiCol.Text);
        set => (this.hasValidColorValue, this.color) = (true, value);
    }

    /// <summary>Gets or sets the color of the rendered text edge.</summary>
    public uint EdgeColor
    {
        readonly get => this.hasValidEdgeColorValue ? this.edgeColor : 0xFF000000;
        set => (this.hasValidEdgeColorValue, this.edgeColor) = (true, value);
    }

    /// <summary>Gets or sets the color of the rendered text shadow.</summary>
    public uint ShadowColor
    {
        readonly get => this.hasValidShadowColorValue ? this.shadowColor : 0xFF000000;
        set => (this.hasValidShadowColorValue, this.shadowColor) = (true, value);
    }

    /// <summary>Gets or sets the background color of a link when hovered.</summary>
    public uint LinkHoverColor
    {
        readonly get => this.hasValidLinkHoverColorValue
                            ? this.linkHoverColor
                            : ImGui.GetColorU32(ImGuiCol.ButtonHovered);
        set => (this.hasValidLinkHoverColorValue, this.linkHoverColor) = (true, value);
    }

    /// <summary>Gets or sets the background color of a link when active.</summary>
    public uint LinkActiveColor
    {
        readonly get => this.hasValidLinkActiveColorValue
                            ? this.linkActiveColor
                            : ImGui.GetColorU32(ImGuiCol.ButtonActive);
        set => (this.hasValidLinkActiveColorValue, this.linkActiveColor) = (true, value);
    }
}
