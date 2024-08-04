using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

using Lumina.Excel;
using Lumina.Excel.GeneratedSheets2;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Color stacks to use while evaluating a SeString.</summary>
internal sealed class SeStringColorStackSet
{
    /// <summary>Parsed <see cref="UIColor.UIForeground"/>, containing colors to use with
    /// <see cref="MacroCode.ColorType"/>.</summary>
    private readonly uint[] colorTypes;

    /// <summary>Parsed <see cref="UIColor.UIGlow"/>, containing colors to use with
    /// <see cref="MacroCode.EdgeColorType"/>.</summary>
    private readonly uint[] edgeColorTypes;

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
    public SeStringColorStackSet(ExcelSheet<UIColor> uiColor)
    {
        var maxId = 0;
        foreach (var row in uiColor)
            maxId = (int)Math.Max(row.RowId, maxId);

        this.colorTypes = new uint[maxId + 1];
        this.edgeColorTypes = new uint[maxId + 1];
        foreach (var row in uiColor)
        {
            // Contains ABGR.
            this.colorTypes[row.RowId] = row.UIForeground;
            this.edgeColorTypes[row.RowId] = row.UIGlow;
        }

        if (BitConverter.IsLittleEndian)
        {
            // ImGui wants RGBA in LE.
            foreach (ref var r in this.colorTypes.AsSpan())
                r = BinaryPrimitives.ReverseEndianness(r);
            foreach (ref var r in this.edgeColorTypes.AsSpan())
                r = BinaryPrimitives.ReverseEndianness(r);
        }
    }

    /// <summary>Gets a value indicating whether at least one color has been pushed to the edge color stack.</summary>
    public bool HasAdditionalEdgeColor { get; private set; }

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
        drawState.ShadowColor = ColorHelpers.ApplyOpacity(AdjustStack(this.shadowColorStack, payload), drawState.Opacity);

    /// <summary>Handles a <see cref="MacroCode.ColorType"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleColorTypePayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload) =>
        drawState.Color = ColorHelpers.ApplyOpacity(AdjustStack(this.colorStack, this.colorTypes, payload), drawState.Opacity);

    /// <summary>Handles a <see cref="MacroCode.EdgeColorType"/> payload.</summary>
    /// <param name="drawState">Draw state.</param>
    /// <param name="payload">Payload to handle.</param>
    internal void HandleEdgeColorTypePayload(
        scoped ref SeStringDrawState drawState,
        ReadOnlySePayloadSpan payload)
    {
        var newColor = AdjustStack(this.edgeColorStack, this.edgeColorTypes, payload);
        if (!drawState.ForceEdgeColor)
            drawState.EdgeColor = ColorHelpers.ApplyOpacity(newColor, drawState.EdgeOpacity);

        this.HasAdditionalEdgeColor = this.edgeColorStack.Count > 1;
    }

    /// <summary>Swaps red and blue channels of a given color in ARGB(BB GG RR AA) and ABGR(RR GG BB AA).</summary>
    /// <param name="x">Color to process.</param>
    /// <returns>Swapped color.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint SwapRedBlue(uint x) => (x & 0xFF00FF00u) | ((x >> 16) & 0xFF) | ((x & 0xFF) << 16);

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
            rgbaStack.Add(SwapRedBlue(bgra) | 0xFF000000u);
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
            rgbaStack.Add(SwapRedBlue((uint)gp.IntValue) | 0xFF000000u);
            return rgbaStack[^1];
        }

        // Fallback value.
        rgbaStack.Add(0xFF000000u);
        return rgbaStack[^1];
    }

    private static uint AdjustStack(List<uint> rgbaStack, uint[] colorTypes, ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var expr))
            return rgbaStack[^1];
        if (!expr.TryGetUInt(out var colorTypeIndex))
            return rgbaStack[^1];

        if (colorTypeIndex == 0)
        {
            // First item in the stack is the color we started to draw with.
            if (rgbaStack.Count > 1)
                rgbaStack.RemoveAt(rgbaStack.Count - 1);
            return rgbaStack[^1];
        }

        // Opacity component is ignored.
        rgbaStack.Add((colorTypeIndex < colorTypes.Length ? colorTypes[colorTypeIndex] : 0u) | 0xFF000000u);

        return rgbaStack[^1];
    }
}
