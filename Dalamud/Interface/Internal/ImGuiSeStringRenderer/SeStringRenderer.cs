using System.Collections.Generic;
using System.Numerics;
using System.Text;

using BitFaster.Caching.Lru;

using Dalamud.Data;
using Dalamud.Game.Config;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Internal.ImGuiSeStringRenderer.TextProcessing;
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

namespace Dalamud.Interface.Internal.ImGuiSeStringRenderer;

/// <summary>Draws SeString.</summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class SeStringRenderer : IInternalDisposableService
{
    private const int ChannelShadow = 0;
    private const int ChannelEdge = 1;
    private const int ChannelFore = 2;
    private const int ChannelCount = 3;

    private const char SoftHyphen = '\u00AD';
    private const char ObjectReplacementCharacter = '\uFFFC';

    [ServiceManager.ServiceDependency]
    private readonly GameConfig gameConfig = Service<GameConfig>.Get();

    private readonly ConcurrentLru<string, ReadOnlySeString> cache = new(1024);

    private readonly GfdFile gfd;
    private readonly uint[] colorTypes;
    private readonly uint[] edgeColorTypes;

    private readonly List<TextFragment> words = [];

    private readonly List<uint> colorStack = [];
    private readonly List<uint> edgeColorStack = [];
    private readonly List<uint> shadowColorStack = [];
    private bool bold;
    private bool italic;
    private Vector2 edge;
    private Vector2 shadow;

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

        return;

        static uint BgraToRgba(uint x)
        {
            var buf = (byte*)&x;
            (buf[0], buf[2]) = (buf[2], buf[0]);
            return x;
        }
    }

    /// <summary>Finalizes an instance of the <see cref="SeStringRenderer"/> class.</summary>
    ~SeStringRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.ReleaseUnmanagedResources();

    /// <summary>Creates and caches a SeString from a text macro representation, and then draws it.</summary>
    /// <param name="text">SeString text macro representation.</param>
    /// <param name="wrapWidth">Wrapping width. If a non-positive number is provided, then the remainder of the width
    /// will be used.</param>
    public void CompileAndDrawWrapped(string text, float wrapWidth = 0)
    {
        ThreadSafety.AssertMainThread();

        this.DrawWrapped(
            this.cache.GetOrAdd(
                text,
                static text =>
                {
                    var outstr = default(Utf8String);
                    outstr.Ctor();
                    RaptureTextModule.Instance()->MacroEncoder.EncodeString(&outstr, text.ReplaceLineEndings("<br>"));
                    var res = new ReadOnlySeString(outstr.AsSpan().ToArray());
                    outstr.Dtor();
                    return res;
                }).AsSpan(),
            wrapWidth);
    }

    /// <inheritdoc cref="DrawWrapped(ReadOnlySeStringSpan, float)"/>
    public void DrawWrapped(in Utf8String utf8String, float wrapWidth = 0) =>
        this.DrawWrapped(utf8String.AsSpan(), wrapWidth);

    /// <summary>Draws a SeString.</summary>
    /// <param name="sss">SeString to draw.</param>
    /// <param name="wrapWidth">Wrapping width. If a non-positive number is provided, then the remainder of the width
    /// will be used.</param>
    public void DrawWrapped(ReadOnlySeStringSpan sss, float wrapWidth = 0)
    {
        ThreadSafety.AssertMainThread();

        if (wrapWidth <= 0)
            wrapWidth = ImGui.GetContentRegionAvail().X;

        this.words.Clear();
        this.colorStack.Clear();
        this.edgeColorStack.Clear();
        this.shadowColorStack.Clear();

        this.colorStack.Add(ImGui.GetColorU32(ImGuiCol.Text));
        this.edgeColorStack.Add(0);
        this.shadowColorStack.Add(0);
        this.bold = this.italic = false;
        this.edge = Vector2.One;
        this.shadow = Vector2.Zero;

        var state = new DrawState(
            sss,
            ImGui.GetWindowDrawList(),
            this.splitter,
            ImGui.GetFont(),
            ImGui.GetFontSize(),
            ImGui.GetCursorScreenPos());
        this.CreateTextFragments(ref state, wrapWidth);

        var size = Vector2.Zero;
        for (var i = 0; i < this.words.Count; i++)
        {
            var word = this.words[i];
            this.DrawWord(
                ref state,
                word.Offset,
                state.Raw.Data[word.From..word.To],
                i == 0
                    ? '\0'
                    : this.words[i - 1].IsSoftHyphenVisible
                        ? this.words[i - 1].LastRuneRepr
                        : this.words[i - 1].LastRuneRepr2);

            if (word.IsSoftHyphenVisible && i > 0)
            {
                this.DrawWord(
                    ref state,
                    word.Offset + new Vector2(word.AdvanceWidthWithoutLastRune, 0),
                    "-"u8,
                    this.words[i - 1].LastRuneRepr);
            }

            size = Vector2.Max(size, word.Offset + new Vector2(word.VisibleWidth, state.FontSize));
        }

        state.Splitter.Merge(state.DrawList);

        ImGui.Dummy(size);
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

    private void ReleaseUnmanagedResources()
    {
        if (this.splitter.NativePtr is not null)
            this.splitter.Destroy();
        this.splitter = default;
    }

    private void CreateTextFragments(ref DrawState state, float wrapWidth)
    {
        var prev = 0;
        var runningOffset = Vector2.Zero;
        var runningWidth = 0f;
        foreach (var (curr, mandatory) in new LineBreakEnumerator(state.Raw, UtfEnumeratorFlags.Utf8SeString))
        {
            var fragment = state.CreateFragment(this, prev, curr, mandatory, runningOffset);
            var nextRunningWidth = Math.Max(runningWidth, runningOffset.X + fragment.VisibleWidth);
            if (nextRunningWidth <= wrapWidth)
            {
                // New fragment fits in the current line.
                if (this.words.Count > 0)
                {
                    char lastFragmentEnd;
                    if (this.words[^1].EndsWithSoftHyphen)
                    {
                        runningOffset.X += this.words[^1].AdvanceWidthWithoutLastRune - this.words[^1].AdvanceWidth;
                        lastFragmentEnd = this.words[^1].LastRuneRepr;
                    }
                    else
                    {
                        lastFragmentEnd = this.words[^1].LastRuneRepr2;
                    }

                    runningOffset.X += MathF.Round(
                        state.Font.GetDistanceAdjustmentForPair(lastFragmentEnd, fragment.FirstRuneRepr) *
                        state.FontSizeScale);
                    fragment = fragment with { Offset = runningOffset };
                }

                this.words.Add(fragment);
                runningWidth = nextRunningWidth;
                runningOffset.X += fragment.AdvanceWidth;
                prev = curr;
            }
            else if (fragment.VisibleWidth <= wrapWidth)
            {
                // New fragment does not fit in the current line, but it will fit in the next line.
                // Implicit conditions: runningWidth > 0, this.words.Count > 0
                runningWidth = fragment.VisibleWidth;
                runningOffset.X = fragment.AdvanceWidth;
                runningOffset.Y += state.FontSize;
                prev = curr;
                this.words[^1] = this.words[^1] with { MandatoryBreakAfter = true };
                this.words.Add(fragment with { Offset = runningOffset with { X = 0 } });
            }
            else
            {
                // New fragment does not fit in the given width, and it needs to be broken down.
                while (prev < curr)
                {
                    if (runningOffset.X > 0)
                    {
                        runningOffset.X = 0;
                        runningOffset.Y += state.FontSize;
                    }

                    fragment = state.CreateFragment(this, prev, curr, mandatory, runningOffset, wrapWidth);
                    runningWidth = fragment.VisibleWidth;
                    runningOffset.X = fragment.AdvanceWidth;
                    prev = fragment.To;
                    if (this.words.Count > 0)
                        this.words[^1] = this.words[^1] with { MandatoryBreakAfter = true };
                    this.words.Add(fragment);
                }
            }

            if (fragment.MandatoryBreakAfter)
            {
                runningOffset.X = runningWidth = 0;
                runningOffset.Y += state.FontSize;
            }
        }
    }

    private void DrawWord(ref DrawState state, Vector2 offset, ReadOnlySpan<byte> span, char lastRuneRepr)
    {
        var gfdTextureSrv =
            (nint)UIModule.Instance()->GetRaptureAtkModule()->AtkModule.AtkFontManager.Gfd->Texture->
                D3D11ShaderResourceView;
        var x = 0f;
        var width = 0f;
        foreach (var c in UtfEnumerator.From(span, UtfEnumeratorFlags.Utf8SeString))
        {
            if (c.IsSeStringPayload)
            {
                var enu = new ReadOnlySeStringSpan(span[c.ByteOffset..]).GetEnumerator();
                if (!enu.MoveNext())
                    continue;

                var payload = enu.Current;
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
                        this.bold = u != 0;
                        continue;
                    case MacroCode.Italic when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        this.italic = u != 0;
                        continue;
                    case MacroCode.Edge when payload.TryGetExpression(out var e1, out var e2) &&
                                             e1.TryGetInt(out var v1) && e2.TryGetInt(out var v2):
                        this.edge = new(v1, v2);
                        continue;
                    case MacroCode.Shadow when payload.TryGetExpression(out var e1, out var e2) &&
                                               e1.TryGetInt(out var v1) && e2.TryGetInt(out var v2):
                        this.shadow = new(v1, v2);
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

                        var useHq = state.FontSize > 19;
                        var sizeScale = (state.FontSize + 1) / gfdEntry.Height;
                        state.SetCurrentChannel(ChannelFore);
                        state.Draw(
                            offset + new Vector2(x, 0),
                            gfdTextureSrv,
                            Vector2.Zero,
                            gfdEntry.Size * sizeScale,
                            Vector2.Zero,
                            useHq ? gfdEntry.HqUv0 : gfdEntry.Uv0,
                            useHq ? gfdEntry.HqUv1 : gfdEntry.Uv1);
                        width = Math.Max(width, x + (gfdEntry.Width * sizeScale));
                        x += MathF.Round(gfdEntry.Width * sizeScale);
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

                var dyItalic = this.italic
                                   ? new Vector2(state.Font.FontSize - g.Y0, state.Font.FontSize - g.Y1) / 6
                                   : Vector2.Zero;

                if (this.shadow != Vector2.Zero && this.shadowColorStack[^1] >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelShadow);
                    state.Draw(
                        offset + this.shadow + new Vector2(x + dist, 0),
                        g,
                        dyItalic,
                        this.shadowColorStack[^1]);
                }

                if (this.edge != Vector2.Zero && this.edgeColorStack[^1] >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelEdge);
                    for (var dx = -this.edge.X; dx <= this.edge.X; dx++)
                    {
                        for (var dy = -this.edge.Y; dy <= this.edge.Y; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            state.Draw(offset + new Vector2(x + dist + dx, dy), g, dyItalic, this.edgeColorStack[^1]);
                        }
                    }
                }

                state.SetCurrentChannel(ChannelFore);
                for (var dx = this.bold ? 1 : 0; dx >= 0; dx--)
                    state.Draw(offset + new Vector2(x + dist + dx, 0), g, dyItalic, this.colorStack[^1]);

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
            if (expr.TryGetPlaceholderExpression(out var p) && p == (int)ExpressionType.StackColor && stack.Count > 1)
                stack.RemoveAt(stack.Count - 1);
            else if (expr.TryGetUInt(out var u))
                stack.Add(u);
        }

        static void TouchColorTypeStack(List<uint> stack, uint[] colorTypes, ReadOnlySePayloadSpan payload)
        {
            if (!payload.TryGetExpression(out var expr))
                return;
            if (!expr.TryGetUInt(out var u))
                return;
            if (u != 0)
                stack.Add(u < colorTypes.Length ? colorTypes[u] : 0u);
            else if (stack.Count > 1)
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
                    PadButtonValue.Accept => ControllerButton1,
                    PadButtonValue.Cancel => ControllerButton0,
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
            float wrapWidth = float.PositiveInfinity)
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
                    var sizeScale = (this.FontSize + 1) / gfdEntry.Height;
                    w = Math.Max(w, x + (gfdEntry.Width * sizeScale));
                    x += MathF.Round(gfdEntry.Width * sizeScale);
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
