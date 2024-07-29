using ImGuiNET;

using Lumina.Text.Payloads;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Render styles for a SeString.</summary>
public record struct SeStringRenderStyle
{
    private uint color;
    private uint edgeColor;
    private uint shadowColor;
    private bool hasValidColorValue;
    private bool hasValidEdgeColorValue;
    private bool hasValidShadowColorValue;

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
}
