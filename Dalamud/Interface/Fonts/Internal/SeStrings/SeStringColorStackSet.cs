using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Text;

using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>Color stacks to use while reading a SeString.</summary>
internal sealed class SeStringColorStackSet
{
    private readonly List<uint> colorStack = [];
    private readonly List<uint> edgeColorStack = [];
    private readonly List<uint> shadowColorStack = [];

    /// <summary>Gets the current text color. 0 is the color the text is drawn with.</summary>
    public uint Color => this.colorStack[^1];

    /// <summary>Gets the current edge color.</summary>
    public uint EdgeColor => this.edgeColorStack[^1];

    /// <summary>Gets the current shadow color.</summary>
    public uint ShadowColor => this.shadowColorStack[^1];

    /// <summary>Gets a value indicating whether at least one color has been pushed to the edge color stack.</summary>
    public bool HasAdditionalEdgeColor => this.edgeColorStack.Count > 1;

    /// <summary>Resets the stacks to the colors the text starts with.</summary>
    /// <param name="color">The text color.</param>
    /// <param name="edgeColor">The edge color.</param>
    /// <param name="shadowColor">The shadow color.</param>
    public void Initialize(uint color, uint edgeColor, uint shadowColor)
    {
        this.colorStack.Clear();
        this.edgeColorStack.Clear();
        this.shadowColorStack.Clear();
        this.colorStack.Add(color);
        this.edgeColorStack.Add(edgeColor);
        this.shadowColorStack.Add(shadowColor);
    }

    /// <summary>Handles a payload that pushes or pops a color.</summary>
    /// <param name="payload">The payload.</param>
    /// <param name="themeIndex">The theme for <see cref="MacroCode.ColorType"/> and
    /// <see cref="MacroCode.EdgeColorType"/>.</param>
    /// <returns>Whether it was such a payload.</returns>
    public bool TryHandle(ReadOnlySePayloadSpan payload, int themeIndex)
    {
        switch (payload.MacroCode)
        {
            case MacroCode.Color:
                AdjustStack(this.colorStack, payload);
                return true;
            case MacroCode.EdgeColor:
                AdjustStack(this.edgeColorStack, payload);
                return true;
            case MacroCode.ShadowColor:
                AdjustStack(this.shadowColorStack, payload);
                return true;
            case MacroCode.ColorType:
                this.AdjustStackByType(this.colorStack, payload, themeIndex);
                return true;
            case MacroCode.EdgeColorType:
                this.AdjustStackByType(this.edgeColorStack, payload, themeIndex);
                return true;
            default:
                return false;
        }
    }

    // The first item in a stack is the color the text started with, so it is never popped.
    private static void Pop(List<uint> rgbaStack)
    {
        if (rgbaStack.Count > 1)
            rgbaStack.RemoveAt(rgbaStack.Count - 1);
    }

    // Color payloads have BGRA values, and ImGui expects RGBA. The opacity is ignored.
    private static unsafe void AdjustStack(List<uint> rgbaStack, ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var expr))
            return;

        if (expr.TryGetPlaceholderExpression(out var p) && p == (int)ExpressionType.StackColor)
        {
            Pop(rgbaStack);
            return;
        }

        uint bgra;
        if (expr.TryGetUInt(out var value))
        {
            bgra = value;
        }
        else if (expr.TryGetParameterExpression(out var et, out var op) &&
                 et == (int)ExpressionType.GlobalNumber &&
                 op.TryGetInt(out var i) &&
                 RaptureTextModule.Instance() is var rtm &&
                 rtm is not null &&
                 i > 0 && i <= rtm->TextModule.MacroDecoder.GlobalParameters.Count &&
                 rtm->TextModule.MacroDecoder.GlobalParameters[i - 1] is { Type: TextParameterType.Integer } gp)
        {
            bgra = (uint)gp.IntValue;
        }
        else
        {
            bgra = 0;
        }

        // 0 duplicates the stack top to match the pop payload that closes it. This includes a global parameter of 0,
        // one that is not set, and other expressions.
        rgbaStack.Add(bgra == 0 ? rgbaStack[^1] : ColorHelpers.SwapRedBlue(bgra) | 0xFF000000u);
    }

    private void AdjustStackByType(List<uint> rgbaStack, ReadOnlySePayloadSpan payload, int themeIndex)
    {
        if (!payload.TryGetExpression(out var expr) || !expr.TryGetUInt(out var colorTypeIndex))
            return;

        // Passing 0 pops the color off the stack, as in Component::GUI::AtkFontAnalyzerBase.vf4.
        if (colorTypeIndex == 0)
        {
            Pop(rgbaStack);
            return;
        }

        var colorTypes = SeStringGameData.ColorTypes;
        var color = themeIndex >= 0 && themeIndex < colorTypes.GetLength(1) && colorTypeIndex < colorTypes.GetLength(0)
                        ? colorTypes[colorTypeIndex, themeIndex]
                        : 0u;
        rgbaStack.Add(color | 0xFF000000u);
    }
}
