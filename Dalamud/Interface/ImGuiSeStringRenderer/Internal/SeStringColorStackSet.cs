using System.Buffers.Binary;
using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Color stacks to use while evaluating a SeString.</summary>
internal sealed class SeStringColorStackSet
{
    /// <summary>Foreground color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> colorStack = [];

    /// <summary>Edge/border color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> edgeColorStack = [];

    /// <summary>Shadow color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> shadowColorStack = [];

    /// <summary>Initializes a new instance of the <see cref="SeStringColorStackSet"/> class.</summary>
    /// <param name="uiColor">UIColor sheet.</param>
    public unsafe SeStringColorStackSet(ExcelSheet<UIColor> uiColor)
    {
        var maxId = 0;
        foreach (var row in uiColor)
            maxId = (int)Math.Max(row.RowId, maxId);

        this.ColorTypes = new uint[maxId + 1, 4];
        foreach (var row in uiColor)
        {
            // Contains ABGR.
            this.ColorTypes[row.RowId, 0] = row.Dark;
            this.ColorTypes[row.RowId, 1] = row.Light;
            this.ColorTypes[row.RowId, 2] = row.ClassicFF;
            this.ColorTypes[row.RowId, 3] = row.ClearBlue;
        }

        if (BitConverter.IsLittleEndian)
        {
            // ImGui wants RGBA in LE.
            fixed (uint* p = this.ColorTypes)
            {
                foreach (ref var r in new Span<uint>(p, this.ColorTypes.GetLength(0) * this.ColorTypes.GetLength(1)))
                    r = BinaryPrimitives.ReverseEndianness(r);
            }
        }
    }

    /// <summary>Initializes a new instance of the <see cref="SeStringColorStackSet"/> class.</summary>
    /// <param name="colorTypes">Color types.</param>
    public SeStringColorStackSet(uint[,] colorTypes) => this.ColorTypes = colorTypes;

    /// <summary>Gets a value indicating whether at least one color has been pushed to the edge color stack.</summary>
    public bool HasAdditionalEdgeColor { get; private set; }

    /// <summary>Gets the parsed <see cref="UIColor"/> containing colors to use with <see cref="MacroCode.ColorType"/>
    /// and <see cref="MacroCode.EdgeColorType"/>.</summary>
    public uint[,] ColorTypes { get; }

    /// <summary>Resets the colors in the stack.</summary>
    /// <param name="drawState">Draw state.</param>
    internal void Initialize(scoped ref SeStringDrawState drawState)
    {
        this.colorStack.Clear();
        this.edgeColorStack.Clear();
        this.shadowColorStack.Clear();
        this.colorStack.Add(drawState.Color);
        this.edgeColorStack.Add(drawState.EdgeColor);
        this.shadowColorStack.Add(drawState.ShadowColor);
        drawState.Color = ColorHelpers.ApplyOpacity(drawState.Color, drawState.Opacity);
        drawState.EdgeColor = ColorHelpers.ApplyOpacity(drawState.EdgeColor, drawState.EdgeOpacity);
        drawState.ShadowColor = ColorHelpers.ApplyOpacity(drawState.ShadowColor, drawState.Opacity);
    }

    /// <summary>Handles a <see cref="MacroCode.Color"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleColorPayload(scoped ref SeStringDrawState drawState, ReadOnlySePayloadSpan payload) =>
        drawState.Color = ColorHelpers.ApplyOpacity(AdjustStack(this.colorStack, payload), drawState.Opacity);

    /// <summary>Handles a <see cref="MacroCode.EdgeColor"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleEdgeColorPayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload)
    {
        var newColor = AdjustStack(this.edgeColorStack, payload);
        if (!drawState.ForceEdgeColor)
            drawState.EdgeColor = ColorHelpers.ApplyOpacity(newColor, drawState.EdgeOpacity);

        this.HasAdditionalEdgeColor = this.edgeColorStack.Count > 1;
    }

    /// <summary>Handles a <see cref="MacroCode.ShadowColor"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleShadowColorPayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload) =>
        drawState.ShadowColor =
            ColorHelpers.ApplyOpacity(AdjustStack(this.shadowColorStack, payload), drawState.Opacity);

    /// <summary>Handles a <see cref="MacroCode.ColorType"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleColorTypePayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload) =>
        drawState.Color = ColorHelpers.ApplyOpacity(
            this.AdjustStackByType(this.colorStack, payload, drawState.ThemeIndex),
            drawState.Opacity);

    /// <summary>Handles a <see cref="MacroCode.EdgeColorType"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleEdgeColorTypePayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload)
    {
        var newColor = this.AdjustStackByType(this.edgeColorStack, payload, drawState.ThemeIndex);
        if (!drawState.ForceEdgeColor)
            drawState.EdgeColor = ColorHelpers.ApplyOpacity(newColor, drawState.EdgeOpacity);

        this.HasAdditionalEdgeColor = this.edgeColorStack.Count > 1;
    }

    private static unsafe uint AdjustStack(List<uint> rgbaStack, ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var expr))
            return rgbaStack[^1];

        // Color payloads have BGRA values as its parameter. ImGui expects RGBA values.
        // Opacity component is ignored.
        if (expr.TryGetPlaceholderExpression(out var p) && p == (int)ExpressionType.StackColor)
        {
            // First item in the stack is the color we started to draw with.
            if (rgbaStack.Count > 1)
                rgbaStack.RemoveAt(rgbaStack.Count - 1);
            return rgbaStack[^1];
        }

        if (expr.TryGetUInt(out var bgra))
        {
            // <color(0)> adds the color on the top of the stack. This makes usages like <color(gnum99)> effectively
            // become a no-op if no value is provided.
            if (bgra == 0)
                rgbaStack.Add(rgbaStack[^1]);
            else
                rgbaStack.Add(ColorHelpers.SwapRedBlue(bgra) | 0xFF000000u);
            return rgbaStack[^1];
        }

        if (expr.TryGetParameterExpression(out var et, out var op) &&
            et == (int)ExpressionType.GlobalNumber &&
            op.TryGetInt(out var i) &&
            RaptureTextModule.Instance() is var rtm &&
            rtm is not null &&
            i > 0 && i <= rtm->TextModule.MacroDecoder.GlobalParameters.Count &&
            rtm->TextModule.MacroDecoder.GlobalParameters[i - 1] is { Type: TextParameterType.Integer } gp)
        {
            rgbaStack.Add(ColorHelpers.SwapRedBlue((uint)gp.IntValue) | 0xFF000000u);
            return rgbaStack[^1];
        }

        // Fallback value.
        rgbaStack.Add(0xFF000000u);
        return rgbaStack[^1];
    }

    private uint AdjustStackByType(List<uint> rgbaStack, ReadOnlySePayloadSpan payload, int themeIndex)
    {
        if (!payload.TryGetExpression(out var expr))
            return rgbaStack[^1];
        if (!expr.TryGetUInt(out var colorTypeIndex))
            return rgbaStack[^1];

        // Component::GUI::AtkFontAnalyzerBase.vf4: passing 0 will pop the color off the stack.
        if (colorTypeIndex == 0)
        {
            // First item in the stack is the color we started to draw with.
            if (rgbaStack.Count > 1)
                rgbaStack.RemoveAt(rgbaStack.Count - 1);
            return rgbaStack[^1];
        }

        // Opacity component is ignored.
        var color = themeIndex >= 0 && themeIndex < this.ColorTypes.GetLength(1) &&
                    colorTypeIndex < this.ColorTypes.GetLength(0)
                        ? this.ColorTypes[colorTypeIndex, themeIndex]
                        : 0u;

        rgbaStack.Add(color | 0xFF000000u);
        return rgbaStack[^1];
    }
}
