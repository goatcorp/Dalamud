using System.Collections.Generic;
using System.Numerics;
using System.Text;

using BitFaster.Caching.Lru;

using Dalamud.Data;
using Dalamud.Game.Config;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets2;
using Lumina.Text.Expressions;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using static Dalamud.Game.Text.SeStringHandling.BitmapFontIcon;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Draws SeString.</summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class SeStringRenderer : IInternalDisposableService
{
    private const int ChannelShadow = 0;
    private const int ChannelLinkBackground = 1;
    private const int ChannelLinkUnderline = 2;
    private const int ChannelEdge = 3;
    private const int ChannelFore = 4;
    private const int ChannelCount = 5;

    private const int ImGuiContextCurrentWindowOffset = 0x3FF0;
    private const int ImGuiWindowDcOffset = 0x118;
    private const int ImGuiWindowTempDataCurrLineTextBaseOffset = 0x38;

    private const char SoftHyphen = '\u00AD';

    [ServiceManager.ServiceDependency]
    private readonly GameConfig gameConfig = Service<GameConfig>.Get();

    private readonly ConcurrentLru<string, ReadOnlySeString> cache = new(1024);

    private readonly GfdFile gfd;
    private readonly uint[] colorTypes;
    private readonly uint[] edgeColorTypes;

    private readonly List<TextFragment> fragments = [];

    private readonly List<uint> colorStack = [];
    private readonly List<uint> edgeColorStack = [];
    private readonly List<uint> shadowColorStack = [];
    private SeStringRenderStyle currentStyle;

    private ImDrawListSplitterPtr splitter = new(ImGuiNative.ImDrawListSplitter_ImDrawListSplitter());

    [ServiceManager.ServiceConstructor]
    private SeStringRenderer(DataManager dm)
    {
        var uiColor = dm.Excel.GetSheet<UIColor>()!;
        var maxId = 0;
        foreach (var row in uiColor)
            maxId = (int)Math.Max(row.RowId, maxId);

        this.colorTypes = new uint[maxId + 1];
        this.edgeColorTypes = new uint[maxId + 1];
        foreach (var row in uiColor)
        {
            this.colorTypes[row.RowId] = BgraToRgba((row.UIForeground >> 8) | (row.UIForeground << 24));
            this.edgeColorTypes[row.RowId] = BgraToRgba((row.UIGlow >> 8) | (row.UIGlow << 24));
        }

        this.gfd = dm.GetFile<GfdFile>("common/font/gfdata.gfd")!;
    }

    /// <summary>Finalizes an instance of the <see cref="SeStringRenderer"/> class.</summary>
    ~SeStringRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.ReleaseUnmanagedResources();

    /// <summary>Creates and caches a SeString from a text macro representation.</summary>
    /// <param name="text">SeString text macro representation.</param>
    /// <returns>Compiled SeString.</returns>
    public ReadOnlySeString Compile(string text) => this.cache.GetOrAdd(
        text,
        static text =>
        {
            using var tmp = new Utf8String();
            RaptureTextModule.Instance()->MacroEncoder.EncodeString(&tmp, text.ReplaceLineEndings("<br>"));
            return new(tmp.AsSpan().ToArray());
        });

    /// <summary>Creates and caches a SeString from a text macro representation, and then draws it.</summary>
    /// <param name="text">SeString text macro representation.</param>
    /// <param name="style">Initial rendering style.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <param name="wrapWidth">Wrapping width. If a non-positive number is provided, then the remainder of the width
    /// will be used.</param>
    /// <returns>Byte offset of the link payload that is being hovered, or <c>-1</c> if none, and whether that link
    /// (or the text itself if no link is active) is clicked.</returns>
    public (int ByteOffset, bool Clicked) CompileAndDrawWrapped(
        string text,
        SeStringRenderStyle style = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault,
        float wrapWidth = 0)
    {
        ThreadSafety.AssertMainThread();
        return this.DrawWrapped(this.Compile(text).AsSpan(), style, imGuiId, buttonFlags, wrapWidth);
    }

    /// <inheritdoc cref="DrawWrapped(ReadOnlySeStringSpan, SeStringRenderStyle, ImGuiId, ImGuiButtonFlags, float)"/>
    public (int ByteOffset, bool Clicked) DrawWrapped(
        in Utf8String utf8String,
        SeStringRenderStyle style = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault, float wrapWidth = 0) =>
        this.DrawWrapped(utf8String.AsSpan(), style, imGuiId, buttonFlags, wrapWidth);

    /// <summary>Draws a SeString.</summary>
    /// <param name="sss">SeString to draw.</param>
    /// <param name="style">Initial rendering style.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <param name="wrapWidth">Wrapping width. If a non-positive number is provided, then the remainder of the width
    /// will be used.</param>
    /// <returns>Byte offset of the link payload that is being hovered, or <c>-1</c> if none, and whether that link
    /// (or the text itself if no link is active) is clicked.</returns>
    public (int ByteOffset, bool Clicked) DrawWrapped(
        ReadOnlySeStringSpan sss,
        SeStringRenderStyle style = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault, float wrapWidth = 0)
    {
        ThreadSafety.AssertMainThread();

        if (wrapWidth <= 0)
            wrapWidth = ImGui.GetContentRegionAvail().X;

        this.fragments.Clear();
        this.colorStack.Clear();
        this.edgeColorStack.Clear();
        this.shadowColorStack.Clear();
        this.currentStyle = style;

        var state = new DrawState(
            sss,
            ImGui.GetWindowDrawList(),
            this.splitter,
            ImGui.GetFont(),
            ImGui.GetFontSize(),
            ImGui.GetCursorScreenPos());

        var pCurrentWindow = *(nint*)(ImGui.GetCurrentContext() + ImGuiContextCurrentWindowOffset);
        var pWindowDc = pCurrentWindow + ImGuiWindowDcOffset;
        var currLineTextBaseOffset = *(float*)(pWindowDc + ImGuiWindowTempDataCurrLineTextBaseOffset);

        this.CreateTextFragments(ref state, currLineTextBaseOffset, wrapWidth);

        var size = Vector2.Zero;
        var hoveredLinkOffset = -1;
        var activeLinkOffset = -1;
        for (var i = 0; i < this.fragments.Count; i++)
        {
            var fragment = this.fragments[i];
            this.DrawTextFragment(
                ref state,
                fragment.Offset,
                state.Raw.Data[fragment.From..fragment.To],
                i == 0
                    ? '\0'
                    : this.fragments[i - 1].IsSoftHyphenVisible
                        ? this.fragments[i - 1].LastRuneRepr
                        : this.fragments[i - 1].LastRuneRepr2,
                fragment.ActiveLinkOffset);

            if (fragment.IsSoftHyphenVisible && i > 0)
            {
                this.DrawTextFragment(
                    ref state,
                    fragment.Offset + new Vector2(fragment.AdvanceWidthWithoutLastRune, 0),
                    "-"u8,
                    this.fragments[i - 1].LastRuneRepr,
                    fragment.ActiveLinkOffset);
            }

            size = Vector2.Max(size, fragment.Offset + new Vector2(fragment.VisibleWidth, state.FontSize));
        }

        ImGui.Dummy(size);
        var clicked = false;
        if (imGuiId.PushId())
        {
            var invisibleButtonDrawn = false;
            foreach (var fragment in this.fragments)
            {
                var fragmentSize = new Vector2(fragment.AdvanceWidth, state.FontSize);
                if (fragment.ActiveLinkOffset != -1)
                {
                    var pos = ImGui.GetMousePos() - state.ScreenOffset - fragment.Offset;
                    if (pos is { X: >= 0, Y: >= 0 } && pos.X <= fragmentSize.X && pos.Y <= fragmentSize.Y)
                    {
                        invisibleButtonDrawn = true;

                        var cursorPosBackup = ImGui.GetCursorScreenPos();
                        ImGui.SetCursorScreenPos(state.ScreenOffset + fragment.Offset);
                        clicked = ImGui.InvisibleButton("##link", new(fragment.AdvanceWidth, state.FontSize));
                        if (ImGui.IsItemHovered())
                            hoveredLinkOffset = fragment.ActiveLinkOffset;
                        if (ImGui.IsItemActive())
                            activeLinkOffset = fragment.ActiveLinkOffset;
                        ImGui.SetCursorScreenPos(cursorPosBackup);

                        break;
                    }
                }

                size = Vector2.Max(size, fragmentSize);
            }

            if (!invisibleButtonDrawn)
            {
                ImGui.SetCursorScreenPos(state.ScreenOffset);
                clicked = ImGui.InvisibleButton("##text", size);
            }

            ImGui.PopID();
        }

        if (hoveredLinkOffset != -1 || activeLinkOffset != -1)
        {
            state.SetCurrentChannel(ChannelLinkBackground);
            foreach (var f in this.fragments)
            {
                if (f.ActiveLinkOffset != hoveredLinkOffset && hoveredLinkOffset != -1)
                    continue;
                if (f.ActiveLinkOffset != activeLinkOffset && activeLinkOffset != -1)
                    continue;
                state.DrawList.AddRectFilled(
                    state.ScreenOffset + f.Offset,
                    state.ScreenOffset + f.Offset + new Vector2(f.AdvanceWidth, state.FontSize),
                    activeLinkOffset == -1
                        ? this.currentStyle.LinkHoverColor
                        : this.currentStyle.LinkActiveColor);
            }
        }

        state.Splitter.Merge(state.DrawList);

        return (hoveredLinkOffset, clicked);
    }

    /// <summary>Gets the printable char for the given char, or null(\0) if it should not be handled at all.</summary>
    /// <param name="c">Character to determine.</param>
    /// <returns>Character to print, or null(\0) if none.</returns>
    private static Rune? ToPrintableRune(int c) => c switch
    {
        char.MaxValue => null,
        SoftHyphen => new('-'),
        _ when UnicodeData.LineBreak[c]
                   is UnicodeLineBreakClass.BK
                   or UnicodeLineBreakClass.CR
                   or UnicodeLineBreakClass.LF
                   or UnicodeLineBreakClass.NL => new(0),
        _ => new(c),
    };

    private static uint BgraToRgba(uint x)
    {
        var buf = (byte*)&x;
        (buf[0], buf[2]) = (buf[2], buf[0]);
        return x;
    }

    private void ReleaseUnmanagedResources()
    {
        if (this.splitter.NativePtr is not null)
            this.splitter.Destroy();
        this.splitter = default;
    }

    private void CreateTextFragments(ref DrawState state, float baseOffset, float wrapWidth)
    {
        var prev = 0;
        var runningOffset = new Vector2(0, baseOffset);
        var runningWidth = 0f;
        var activeLinkOffset = -1;
        foreach (var (curr2, mandatory) in new LineBreakEnumerator(state.Raw, UtfEnumeratorFlags.Utf8SeString))
        {
            var curr = curr2;
            var fragment = state.CreateFragment(this, prev, curr, mandatory, runningOffset, activeLinkOffset);
            var nextRunningWidth = Math.Max(runningWidth, runningOffset.X + fragment.VisibleWidth);
            var nextLinkOffset = activeLinkOffset;
            if (nextRunningWidth <= wrapWidth)
            {
                // New fragment fits in the current line.
                foreach (var p in new ReadOnlySeStringSpan(state.Raw.Data[prev..curr2]).GetOffsetEnumerator())
                {
                    if (p.Payload.MacroCode == MacroCode.Link)
                    {
                        nextLinkOffset =
                            p.Payload.TryGetExpression(out var e) &&
                            e.TryGetUInt(out var u) &&
                            u == (uint)LinkMacroPayloadType.Terminator
                                ? -1
                                : prev + p.Offset;
                        if (p.Offset != 0)
                        {
                            curr = prev + p.Offset;
                            this.CreateNonBreakingTextFragment(
                                ref state,
                                ref runningOffset,
                                ref prev,
                                curr,
                                curr == curr2 && mandatory,
                                activeLinkOffset);
                        }

                        activeLinkOffset = nextLinkOffset;
                    }
                }

                this.CreateNonBreakingTextFragment(
                    ref state,
                    ref runningOffset,
                    ref prev,
                    curr2,
                    mandatory,
                    activeLinkOffset);

                prev = curr2;
                runningWidth = nextRunningWidth;
            }
            else if (fragment.VisibleWidth <= wrapWidth)
            {
                // New fragment does not fit in the current line, but it will fit in the next line.
                // Implicit conditions: runningWidth > 0, this.words.Count > 0
                runningOffset.X = 0;
                runningOffset.Y += state.FontSize;
                this.fragments[^1] = this.fragments[^1] with { MandatoryBreakAfter = true };

                foreach (var p in new ReadOnlySeStringSpan(state.Raw.Data[prev..curr2]).GetOffsetEnumerator())
                {
                    if (p.Payload.MacroCode == MacroCode.Link)
                    {
                        nextLinkOffset =
                            p.Payload.TryGetExpression(out var e) &&
                            e.TryGetUInt(out var u) &&
                            u == (uint)LinkMacroPayloadType.Terminator
                                ? -1
                                : prev + p.Offset;
                        if (p.Offset != 0)
                        {
                            curr = prev + p.Offset;
                            this.CreateNonBreakingTextFragment(
                                ref state,
                                ref runningOffset,
                                ref prev,
                                curr,
                                curr == curr2 && mandatory,
                                activeLinkOffset);
                        }

                        activeLinkOffset = nextLinkOffset;
                    }
                }

                this.CreateNonBreakingTextFragment(
                    ref state,
                    ref runningOffset,
                    ref prev,
                    curr2,
                    mandatory,
                    activeLinkOffset);

                prev = curr2;
                runningWidth = this.fragments[^1].AdvanceWidth;
            }
            else
            {
                // New fragment does not fit in the given width, and it needs to be broken down.
                while (prev < curr2)
                {
                    if (runningOffset.X > 0)
                    {
                        runningOffset.X = 0;
                        runningOffset.Y += state.FontSize;
                    }

                    curr = curr2;
                    foreach (var p in new ReadOnlySeStringSpan(state.Raw.Data[prev..curr2]).GetOffsetEnumerator())
                    {
                        if (p.Payload.MacroCode == MacroCode.Link)
                        {
                            nextLinkOffset =
                                p.Payload.TryGetExpression(out var e) &&
                                e.TryGetUInt(out var u) &&
                                u == (uint)LinkMacroPayloadType.Terminator
                                    ? -1
                                    : prev + p.Offset;
                            if (p.Offset != 0)
                            {
                                curr = prev + p.Offset;
                                break;
                            }
                        }
                    }

                    fragment = state.CreateFragment(
                        this,
                        prev,
                        curr,
                        fragment.To != curr || mandatory,
                        runningOffset,
                        activeLinkOffset,
                        wrapWidth);
                    activeLinkOffset = nextLinkOffset;
                    runningWidth = fragment.VisibleWidth;
                    runningOffset.X = fragment.AdvanceWidth;
                    prev = fragment.To;
                    if (this.fragments.Count > 0)
                        this.fragments[^1] = this.fragments[^1] with { MandatoryBreakAfter = true };
                    this.fragments.Add(fragment);
                }
            }

            if (fragment.MandatoryBreakAfter)
            {
                runningOffset.X = runningWidth = 0;
                runningOffset.Y += state.FontSize;
            }
        }
    }

    private void CreateNonBreakingTextFragment(
        ref DrawState state,
        ref Vector2 runningOffset,
        ref int prev,
        int curr,
        bool mandatory,
        int activeLinkOffset)
    {
        var fragment = state.CreateFragment(this, prev, curr, mandatory, runningOffset, activeLinkOffset);

        if (this.fragments.Count > 0)
        {
            char lastFragmentEnd;
            if (this.fragments[^1].EndsWithSoftHyphen)
            {
                runningOffset.X += this.fragments[^1].AdvanceWidthWithoutLastRune - this.fragments[^1].AdvanceWidth;
                lastFragmentEnd = this.fragments[^1].LastRuneRepr;
            }
            else
            {
                lastFragmentEnd = this.fragments[^1].LastRuneRepr2;
            }

            runningOffset.X += MathF.Round(
                state.Font.GetDistanceAdjustmentForPair(lastFragmentEnd, fragment.FirstRuneRepr) *
                state.FontSizeScale);
            fragment = fragment with { Offset = runningOffset };
        }

        this.fragments.Add(fragment);
        runningOffset.X += fragment.AdvanceWidth;
        prev = curr;
    }

    private void DrawTextFragment(
        ref DrawState state,
        Vector2 offset,
        ReadOnlySpan<byte> span,
        char lastRuneRepr,
        int activeLinkOffset)
    {
        var gfdTextureSrv =
            (nint)UIModule.Instance()->GetRaptureAtkModule()->AtkModule.AtkFontManager.Gfd->Texture->
                D3D11ShaderResourceView;
        var x = 0f;
        var width = 0f;
        foreach (var c in UtfEnumerator.From(span, UtfEnumeratorFlags.Utf8SeString))
        {
            var activeColor = this.colorStack.Count == 0 ? this.currentStyle.Color : this.colorStack[^1];

            if (c.IsSeStringPayload)
            {
                var enu = new ReadOnlySeStringSpan(span[c.ByteOffset..]).GetOffsetEnumerator();
                if (!enu.MoveNext())
                    continue;

                var payload = enu.Current.Payload;
                switch (payload.MacroCode)
                {
                    case MacroCode.Color:
                        TouchColorStack(this.colorStack, payload);
                        continue;
                    case MacroCode.EdgeColor:
                        TouchColorStack(this.edgeColorStack, payload);
                        continue;
                    case MacroCode.ShadowColor:
                        TouchColorStack(this.shadowColorStack, payload);
                        continue;
                    case MacroCode.Bold when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        // doesn't actually work in chat log
                        this.currentStyle.Bold = u != 0;
                        continue;
                    case MacroCode.Italic when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        this.currentStyle.Italic = u != 0;
                        continue;
                    case MacroCode.Edge when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        this.currentStyle.Edge = u != 0;
                        continue;
                    case MacroCode.Shadow when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        this.currentStyle.Shadow = u != 0;
                        continue;
                    case MacroCode.ColorType:
                        TouchColorTypeStack(this.colorStack, this.colorTypes, payload);
                        continue;
                    case MacroCode.EdgeColorType:
                        TouchColorTypeStack(this.edgeColorStack, this.edgeColorTypes, payload);
                        continue;
                    case MacroCode.Icon:
                    case MacroCode.Icon2:
                    {
                        if (this.GetBitmapFontIconFor(span[c.ByteOffset..]) is not (var icon and not None) ||
                            !this.gfd.TryGetEntry((uint)icon, out var gfdEntry) ||
                            gfdEntry.IsEmpty)
                            continue;

                        var size = state.CalculateGfdEntrySize(gfdEntry, out var useHq);
                        state.SetCurrentChannel(ChannelFore);
                        state.Draw(
                            offset + new Vector2(x, (state.FontSize - size.Y) / 2),
                            gfdTextureSrv,
                            Vector2.Zero,
                            size,
                            Vector2.Zero,
                            useHq ? gfdEntry.HqUv0 : gfdEntry.Uv0,
                            useHq ? gfdEntry.HqUv1 : gfdEntry.Uv1);
                        if (activeLinkOffset != -1 && this.currentStyle.LinkUnderline)
                        {
                            state.SetCurrentChannel(ChannelLinkUnderline);
                            state.DrawList.AddLine(
                                state.ScreenOffset + offset + new Vector2(
                                    x,
                                    MathF.Round(state.Font.Ascent * state.FontSizeScale)),
                                state.ScreenOffset + offset + new Vector2(
                                    x + size.X,
                                    MathF.Round(state.Font.Ascent * state.FontSizeScale)),
                                activeColor);
                        }

                        width = Math.Max(width, x + size.X);
                        x += MathF.Round(size.X);
                        lastRuneRepr = '\0';
                        continue;
                    }

                    default:
                        continue;
                }
            }

            if (ToPrintableRune(c.EffectiveChar) is not { } rune)
                continue;

            var runeRepr = rune.Value is >= 0 and < char.MaxValue ? (char)rune.Value : '\uFFFE';
            if (runeRepr != 0)
            {
                var dist = state.Font.GetDistanceAdjustmentForPair(lastRuneRepr, runeRepr);
                ref var g = ref *(ImGuiHelpers.ImFontGlyphReal*)state.Font.FindGlyph(runeRepr).NativePtr;

                var dyItalic = this.currentStyle.Italic
                                   ? new Vector2(state.Font.FontSize - g.Y0, state.Font.FontSize - g.Y1) / 6
                                   : Vector2.Zero;

                var activeShadowColor = this.shadowColorStack.Count == 0
                                            ? this.currentStyle.ShadowColor
                                            : this.shadowColorStack[^1];
                if (this.currentStyle.Shadow && activeShadowColor >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelShadow);
                    state.Draw(offset + new Vector2(x + dist, 1), g, dyItalic, activeShadowColor);
                }

                var useEdge = this.edgeColorStack.Count > 0 || this.currentStyle.Edge;
                var activeEdgeColor =
                    this.currentStyle.ForceEdgeColor
                        ? this.currentStyle.EdgeColor
                        : this.edgeColorStack.Count == 0
                            ? this.currentStyle.EdgeColor
                            : this.edgeColorStack[^1];
                activeEdgeColor = (activeEdgeColor & 0xFFFFFFu) | ((activeEdgeColor >> 26) << 24);
                if (useEdge && activeEdgeColor >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelEdge);
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            state.Draw(offset + new Vector2(x + dist + dx, dy), g, dyItalic, activeEdgeColor);
                        }
                    }
                }

                state.SetCurrentChannel(ChannelFore);
                for (var dx = this.currentStyle.Bold ? 1 : 0; dx >= 0; dx--)
                    state.Draw(offset + new Vector2(x + dist + dx, 0), g, dyItalic, activeColor);

                if (activeLinkOffset != -1 && this.currentStyle.LinkUnderline)
                {
                    state.SetCurrentChannel(ChannelLinkUnderline);
                    state.DrawList.AddLine(
                        state.ScreenOffset + offset + new Vector2(
                            x + dist,
                            MathF.Round(state.Font.Ascent * state.FontSizeScale)),
                        state.ScreenOffset + offset + new Vector2(
                            x + dist + g.AdvanceX,
                            MathF.Round(state.Font.Ascent * state.FontSizeScale)),
                        activeColor);
                }

                width = Math.Max(width, x + dist + (g.X1 * state.FontSizeScale));
                x += dist + MathF.Round(g.AdvanceX * state.FontSizeScale);
            }

            lastRuneRepr = runeRepr;
        }

        return;

        static void TouchColorStack(List<uint> stack, ReadOnlySePayloadSpan payload)
        {
            if (!payload.TryGetExpression(out var expr))
                return;
            if (expr.TryGetPlaceholderExpression(out var p) && p == (int)ExpressionType.StackColor && stack.Count > 0)
                stack.RemoveAt(stack.Count - 1);
            else if (expr.TryGetUInt(out var u))
                stack.Add(BgraToRgba(u) | 0xFF000000u);
        }

        static void TouchColorTypeStack(List<uint> stack, uint[] colorTypes, ReadOnlySePayloadSpan payload)
        {
            if (!payload.TryGetExpression(out var expr))
                return;
            if (!expr.TryGetUInt(out var u))
                return;
            if (u != 0)
                stack.Add((u < colorTypes.Length ? colorTypes[u] : 0u) | 0xFF000000u);
            else if (stack.Count > 0)
                stack.RemoveAt(stack.Count - 1);
        }
    }

    private BitmapFontIcon GetBitmapFontIconFor(ReadOnlySpan<byte> sss)
    {
        var e = new ReadOnlySeStringSpan(sss).GetEnumerator();
        if (!e.MoveNext() || e.Current.MacroCode is not MacroCode.Icon and not MacroCode.Icon2)
            return None;

        var payload = e.Current;
        switch (payload.MacroCode)
        {
            case MacroCode.Icon
                when payload.TryGetExpression(out var icon) && icon.TryGetInt(out var iconId):
                return (BitmapFontIcon)iconId;
            case MacroCode.Icon2
                when payload.TryGetExpression(out var icon) && icon.TryGetInt(out var iconId):
                var configName = (BitmapFontIcon)iconId switch
                {
                    ControllerShoulderLeft => SystemConfigOption.PadButton_L1,
                    ControllerShoulderRight => SystemConfigOption.PadButton_R1,
                    ControllerTriggerLeft => SystemConfigOption.PadButton_L2,
                    ControllerTriggerRight => SystemConfigOption.PadButton_R2,
                    ControllerButton3 => SystemConfigOption.PadButton_Triangle,
                    ControllerButton1 => SystemConfigOption.PadButton_Cross,
                    ControllerButton0 => SystemConfigOption.PadButton_Circle,
                    ControllerButton2 => SystemConfigOption.PadButton_Square,
                    ControllerStart => SystemConfigOption.PadButton_Start,
                    ControllerBack => SystemConfigOption.PadButton_Select,
                    ControllerAnalogLeftStick => SystemConfigOption.PadButton_LS,
                    ControllerAnalogLeftStickIn => SystemConfigOption.PadButton_LS,
                    ControllerAnalogLeftStickUpDown => SystemConfigOption.PadButton_LS,
                    ControllerAnalogLeftStickLeftRight => SystemConfigOption.PadButton_LS,
                    ControllerAnalogRightStick => SystemConfigOption.PadButton_RS,
                    ControllerAnalogRightStickIn => SystemConfigOption.PadButton_RS,
                    ControllerAnalogRightStickUpDown => SystemConfigOption.PadButton_RS,
                    ControllerAnalogRightStickLeftRight => SystemConfigOption.PadButton_RS,
                    _ => (SystemConfigOption?)null,
                };

                if (configName is null || !this.gameConfig.TryGet(configName.Value, out PadButtonValue pb))
                    return (BitmapFontIcon)iconId;

                return pb switch
                {
                    PadButtonValue.Autorun_Support => ControllerShoulderLeft,
                    PadButtonValue.Hotbar_Set_Change => ControllerShoulderRight,
                    PadButtonValue.XHB_Left_Start => ControllerTriggerLeft,
                    PadButtonValue.XHB_Right_Start => ControllerTriggerRight,
                    PadButtonValue.Jump => ControllerButton3,
                    PadButtonValue.Accept => ControllerButton0,
                    PadButtonValue.Cancel => ControllerButton1,
                    PadButtonValue.Map_Sub => ControllerButton2,
                    PadButtonValue.MainCommand => ControllerStart,
                    PadButtonValue.HUD_Select => ControllerBack,
                    PadButtonValue.Move_Operation => (BitmapFontIcon)iconId switch
                    {
                        ControllerAnalogLeftStick => ControllerAnalogLeftStick,
                        ControllerAnalogLeftStickIn => ControllerAnalogLeftStickIn,
                        ControllerAnalogLeftStickUpDown => ControllerAnalogLeftStickUpDown,
                        ControllerAnalogLeftStickLeftRight => ControllerAnalogLeftStickLeftRight,
                        ControllerAnalogRightStick => ControllerAnalogLeftStick,
                        ControllerAnalogRightStickIn => ControllerAnalogLeftStickIn,
                        ControllerAnalogRightStickUpDown => ControllerAnalogLeftStickUpDown,
                        ControllerAnalogRightStickLeftRight => ControllerAnalogLeftStickLeftRight,
                        _ => (BitmapFontIcon)iconId,
                    },
                    PadButtonValue.Camera_Operation => (BitmapFontIcon)iconId switch
                    {
                        ControllerAnalogLeftStick => ControllerAnalogRightStick,
                        ControllerAnalogLeftStickIn => ControllerAnalogRightStickIn,
                        ControllerAnalogLeftStickUpDown => ControllerAnalogRightStickUpDown,
                        ControllerAnalogLeftStickLeftRight => ControllerAnalogRightStickLeftRight,
                        ControllerAnalogRightStick => ControllerAnalogRightStick,
                        ControllerAnalogRightStickIn => ControllerAnalogRightStickIn,
                        ControllerAnalogRightStickUpDown => ControllerAnalogRightStickUpDown,
                        ControllerAnalogRightStickLeftRight => ControllerAnalogRightStickLeftRight,
                        _ => (BitmapFontIcon)iconId,
                    },
                    _ => (BitmapFontIcon)iconId,
                };
        }

        return None;
    }

    private readonly record struct TextFragment(
        int From,
        int To,
        int ActiveLinkOffset,
        Vector2 Offset,
        float VisibleWidth,
        float AdvanceWidth,
        float AdvanceWidthWithoutLastRune,
        bool MandatoryBreakAfter,
        bool EndsWithSoftHyphen,
        char FirstRuneRepr,
        char LastRuneRepr,
        char LastRuneRepr2)
    {
        public bool IsSoftHyphenVisible => this.EndsWithSoftHyphen && this.MandatoryBreakAfter;
    }

    private ref struct DrawState
    {
        public readonly ReadOnlySeStringSpan Raw;
        public readonly float FontSize;
        public readonly float FontSizeScale;
        public readonly Vector2 ScreenOffset;

        public ImDrawListPtr DrawList;
        public ImDrawListSplitterPtr Splitter;
        public ImFontPtr Font;

        public DrawState(
            ReadOnlySeStringSpan raw,
            ImDrawListPtr drawList,
            ImDrawListSplitterPtr splitter,
            ImFontPtr font,
            float fontSize,
            Vector2 screenOffset)
        {
            this.Raw = raw;
            this.DrawList = drawList;
            this.Splitter = splitter;
            this.Font = font;
            this.FontSize = fontSize;
            this.FontSizeScale = fontSize / font.FontSize;
            this.ScreenOffset = screenOffset;

            splitter.Split(drawList, ChannelCount);
        }

        public Vector2 CalculateGfdEntrySize(in GfdFile.GfdEntry gfdEntry, out bool useHq)
        {
            useHq = this.FontSize > 20;
            var targetHeight = useHq ? this.FontSize : 20;
            return new(gfdEntry.Width * (targetHeight / gfdEntry.Height), targetHeight);
        }

        public void SetCurrentChannel(int channelIndex) => this.Splitter.SetCurrentChannel(this.DrawList, channelIndex);

        public void Draw(Vector2 offset, in ImGuiHelpers.ImFontGlyphReal g, Vector2 dyItalic, uint color) =>
            this.Draw(
                offset,
                this.Font.ContainerAtlas.Textures[g.TextureIndex].TexID,
                g.XY0 * this.FontSizeScale,
                g.XY1 * this.FontSizeScale,
                dyItalic * this.FontSizeScale,
                g.UV0,
                g.UV1,
                color);

        public void Draw(
            Vector2 offset,
            nint igTextureId,
            Vector2 xy0,
            Vector2 xy1,
            Vector2 dyItalic,
            Vector2 uv0,
            Vector2 uv1,
            uint color = uint.MaxValue)
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

        public TextFragment CreateFragment(
            SeStringRenderer renderer,
            int from,
            int to,
            bool mandatoryBreakAfter,
            Vector2 offset,
            int activeLinkOffset,
            float wrapWidth = float.MaxValue)
        {
            var lastNonSpace = from;

            var x = 0f;
            var w = 0f;
            var visibleWidth = 0f;
            var advanceWidth = 0f;
            var prevAdvanceWidth = 0f;
            var firstRuneRepr = char.MaxValue;
            var lastRuneRepr = default(char);
            var lastRuneRepr2 = default(char);
            var endsWithSoftHyphen = false;
            foreach (var c in UtfEnumerator.From(this.Raw.Data[from..to], UtfEnumeratorFlags.Utf8SeString))
            {
                prevAdvanceWidth = x;
                lastRuneRepr2 = lastRuneRepr;
                endsWithSoftHyphen = c.EffectiveChar == SoftHyphen;

                var byteOffset = from + c.ByteOffset;
                var isBreakableWhitespace = false;
                if (c is { IsSeStringPayload: true, MacroCode: MacroCode.Icon or MacroCode.Icon2 } &&
                    renderer.GetBitmapFontIconFor(this.Raw.Data[byteOffset..]) is var icon and not None &&
                    renderer.gfd.TryGetEntry((uint)icon, out var gfdEntry) &&
                    !gfdEntry.IsEmpty)
                {
                    var size = this.CalculateGfdEntrySize(gfdEntry, out _);
                    w = Math.Max(w, x + size.X);
                    x += MathF.Round(size.X);
                    lastRuneRepr = default;
                }
                else if (ToPrintableRune(c.EffectiveChar) is { } rune)
                {
                    var runeRepr = rune.Value is >= 0 and < char.MaxValue ? (char)rune.Value : '\uFFFE';
                    if (runeRepr != 0)
                    {
                        var dist = this.Font.GetDistanceAdjustmentForPair(lastRuneRepr, runeRepr);
                        ref var g = ref *(ImGuiHelpers.ImFontGlyphReal*)this.Font.FindGlyph(runeRepr).NativePtr;
                        w = Math.Max(w, x + ((dist + g.X1) * this.FontSizeScale));
                        x += MathF.Round((dist + g.AdvanceX) * this.FontSizeScale);
                    }

                    isBreakableWhitespace = Rune.IsWhiteSpace(rune) &&
                                            UnicodeData.LineBreak[rune.Value] is not UnicodeLineBreakClass.GL;
                    lastRuneRepr = runeRepr;
                }
                else
                {
                    continue;
                }

                if (firstRuneRepr == char.MaxValue)
                    firstRuneRepr = lastRuneRepr;

                if (isBreakableWhitespace)
                {
                    advanceWidth = x;
                }
                else
                {
                    if (w > wrapWidth && lastNonSpace != from && !endsWithSoftHyphen)
                    {
                        to = byteOffset;
                        break;
                    }

                    advanceWidth = x;
                    visibleWidth = w;
                    lastNonSpace = byteOffset + c.ByteLength;
                }
            }

            return new(
                from,
                to,
                activeLinkOffset,
                offset,
                visibleWidth,
                advanceWidth,
                prevAdvanceWidth,
                mandatoryBreakAfter,
                endsWithSoftHyphen,
                firstRuneRepr,
                lastRuneRepr,
                lastRuneRepr2);
        }
    }
}
