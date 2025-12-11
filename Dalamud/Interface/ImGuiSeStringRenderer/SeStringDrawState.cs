using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.ImGuiSeStringRenderer;

/// <summary>Calculated values from <see cref="SeStringDrawParams"/> using ImGui styles.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe ref struct SeStringDrawState : IDisposable
{
    private static readonly int ChannelCount = Enum.GetValues<SeStringDrawChannel>().Length;

    private readonly ImDrawList* drawList;

    private ImDrawListSplitter splitter;

    /// <summary>Initializes a new instance of the <see cref="SeStringDrawState"/> struct.</summary>
    /// <param name="span">Raw SeString byte span.</param>
    /// <param name="ssdp">Instance of <see cref="SeStringDrawParams"/> to initialize from.</param>
    /// <param name="colorStackSet">Instance of <see cref="SeStringColorStackSet"/> to use.</param>
    /// <param name="fragments">Fragments.</param>
    /// <param name="font">Font to use.</param>
    internal SeStringDrawState(
        ReadOnlySpan<byte> span,
        scoped in SeStringDrawParams ssdp,
        SeStringColorStackSet colorStackSet,
        List<TextFragment> fragments,
        ImFont* font)
    {
        this.Span = span;
        this.ColorStackSet = colorStackSet;
        this.Fragments = fragments;
        this.Font = font;

        if (ssdp.TargetDrawList is null)
        {
            if (!ThreadSafety.IsMainThread)
            {
                throw new ArgumentException(
                    $"{nameof(ssdp.TargetDrawList)} must be set to render outside the main thread.");
            }

            this.drawList = ssdp.TargetDrawList ?? ImGui.GetWindowDrawList();
            this.ScreenOffset = ssdp.ScreenOffset ?? ImGui.GetCursorScreenPos();
            this.FontSize = ssdp.FontSize ?? ImGui.GetFontSize();
            this.WrapWidth = ssdp.WrapWidth ?? ImGui.GetContentRegionAvail().X;
            this.Color = ssdp.Color ?? ImGui.GetColorU32(ImGuiCol.Text);
            this.LinkHoverBackColor = ssdp.LinkHoverBackColor ?? ImGui.GetColorU32(ImGuiCol.ButtonHovered);
            this.LinkActiveBackColor = ssdp.LinkActiveBackColor ?? ImGui.GetColorU32(ImGuiCol.ButtonActive);
            this.ThemeIndex = ssdp.ThemeIndex ?? AtkStage.Instance()->AtkUIColorHolder->ActiveColorThemeType;
        }
        else
        {
            this.drawList = ssdp.TargetDrawList.Value;
            this.ScreenOffset = ssdp.ScreenOffset ?? Vector2.Zero;

            // API14: Remove, always throw
            if (ThreadSafety.IsMainThread)
            {
                this.ScreenOffset = ssdp.ScreenOffset ?? ImGui.GetCursorScreenPos();
                this.FontSize = ssdp.FontSize ?? ImGui.GetFontSize();
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(ssdp.FontSize)} must be set when specifying a target draw list, as it cannot be fetched from the ImGui state.");
            }

            // this.FontSize = ssdp.FontSize ?? throw new ArgumentException(
            //                     $"{nameof(ssdp.FontSize)} must be set when specifying a target draw list, as it cannot be fetched from the ImGui state.");
            this.WrapWidth = ssdp.WrapWidth ?? float.MaxValue;
            this.Color = ssdp.Color ?? uint.MaxValue;
            this.LinkHoverBackColor = 0; // Interactivity is unused outside the main thread.
            this.LinkActiveBackColor = 0; // Interactivity is unused outside the main thread.
            this.ThemeIndex = ssdp.ThemeIndex ?? 0;
        }

        this.splitter = default;
        this.GetEntity = ssdp.GetEntity;
        this.ScreenOffset = new(MathF.Round(this.ScreenOffset.X), MathF.Round(this.ScreenOffset.Y));
        this.FontSizeScale = this.FontSize / this.Font->FontSize;
        this.LineHeight = MathF.Round(ssdp.EffectiveLineHeight);
        this.LinkUnderlineThickness = ssdp.LinkUnderlineThickness ?? 0f;
        this.Opacity = ssdp.EffectiveOpacity;
        this.EdgeOpacity = (ssdp.EdgeStrength ?? 0.25f) * ssdp.EffectiveOpacity;
        this.EdgeColor = ssdp.EdgeColor ?? 0xFF000000;
        this.ShadowColor = ssdp.ShadowColor ?? 0xFF000000;
        this.ForceEdgeColor = ssdp.ForceEdgeColor;
        this.Bold = ssdp.Bold;
        this.Italic = ssdp.Italic;
        this.Edge = ssdp.Edge;
        this.Shadow = ssdp.Shadow;

        this.ColorStackSet.Initialize(ref this);
        fragments.Clear();
    }

    /// <inheritdoc cref="SeStringDrawParams.TargetDrawList"/>
    public readonly ImDrawListPtr DrawList => new(this.drawList);

    /// <summary>Gets the raw SeString byte span.</summary>
    public ReadOnlySpan<byte> Span { get; }

    /// <inheritdoc cref="SeStringDrawParams.GetEntity"/>
    public SeStringReplacementEntity.GetEntityDelegate? GetEntity { get; }

    /// <inheritdoc cref="SeStringDrawParams.ScreenOffset"/>
    public Vector2 ScreenOffset { get; }

    /// <inheritdoc cref="SeStringDrawParams.Font"/>
    public ImFont* Font { get; }

    /// <inheritdoc cref="SeStringDrawParams.FontSize"/>
    public float FontSize { get; }

    /// <summary>Gets the multiplier value for glyph metrics, so that it scales to <see cref="FontSize"/>.</summary>
    /// <remarks>Multiplied to <see cref="ImGuiHelpers.ImFontGlyphReal.XY"/>,
    /// <see cref="ImGuiHelpers.ImFontGlyphReal.AdvanceX"/>, and distance values from
    /// <see cref="ImFontPtr.GetDistanceAdjustmentForPair"/>.</remarks>
    public float FontSizeScale { get; }

    /// <inheritdoc cref="SeStringDrawParams.LineHeight"/>
    public float LineHeight { get; }

    /// <inheritdoc cref="SeStringDrawParams.WrapWidth"/>
    public float WrapWidth { get; }

    /// <inheritdoc cref="SeStringDrawParams.LinkUnderlineThickness"/>
    public float LinkUnderlineThickness { get; }

    /// <inheritdoc cref="SeStringDrawParams.Opacity"/>
    public float Opacity { get; }

    /// <inheritdoc cref="SeStringDrawParams.EdgeStrength"/>
    public float EdgeOpacity { get; }

    /// <inheritdoc cref="SeStringDrawParams.ThemeIndex"/>
    public int ThemeIndex { get; }

    /// <inheritdoc cref="SeStringDrawParams.Color"/>
    public uint Color { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.EdgeColor"/>
    public uint EdgeColor { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.ShadowColor"/>
    public uint ShadowColor { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.LinkHoverBackColor"/>
    public uint LinkHoverBackColor { get; }

    /// <inheritdoc cref="SeStringDrawParams.LinkActiveBackColor"/>
    public uint LinkActiveBackColor { get; }

    /// <inheritdoc cref="SeStringDrawParams.ForceEdgeColor"/>
    public bool ForceEdgeColor { get; }

    /// <inheritdoc cref="SeStringDrawParams.Bold"/>
    public bool Bold { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.Italic"/>
    public bool Italic { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.Edge"/>
    public bool Edge { get; set; }

    /// <inheritdoc cref="SeStringDrawParams.Shadow"/>
    public bool Shadow { get; set; }

    /// <summary>Gets a value indicating whether the edge should be drawn.</summary>
    public readonly bool ShouldDrawEdge =>
        (this.Edge || this.ColorStackSet.HasAdditionalEdgeColor) && this.EdgeColor >= 0x1000000;

    /// <summary>Gets a value indicating whether the edge should be drawn.</summary>
    public readonly bool ShouldDrawShadow => this is { Shadow: true, ShadowColor: >= 0x1000000 };

    /// <summary>Gets a value indicating whether the edge should be drawn.</summary>
    public readonly bool ShouldDrawForeground => this is { Color: >= 0x1000000 };

    /// <summary>Gets the color stacks.</summary>
    internal SeStringColorStackSet ColorStackSet { get; }

    /// <summary>Gets the text fragments.</summary>
    internal List<TextFragment> Fragments { get; }

    /// <inheritdoc/>
    public void Dispose() => this.splitter.ClearFreeMemory();

    /// <summary>Sets the current channel in the ImGui draw list splitter.</summary>
    /// <param name="channelIndex">Channel to switch to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetCurrentChannel(SeStringDrawChannel channelIndex) =>
        this.splitter.SetCurrentChannel(this.drawList, (int)channelIndex);

    /// <summary>Draws a single texture.</summary>
    /// <param name="igTextureId">ImGui texture ID to draw from.</param>
    /// <param name="offset">Offset of the glyph in pixels w.r.t. <see cref="ScreenOffset"/>.</param>
    /// <param name="size">Right bottom corner of the glyph w.r.t. its glyph origin in the target draw list.</param>
    /// <param name="uv0">Left top corner of the glyph w.r.t. its glyph origin in the source texture.</param>
    /// <param name="uv1">Right bottom corner of the glyph w.r.t. its glyph origin in the source texture.</param>
    /// <param name="color">Color of the glyph in RGBA.</param>
    public readonly void Draw(
        ImTextureID igTextureId,
        Vector2 offset,
        Vector2 size,
        Vector2 uv0,
        Vector2 uv1,
        uint color = uint.MaxValue)
    {
        offset += this.ScreenOffset;
        this.DrawList.AddImageQuad(
            igTextureId,
            offset,
            offset + size with { X = 0 },
            offset + size,
            offset + size with { Y = 0 },
            new(uv0.X, uv0.Y),
            new(uv0.X, uv1.Y),
            new(uv1.X, uv1.Y),
            new(uv1.X, uv0.Y),
            color);
    }

    /// <summary>Draws a single texture.</summary>
    /// <param name="igTextureId">ImGui texture ID to draw from.</param>
    /// <param name="offset">Offset of the glyph in pixels w.r.t. <see cref="ScreenOffset"/>.</param>
    /// <param name="xy0">Left top corner of the glyph w.r.t. its glyph origin in the target draw list.</param>
    /// <param name="xy1">Right bottom corner of the glyph w.r.t. its glyph origin in the target draw list.</param>
    /// <param name="uv0">Left top corner of the glyph w.r.t. its glyph origin in the source texture.</param>
    /// <param name="uv1">Right bottom corner of the glyph w.r.t. its glyph origin in the source texture.</param>
    /// <param name="color">Color of the glyph in RGBA.</param>
    /// <param name="dyItalic">Transformation for <paramref name="xy0"/> and <paramref name="xy1"/> that will push
    ///     top and bottom pixels to apply faux italicization by <see cref="Vector2.X"/> and <see cref="Vector2.Y"/>
    ///     respectively.</param>
    public readonly void Draw(
        ImTextureID igTextureId,
        Vector2 offset,
        Vector2 xy0,
        Vector2 xy1,
        Vector2 uv0,
        Vector2 uv1,
        uint color = uint.MaxValue,
        Vector2 dyItalic = default)
    {
        offset += this.ScreenOffset;
        this.DrawList.AddImageQuad(
            igTextureId,
            offset + new Vector2(xy0.X + dyItalic.X, xy0.Y),
            offset + new Vector2(xy0.X + dyItalic.Y, xy1.Y),
            offset + new Vector2(xy1.X + dyItalic.Y, xy1.Y),
            offset + new Vector2(xy1.X + dyItalic.X, xy0.Y),
            new(uv0.X, uv0.Y),
            new(uv0.X, uv1.Y),
            new(uv1.X, uv1.Y),
            new(uv1.X, uv0.Y),
            color);
    }

    /// <summary>Draws a single glyph using current styling configurations.</summary>
    /// <param name="g">Glyph to draw.</param>
    /// <param name="offset">Offset of the glyph in pixels w.r.t. <see cref="ScreenOffset"/>.</param>
    internal void DrawGlyph(scoped in ImGuiHelpers.ImFontGlyphReal g, Vector2 offset)
    {
        var texId = this.Font->ContainerAtlas->Textures.Ref<ImFontAtlasTexture>(g.TextureIndex).TexID;
        var xy0 = new Vector2(
            MathF.Round(g.X0 * this.FontSizeScale),
            MathF.Round(g.Y0 * this.FontSizeScale));
        var xy1 = new Vector2(
            MathF.Round(g.X1 * this.FontSizeScale),
            MathF.Round(g.Y1 * this.FontSizeScale));
        var dxBold = this.Bold ? 2 : 1;
        var dyItalic = this.Italic
                           ? new Vector2(this.FontSize - xy0.Y, this.FontSize - xy1.Y) / 6
                           : Vector2.Zero;
        // Note: dyItalic values can be non-rounded; the glyph will be rendered sheared anyway.

        offset.Y += MathF.Round((this.LineHeight - this.FontSize) / 2f);

        if (this.ShouldDrawShadow)
        {
            this.SetCurrentChannel(SeStringDrawChannel.Shadow);
            for (var i = 0; i < dxBold; i++)
                this.Draw(texId, offset + new Vector2(i, 1), xy0, xy1, g.UV0, g.UV1, this.ShadowColor, dyItalic);
        }

        if (this.ShouldDrawEdge)
        {
            this.SetCurrentChannel(SeStringDrawChannel.Edge);

            // Top & Bottom
            for (var i = -1; i <= dxBold; i++)
            {
                this.Draw(texId, offset + new Vector2(i, -1), xy0, xy1, g.UV0, g.UV1, this.EdgeColor, dyItalic);
                this.Draw(texId, offset + new Vector2(i, 1), xy0, xy1, g.UV0, g.UV1, this.EdgeColor, dyItalic);
            }

            // Left & Right
            this.Draw(texId, offset + new Vector2(-1, 0), xy0, xy1, g.UV0, g.UV1, this.EdgeColor, dyItalic);
            this.Draw(texId, offset + new Vector2(1, 0), xy0, xy1, g.UV0, g.UV1, this.EdgeColor, dyItalic);
        }

        if (this.ShouldDrawForeground)
        {
            this.SetCurrentChannel(SeStringDrawChannel.Foreground);
            for (var i = 0; i < dxBold; i++)
                this.Draw(texId, offset + new Vector2(i, 0), xy0, xy1, g.UV0, g.UV1, this.Color, dyItalic);
        }
    }

    /// <summary>Draws an underline, for links.</summary>
    /// <param name="offset">Offset of the glyph in pixels w.r.t.
    /// <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
    /// <param name="advanceWidth">Advance width of the glyph.</param>
    internal void DrawLinkUnderline(Vector2 offset, float advanceWidth)
    {
        if (this.LinkUnderlineThickness < 1f)
            return;

        offset += this.ScreenOffset;
        offset.Y += (this.LinkUnderlineThickness - 1) / 2f;
        offset.Y += MathF.Round(((this.LineHeight - this.FontSize) / 2) + (this.Font->Ascent * this.FontSizeScale));

        this.SetCurrentChannel(SeStringDrawChannel.Foreground);
        this.DrawList.AddLine(
            offset,
            offset + new Vector2(advanceWidth, 0),
            this.Color,
            this.LinkUnderlineThickness);

        if (this is { Shadow: true, ShadowColor: >= 0x1000000 })
        {
            this.SetCurrentChannel(SeStringDrawChannel.Shadow);
            this.DrawList.AddLine(
                offset + new Vector2(0, 1),
                offset + new Vector2(advanceWidth, 1),
                this.ShadowColor,
                this.LinkUnderlineThickness);
        }
    }

    /// <summary>Gets the glyph corresponding to the given codepoint.</summary>
    /// <param name="rune">An instance of <see cref="Rune"/> that represents a character to display.</param>
    /// <returns>Corresponding glyph, or glyph of a fallback character specified from
    /// <see cref="ImFont.FallbackChar"/>.</returns>
    internal readonly ref ImGuiHelpers.ImFontGlyphReal FindGlyph(Rune rune)
    {
        var p = rune.Value is >= ushort.MinValue and < ushort.MaxValue
                    ? this.Font->FindGlyph((ushort)rune.Value)
                    : this.Font->FallbackGlyph;
        return ref *(ImGuiHelpers.ImFontGlyphReal*)p;
    }

    /// <summary>Gets the glyph corresponding to the given codepoint.</summary>
    /// <param name="rune">An instance of <see cref="Rune"/> that represents a character to display, that will be
    /// changed on return to the rune corresponding to the fallback glyph if a glyph not corresponding to the
    /// requested glyph is being returned.</param>
    /// <returns>Corresponding glyph, or glyph of a fallback character specified from
    /// <see cref="ImFont.FallbackChar"/>.</returns>
    internal readonly ref ImGuiHelpers.ImFontGlyphReal FindGlyph(ref Rune rune)
    {
        ref var glyph = ref this.FindGlyph(rune);
        if (rune.Value != glyph.Codepoint && !Rune.TryCreate(glyph.Codepoint, out rune))
            rune = Rune.ReplacementChar;
        return ref glyph;
    }

    /// <summary>Gets the kerning adjustment between two glyphs in a succession corresponding to the given runes.
    /// </summary>
    /// <param name="left">Rune representing the glyph on the left side of a pair.</param>
    /// <param name="right">Rune representing the glyph on the right side of a pair.</param>
    /// <returns>Distance adjustment in pixels, scaled to the size specified from
    /// <see cref="SeStringDrawParams.FontSize"/>, and rounded.</returns>
    internal readonly float CalculateScaledDistance(Rune left, Rune right)
    {
        // Kerning distance entries are ignored if NUL, U+FFFF(invalid Unicode character), or characters outside
        // the basic multilingual plane(BMP) is involved.
        if (left.Value is <= 0 or >= char.MaxValue)
            return 0;
        if (right.Value is <= 0 or >= char.MaxValue)
            return 0;

        return MathF.Round(
            this.Font->GetDistanceAdjustmentForPair(
                (ushort)left.Value,
                (ushort)right.Value) * this.FontSizeScale);
    }

    /// <summary>Handles style adjusting payloads.</summary>
    /// <param name="payload">Payload to handle.</param>
    /// <returns><c>true</c> if the payload was handled.</returns>
    internal bool HandleStyleAdjustingPayloads(ReadOnlySePayloadSpan payload)
    {
        switch (payload.MacroCode)
        {
            case MacroCode.Color:
                this.ColorStackSet.HandleColorPayload(ref this, payload);
                return true;

            case MacroCode.EdgeColor:
                this.ColorStackSet.HandleEdgeColorPayload(ref this, payload);
                return true;

            case MacroCode.ShadowColor:
                this.ColorStackSet.HandleShadowColorPayload(ref this, payload);
                return true;

            case MacroCode.Bold when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                // doesn't actually work in chat log
                this.Bold = u != 0;
                return true;

            case MacroCode.Italic when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                this.Italic = u != 0;
                return true;

            case MacroCode.Edge when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                this.Edge = u != 0;
                return true;

            case MacroCode.Shadow when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                this.Shadow = u != 0;
                return true;

            case MacroCode.ColorType:
                this.ColorStackSet.HandleColorTypePayload(ref this, payload);
                return true;

            case MacroCode.EdgeColorType:
                this.ColorStackSet.HandleEdgeColorTypePayload(ref this, payload);
                return true;

            default:
                return false;
        }
    }

    /// <summary>Splits the draw list.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SplitDrawList() => this.splitter.Split(this.drawList, ChannelCount);

    /// <summary>Merges the draw list.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MergeDrawList() => this.splitter.Merge(this.drawList);
}
