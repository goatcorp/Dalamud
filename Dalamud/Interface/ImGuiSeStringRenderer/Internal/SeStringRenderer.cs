using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using BitFaster.Caching.Lru;

using Dalamud.Data;
using Dalamud.Game;
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

using SeStringBuilder = Lumina.Text.SeStringBuilder;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Draws SeString.</summary>
[ServiceManager.EarlyLoadedService]
internal unsafe class SeStringRenderer : IInternalDisposableService
{
    private const int ChannelLinkBackground = 0;
    private const int ChannelShadow = 1;
    private const int ChannelLinkUnderline = 2;
    private const int ChannelEdge = 3;
    private const int ChannelFore = 4;
    private const int ChannelCount = 5;

    private const int ImGuiContextCurrentWindowOffset = 0x3FF0;
    private const int ImGuiWindowDcOffset = 0x118;
    private const int ImGuiWindowTempDataCurrLineTextBaseOffset = 0x38;

    /// <summary>Soft hyphen character, which signifies that a word can be broken here, and will display a standard
    /// hyphen when it is broken there.</summary>
    private const char SoftHyphen = '\u00AD';

    /// <summary>SeString to return instead, if macro encoder has failed and could not provide us the reason.</summary>
    private static readonly ReadOnlySeString MacroEncoderEncodeStringError =
        new SeStringBuilder()
            .BeginMacro(MacroCode.ColorType).AppendIntExpression(508).EndMacro()
            .BeginMacro(MacroCode.EdgeColorType).AppendIntExpression(509).EndMacro()
            .Append(
                "<encode failed, and error message generation failed because the part that caused the error was too long>"u8)
            .BeginMacro(MacroCode.EdgeColorType).AppendIntExpression(0).EndMacro()
            .BeginMacro(MacroCode.ColorType).AppendIntExpression(0).EndMacro()
            .ToReadOnlySeString();

    [ServiceManager.ServiceDependency]
    private readonly GameConfig gameConfig = Service<GameConfig>.Get();

    /// <summary>Cache of compiled SeStrings from <see cref="CompileAndCache"/>.</summary>
    private readonly ConcurrentLru<string, ReadOnlySeString> cache = new(1024);

    /// <summary>Sets the global invalid parameter handler. Used to suppress <c>vsprintf_s</c> from raising.</summary>
    /// <remarks>There exists a thread local version of this, but as the game-provided implementation is what
    /// effectively is a screaming tool that the game has a bug, it should be safe to fail in any means.</remarks>
    private readonly delegate* unmanaged<
        delegate* unmanaged<char*, char*, char*, int, nuint, void>,
        delegate* unmanaged<char*, char*, char*, int, nuint, void>> setInvalidParameterHandler;

    /// <summary>Parsed <c>gfdata.gfd</c> file, containing bitmap font icon lookup table.</summary>
    private readonly GfdFile gfd;

    /// <summary>Parsed <see cref="UIColor.UIForeground"/>, containing colors to use with
    /// <see cref="MacroCode.ColorType"/>.</summary>
    private readonly uint[] colorTypes;

    /// <summary>Parsed <see cref="UIColor.UIGlow"/>, containing colors to use with
    /// <see cref="MacroCode.EdgeColorType"/>.</summary>
    private readonly uint[] edgeColorTypes;

    /// <summary>Parsed text fragments from a SeString.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<TextFragment> fragments = [];

    /// <summary>Foreground color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> colorStack = [];

    /// <summary>Edge/border color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> edgeColorStack = [];

    /// <summary>Shadow color stack while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<uint> shadowColorStack = [];

    /// <summary>Splits a draw list so that different layers of a single glyph can be drawn out of order.</summary>
    private ImDrawListSplitter* splitter = ImGuiNative.ImDrawListSplitter_ImDrawListSplitter();

    [ServiceManager.ServiceConstructor]
    private SeStringRenderer(DataManager dm, TargetSigScanner sigScanner)
    {
        var uiColor = dm.Excel.GetSheet<UIColor>()!;
        var maxId = 0;
        foreach (var row in uiColor)
            maxId = (int)Math.Max(row.RowId, maxId);

        this.colorTypes = new uint[maxId + 1];
        this.edgeColorTypes = new uint[maxId + 1];
        foreach (var row in uiColor)
        {
            this.colorTypes[row.RowId] = SwapRedBlue((row.UIForeground >> 8) | (row.UIForeground << 24));
            this.edgeColorTypes[row.RowId] = SwapRedBlue((row.UIGlow >> 8) | (row.UIGlow << 24));
        }

        this.gfd = dm.GetFile<GfdFile>("common/font/gfdata.gfd")!;

        // SetUnhandledExceptionFilter(who cares);
        // _set_purecall_handler(() => *(int*)0 = 0xff14);
        // _set_invalid_parameter_handler(() => *(int*)0 = 0xff14);
        var f = sigScanner.ScanText(
                    "ff 15 ff 0e e3 01 48 8d 0d ?? ?? ?? ?? e8 ?? ?? ?? ?? 48 8d 0d ?? ?? ?? ?? e8 ?? ?? ?? ??") + 26;
        fixed (void* p = &this.setInvalidParameterHandler)
            *(nint*)p = *(int*)f + f + 4;
    }

    /// <summary>Finalizes an instance of the <see cref="SeStringRenderer"/> class.</summary>
    ~SeStringRenderer() => this.ReleaseUnmanagedResources();

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService() => this.ReleaseUnmanagedResources();

    /// <summary>Compiles a SeString from a text macro representation.</summary>
    /// <param name="text">SeString text macro representation.</param>
    /// <returns>Compiled SeString.</returns>
    public ReadOnlySeString Compile(ReadOnlySpan<byte> text)
    {
        // MacroEncoder looks stateful; disallowing calls from off main threads for now.
        ThreadSafety.AssertMainThread();

        var prev = this.setInvalidParameterHandler(&MsvcrtInvalidParameterHandlerDetour);
        try
        {
            using var tmp = new Utf8String();
            RaptureTextModule.Instance()->MacroEncoder.EncodeString(&tmp, text);
            return new(tmp.AsSpan().ToArray());
        }
        catch (Exception)
        {
            return MacroEncoderEncodeStringError;
        }
        finally
        {
            this.setInvalidParameterHandler(prev);
        }

        [UnmanagedCallersOnly]
        static void MsvcrtInvalidParameterHandlerDetour(char* a, char* b, char* c, int d, nuint e) =>
            throw new InvalidOperationException();
    }

    /// <summary>Compiles a SeString from a text macro representation.</summary>
    /// <param name="text">SeString text macro representation.</param>
    /// <returns>Compiled SeString.</returns>
    public ReadOnlySeString Compile(ReadOnlySpan<char> text)
    {
        var len = Encoding.UTF8.GetByteCount(text);
        if (len >= 1024)
        {
            var buf = ArrayPool<byte>.Shared.Rent(len + 1);
            buf[Encoding.UTF8.GetBytes(text, buf)] = 0;
            var res = this.Compile(buf);
            ArrayPool<byte>.Shared.Return(buf);
            return res;
        }
        else
        {
            Span<byte> buf = stackalloc byte[len + 1];
            buf[Encoding.UTF8.GetBytes(text, buf)] = 0;
            return this.Compile(buf);
        }
    }

    /// <summary>Compiles and caches a SeString from a text macro representation.</summary>
    /// <param name="text">SeString text macro representation.
    /// Newline characters will be normalized to newline payloads.</param>
    /// <returns>Compiled SeString.</returns>
    public ReadOnlySeString CompileAndCache(string text)
    {
        // MacroEncoder looks stateful; disallowing calls from off main threads for now.
        // Note that this is replicated in context.Compile. Only access cache from the main thread.
        ThreadSafety.AssertMainThread();

        return this.cache.GetOrAdd(
            text,
            static (text, context) => context.Compile(text.ReplaceLineEndings("<br>")),
            this);
    }

    /// <summary>Compiles and caches a SeString from a text macro representation, and then draws it.</summary>
    /// <param name="text">SeString text macro representation.
    /// Newline characters will be normalized to newline payloads.</param>
    /// <param name="drawParams">Parameters for drawing.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <returns>Interaction result of the rendered text.</returns>
    public SeStringDrawResult CompileAndDrawWrapped(
        string text,
        SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault) =>
        this.Draw(this.CompileAndCache(text).AsSpan(), drawParams, imGuiId, buttonFlags);

    /// <inheritdoc cref="Draw(ReadOnlySeStringSpan, SeStringDrawParams, ImGuiId, ImGuiButtonFlags)"/>
    public SeStringDrawResult Draw(
        in Utf8String utf8String,
        SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault) =>
        this.Draw(utf8String.AsSpan(), drawParams, imGuiId, buttonFlags);

    /// <summary>Draws a SeString.</summary>
    /// <param name="sss">SeString to draw.</param>
    /// <param name="drawParams">Parameters for drawing.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <returns>Interaction result of the rendered text.</returns>
    public SeStringDrawResult Draw(
        ReadOnlySeStringSpan sss,
        SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault)
    {
        // Drawing is only valid if done from the main thread anyway, especially with interactivity.
        ThreadSafety.AssertMainThread();

        if (drawParams.TargetDrawList is not null && imGuiId)
            throw new ArgumentException("ImGuiId cannot be set if TargetDrawList is manually set.", nameof(imGuiId));

        // This also does argument validation for drawParams. Do it here.
        var state = new DrawState(sss, new(drawParams), this.splitter);

        // Reset the state.
        this.fragments.Clear();
        this.colorStack.Clear();
        this.edgeColorStack.Clear();
        this.shadowColorStack.Clear();

        // Handle cases where ImGui.AlignTextToFramePadding has been called.
        var pCurrentWindow = *(nint*)(ImGui.GetCurrentContext() + ImGuiContextCurrentWindowOffset);
        var pWindowDc = pCurrentWindow + ImGuiWindowDcOffset;
        var currLineTextBaseOffset = *(float*)(pWindowDc + ImGuiWindowTempDataCurrLineTextBaseOffset);

        // Analyze the provided SeString and break it up to text fragments.
        this.CreateTextFragments(ref state, currLineTextBaseOffset);
        var fragmentSpan = CollectionsMarshal.AsSpan(this.fragments);

        // Calculate size.
        var size = Vector2.Zero;
        foreach (ref var fragment in fragmentSpan)
            size = Vector2.Max(size, fragment.Offset + new Vector2(fragment.VisibleWidth, state.Params.LineHeight));

        // If we're not drawing at all, stop further processing.
        if (state.Params.DrawList is null)
            return new() { Size = size };

        ImGuiNative.ImDrawListSplitter_Split(state.Splitter, state.Params.DrawList, ChannelCount);

        // Draw all text fragments.
        var lastRune = 0;
        foreach (ref var f in fragmentSpan)
        {
            var data = state.Raw.Data[f.From..f.To];
            this.DrawTextFragment(ref state, f.Offset, f.IsSoftHyphenVisible, data, lastRune, f.Link);
            lastRune = f.IsSoftHyphenVisible ? f.LastRune1 : f.LastRune2;
        }

        // Create an ImGui item, if a target draw list is not manually set.
        if (drawParams.TargetDrawList is null)
            ImGui.Dummy(size);

        // Handle link interactions.
        var clicked = false;
        var hoveredLinkOffset = -1;
        var activeLinkOffset = -1;
        if (imGuiId.PushId())
        {
            var invisibleButtonDrawn = false;
            foreach (ref readonly var f in fragmentSpan)
            {
                if (f.Link == -1)
                    continue;

                var pos = ImGui.GetMousePos() - state.ScreenOffset - f.Offset;
                var sz = new Vector2(f.AdvanceWidth, state.Params.LineHeight);
                if (pos is { X: >= 0, Y: >= 0 } && pos.X <= sz.X && pos.Y <= sz.Y)
                {
                    invisibleButtonDrawn = true;

                    var cursorPosBackup = ImGui.GetCursorScreenPos();
                    ImGui.SetCursorScreenPos(state.ScreenOffset + f.Offset);
                    clicked = ImGui.InvisibleButton("##link", sz, buttonFlags);
                    if (ImGui.IsItemHovered())
                        hoveredLinkOffset = f.Link;
                    if (ImGui.IsItemActive())
                        activeLinkOffset = f.Link;
                    ImGui.SetCursorScreenPos(cursorPosBackup);

                    break;
                }
            }

            // If no link was hovered and thus no invisible button is put, treat the whole area as the button.
            if (!invisibleButtonDrawn)
            {
                ImGui.SetCursorScreenPos(state.ScreenOffset);
                clicked = ImGui.InvisibleButton("##text", size, buttonFlags);
            }

            ImGui.PopID();
        }

        // If any link is being interacted, draw rectangles behind the relevant text fragments.
        if (hoveredLinkOffset != -1 || activeLinkOffset != -1)
        {
            state.SetCurrentChannel(ChannelLinkBackground);
            var color = activeLinkOffset == -1 ? state.Params.LinkHoverBackColor : state.Params.LinkActiveBackColor;
            foreach (ref readonly var fragment in fragmentSpan)
            {
                if (fragment.Link != hoveredLinkOffset && hoveredLinkOffset != -1)
                    continue;
                if (fragment.Link != activeLinkOffset && activeLinkOffset != -1)
                    continue;
                ImGuiNative.ImDrawList_AddRectFilled(
                    state.Params.DrawList,
                    state.ScreenOffset + fragment.Offset,
                    state.ScreenOffset + fragment.Offset + new Vector2(fragment.AdvanceWidth, state.Params.LineHeight),
                    color,
                    0,
                    ImDrawFlags.None);
            }
        }

        ImGuiNative.ImDrawListSplitter_Merge(state.Splitter, state.Params.DrawList);

        var payloadEnumerator = new ReadOnlySeStringSpan(
            hoveredLinkOffset == -1 ? ReadOnlySpan<byte>.Empty : sss.Data[hoveredLinkOffset..]).GetEnumerator();
        if (!payloadEnumerator.MoveNext())
            return new() { Size = size, Clicked = clicked, InteractedPayloadOffset = -1 };
        return new()
        {
            Size = size,
            Clicked = clicked,
            InteractedPayloadOffset = hoveredLinkOffset,
            InteractedPayloadEnvelope = sss.Data.Slice(hoveredLinkOffset, payloadEnumerator.Current.EnvelopeByteLength),
        };
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

    /// <summary>Swaps red and blue channels of a given color in ARGB(BB GG RR AA) and ABGR(RR GG BB AA).</summary>
    /// <param name="x">Color to process.</param>
    /// <returns>Swapped color.</returns>
    private static uint SwapRedBlue(uint x) => (x & 0xFF00FF00u) | ((x >> 16) & 0xFF) | ((x & 0xFF) << 16);

    private void ReleaseUnmanagedResources()
    {
        if (this.splitter is not null)
        {
            ImGuiNative.ImDrawListSplitter_destroy(this.splitter);
            this.splitter = null;
        }
    }

    /// <summary>Creates text fragment, taking line and word breaking into account.</summary>
    /// <param name="state">Draw state.</param>
    /// <param name="baseY">Y offset adjustment for all text fragments. Used to honor
    /// <see cref="ImGui.AlignTextToFramePadding"/>.</param>
    private void CreateTextFragments(ref DrawState state, float baseY)
    {
        var prev = 0;
        var xy = new Vector2(0, baseY);
        var w = 0f;
        var linkOffset = -1;
        foreach (var (breakAt, mandatory) in new LineBreakEnumerator(state.Raw, UtfEnumeratorFlags.Utf8SeString))
        {
            var nextLinkOffset = linkOffset;
            while (prev < breakAt)
            {
                var curr = breakAt;

                // Try to split by link payloads.
                foreach (var p in new ReadOnlySeStringSpan(state.Raw.Data[prev..breakAt]).GetOffsetEnumerator())
                {
                    if (p.Payload.MacroCode == MacroCode.Link)
                    {
                        nextLinkOffset =
                            p.Payload.TryGetExpression(out var e) &&
                            e.TryGetUInt(out var u) &&
                            u == (uint)LinkMacroPayloadType.Terminator
                                ? -1
                                : prev + p.Offset;

                        // Split only if we're not splitting at the beginning.
                        if (p.Offset != 0)
                        {
                            curr = prev + p.Offset;
                            break;
                        }

                        linkOffset = nextLinkOffset;
                    }
                }

                // Create a text fragment without applying wrap width limits for testing.
                var fragment = state.CreateFragment(this, prev, curr, curr == breakAt && mandatory, xy, linkOffset);

                // Test if the fragment does not fit into the current line and the current line is not empty.
                if (xy.X != 0 &&
                    this.fragments.Count > 0 &&
                    !this.fragments[^1].BreakAfter &&
                    xy.X + fragment.VisibleWidth > state.Params.WrapWidth)
                {
                    // The break unit as a whole does not fit into the current line. Advance to the next line.
                    xy.X = 0;
                    xy.Y += state.Params.LineHeight;
                    w = 0;
                    CollectionsMarshal.AsSpan(this.fragments)[^1].BreakAfter = true;
                    fragment.Offset = xy;
                }

                // See if it's possible to add the current fragment within wrap width.
                if (state.Params.WrapWidth <= Math.Max(w, xy.X + fragment.VisibleWidth))
                {
                    // Create a fragment again that fits into the given width limit.
                    var remainingWidth = state.Params.WrapWidth - xy.X;
                    fragment = state.CreateFragment(this, prev, curr, true, xy, linkOffset, remainingWidth);
                }
                else if (this.fragments.Count > 0 && xy.X != 0)
                {
                    // New fragment fits into the current line, and it has a previous fragment in the same line.
                    int lastFragmentEnd;
                    if (this.fragments[^1].EndsWithSoftHyphen)
                    {
                        // Previous fragment needs to be marked that its ending soft hyphen won't be shown.
                        xy.X += this.fragments[^1].AdvanceWidthWithoutLastRune -
                                this.fragments[^1].AdvanceWidth;
                        lastFragmentEnd = this.fragments[^1].LastRune2;
                    }
                    else
                    {
                        lastFragmentEnd = this.fragments[^1].LastRune1;
                    }

                    // Adjust this fragment's offset from kerning distance.
                    xy.X += state.CalculateDistance(lastFragmentEnd, fragment.FirstRune);
                    fragment.Offset = xy;
                }

                if (fragment.To == curr)
                    linkOffset = nextLinkOffset;
                w = Math.Max(w, xy.X + fragment.VisibleWidth);
                xy.X += fragment.AdvanceWidth;
                prev = fragment.To;
                this.fragments.Add(fragment);

                if (fragment.BreakAfter)
                {
                    xy.X = w = 0;
                    xy.Y += state.Params.LineHeight;
                }
            }
        }
    }

    /// <summary>Draws a text fragment.</summary>
    /// <param name="state">Draw state.</param>
    /// <param name="offset">Offset of left top corner of this text fragment in pixels w.r.t.
    /// <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
    /// <param name="displaySoftHyphen">Whether to display soft hyphens in this text fragment.</param>
    /// <param name="span">Byte span of the SeString fragment to draw.</param>
    /// <param name="lastRune">Rune that preceded this text fragment in the same line, or <c>0</c> if none.</param>
    /// <param name="link">Byte offset of the link payload that decorates this text fragment in
    /// <see cref="DrawState.Raw"/>, or <c>-1</c> if none.</param>
    private void DrawTextFragment(
        ref DrawState state,
        Vector2 offset,
        bool displaySoftHyphen,
        ReadOnlySpan<byte> span,
        int lastRune,
        int link)
    {
        var gfdTextureSrv =
            (nint)UIModule.Instance()->GetRaptureAtkModule()->AtkModule.AtkFontManager.Gfd->Texture->
                D3D11ShaderResourceView;
        var x = 0f;
        var width = 0f;
        foreach (var c in UtfEnumerator.From(span, UtfEnumeratorFlags.Utf8SeString))
        {
            var color = this.colorStack.Count == 0 ? state.Params.Color : this.colorStack[^1];
            var shadowColor = this.shadowColorStack.Count == 0 ? state.Params.ShadowColor : this.shadowColorStack[^1];

            if (c is { IsSeStringPayload: true, EffectiveInt: char.MaxValue or '\uFFFC' })
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
                        state.Params.Bold = u != 0;
                        continue;
                    case MacroCode.Italic when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        state.Params.Italic = u != 0;
                        continue;
                    case MacroCode.Edge when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        state.Params.Edge = u != 0;
                        continue;
                    case MacroCode.Shadow when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                        state.Params.Shadow = u != 0;
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
                            offset + new Vector2(x, MathF.Round((state.Params.LineHeight - size.Y) / 2)),
                            gfdTextureSrv,
                            Vector2.Zero,
                            size,
                            Vector2.Zero,
                            useHq ? gfdEntry.HqUv0 : gfdEntry.Uv0,
                            useHq ? gfdEntry.HqUv1 : gfdEntry.Uv1);
                        if (link != -1)
                            state.DrawLinkUnderline(offset + new Vector2(x, 0), size.X, color, shadowColor);

                        width = Math.Max(width, x + size.X);
                        x += MathF.Round(size.X);
                        lastRune = '\0';
                        continue;
                    }

                    default:
                        continue;
                }
            }

            var effectiveInt = c.EffectiveInt;
            if (effectiveInt == SoftHyphen && !displaySoftHyphen)
                continue;
            if (ToPrintableRune(effectiveInt) is not { } rune)
                continue;

            var currChar = rune.Value is >= 0 and < char.MaxValue ? (char)rune.Value : '\uFFFE';
            if (currChar != 0)
            {
                var dist = state.CalculateDistance(lastRune, currChar);
                ref var g = ref state.FindGlyph(currChar);

                var dxBold = state.Params.Bold ? 2 : 1;
                var dyItalic = state.Params.Italic
                                   ? new Vector2(state.Params.FontSize - g.Y0, state.Params.FontSize - g.Y1) / 6
                                   : Vector2.Zero;

                if (state.Params.Shadow && shadowColor >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelShadow);
                    for (var dx = 0; dx < dxBold; dx++)
                        state.Draw(offset + new Vector2(x + dist + dx, 1), g, dyItalic, shadowColor);
                }

                var useEdge = this.edgeColorStack.Count > 0 || state.Params.Edge;
                var activeEdgeColor =
                    state.Params.ForceEdgeColor
                        ? state.Params.EdgeColor
                        : this.edgeColorStack.Count == 0
                            ? state.Params.EdgeColor
                            : this.edgeColorStack[^1];
                activeEdgeColor = (activeEdgeColor & 0xFFFFFFu) | ((activeEdgeColor >> 26) << 24);
                if (useEdge && activeEdgeColor >= 0x1000000)
                {
                    state.SetCurrentChannel(ChannelEdge);
                    for (var dx = -1; dx <= dxBold; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            if (dx >= 0 && dx < dxBold && dy == 0)
                                continue;

                            state.Draw(offset + new Vector2(x + dist + dx, dy), g, dyItalic, activeEdgeColor);
                        }
                    }
                }

                state.SetCurrentChannel(ChannelFore);
                for (var dx = 0; dx < dxBold; dx++)
                    state.Draw(offset + new Vector2(x + dist + dx, 0), g, dyItalic, color);

                if (link != -1)
                    state.DrawLinkUnderline(offset + new Vector2(x + dist, 0), g.AdvanceX, color, shadowColor);

                width = Math.Max(width, x + dist + (g.X1 * state.FontSizeScale));
                x += dist + MathF.Round(g.AdvanceX * state.FontSizeScale);
            }

            lastRune = currChar;
        }

        return;

        static void TouchColorStack(List<uint> stack, ReadOnlySePayloadSpan payload)
        {
            if (!payload.TryGetExpression(out var expr))
                return;
            if (expr.TryGetPlaceholderExpression(out var p) && p == (int)ExpressionType.StackColor && stack.Count > 0)
                stack.RemoveAt(stack.Count - 1);
            else if (expr.TryGetUInt(out var u))
                stack.Add(SwapRedBlue(u) | 0xFF000000u);
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

    /// <summary>Determines a bitmap icon to display for the given SeString payload.</summary>
    /// <param name="sss">Byte span that should include a SeString payload.</param>
    /// <returns>Icon to display, or <see cref="None"/> if it should not be displayed as an icon.</returns>
    private BitmapFontIcon GetBitmapFontIconFor(ReadOnlySpan<byte> sss)
    {
        var e = new ReadOnlySeStringSpan(sss).GetEnumerator();
        if (!e.MoveNext() || e.Current.MacroCode is not MacroCode.Icon and not MacroCode.Icon2)
            return None;

        var payload = e.Current;
        switch (payload.MacroCode)
        {
            // Show the specified icon as-is.
            case MacroCode.Icon
                when payload.TryGetExpression(out var icon) && icon.TryGetInt(out var iconId):
                return (BitmapFontIcon)iconId;

            // Apply gamepad key mapping to icons.
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

    /// <summary>Represents a text fragment in a SeString span.</summary>
    /// <param name="From">Starting byte offset (inclusive) in a SeString.</param>
    /// <param name="To">Ending byte offset (exclusive) in a SeString.</param>
    /// <param name="Link">Byte offset of the link that decorates this text fragment, or <c>-1</c> if none.</param>
    /// <param name="Offset">Offset in pixels w.r.t. <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
    /// <param name="VisibleWidth">Visible width of this text fragment. This is the width required to draw everything
    /// without clipping.</param>
    /// <param name="AdvanceWidth">Advance width of this text fragment. This is the width required to add to the cursor
    /// to position the next fragment correctly.</param>
    /// <param name="AdvanceWidthWithoutLastRune">Same with <paramref name="AdvanceWidth"/>, but excluding the last
    /// rune.</param>
    /// <param name="BreakAfter">Whether to insert a line break after this text fragment.</param>
    /// <param name="EndsWithSoftHyphen">Whether this text fragment ends with a soft hyphen.</param>
    /// <param name="FirstRune">First rune in this text fragment.</param>
    /// <param name="LastRune1">Last rune in this text fragment. Referred if it is not a soft hyphen, for the purpose
    /// of calculating kerning distance with the following text fragment, if any.</param>
    /// <param name="LastRune2">Second last rune in this text fragment. Referred if <paramref name="LastRune1"/> is a
    /// soft hyphen, for the purpose of calculating kerning distance with the following text fragment, if any.</param>
    private record struct TextFragment(
        int From,
        int To,
        int Link,
        Vector2 Offset,
        float VisibleWidth,
        float AdvanceWidth,
        float AdvanceWidthWithoutLastRune,
        bool BreakAfter,
        bool EndsWithSoftHyphen,
        int FirstRune,
        int LastRune1,
        int LastRune2)
    {
        public bool IsSoftHyphenVisible => this.EndsWithSoftHyphen && this.BreakAfter;
    }

    /// <summary>Represents a temporary state required for drawing.</summary>
    private ref struct DrawState(
        ReadOnlySeStringSpan raw,
        SeStringDrawParams.Resolved @params,
        ImDrawListSplitter* splitter)
    {
        /// <summary>Raw SeString span.</summary>
        public readonly ReadOnlySeStringSpan Raw = raw;

        /// <summary>Multiplier value for glyph metrics, so that it scales to <see cref="SeStringDrawParams.FontSize"/>.
        /// </summary>
        public readonly float FontSizeScale = @params.FontSize / @params.Font->FontSize;

        /// <summary>Value obtained from <see cref="ImGui.GetCursorScreenPos"/>.</summary>
        public readonly Vector2 ScreenOffset = @params.ScreenOffset;

        /// <summary>Splitter to split <see cref="SeStringDrawParams.TargetDrawList"/>.</summary>
        public readonly ImDrawListSplitter* Splitter = splitter;

        /// <summary>Resolved draw parameters from the caller.</summary>
        public SeStringDrawParams.Resolved Params = @params;

        /// <summary>Calculates the size in pixels of a GFD entry when drawn.</summary>
        /// <param name="gfdEntry">GFD entry to determine the size.</param>
        /// <param name="useHq">Whether to draw the HQ texture.</param>
        /// <returns>Determined size of the GFD entry when drawn.</returns>
        public readonly Vector2 CalculateGfdEntrySize(in GfdFile.GfdEntry gfdEntry, out bool useHq)
        {
            useHq = this.Params.FontSize > 20;
            var targetHeight = useHq ? this.Params.FontSize : 20;
            return new(gfdEntry.Width * (targetHeight / gfdEntry.Height), targetHeight);
        }

        /// <summary>Sets the current channel in the ImGui draw list splitter.</summary>
        /// <param name="channelIndex">Channel to switch to.</param>
        public readonly void SetCurrentChannel(int channelIndex) =>
            ImGuiNative.ImDrawListSplitter_SetCurrentChannel(
                this.Splitter,
                this.Params.DrawList,
                channelIndex);

        /// <summary>Draws a single glyph.</summary>
        /// <param name="offset">Offset of the glyph in pixels w.r.t.
        /// <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
        /// <param name="g">Glyph to draw.</param>
        /// <param name="dyItalic">Transformation for <paramref name="g"/> that will push top and bottom pixels to
        /// apply faux italicization.</param>
        /// <param name="color">Color of the glyph.</param>
        public readonly void Draw(Vector2 offset, in ImGuiHelpers.ImFontGlyphReal g, Vector2 dyItalic, uint color) =>
            this.Draw(
                offset + new Vector2(
                    0,
                    MathF.Round(((this.Params.LineHeight - this.Params.Font->FontSize) * this.FontSizeScale) / 2f)),
                this.Params.Font->ContainerAtlas->Textures.Ref<ImFontAtlasTexture>(g.TextureIndex).TexID,
                g.XY0 * this.FontSizeScale,
                g.XY1 * this.FontSizeScale,
                dyItalic * this.FontSizeScale,
                g.UV0,
                g.UV1,
                color);

        /// <summary>Draws a single glyph.</summary>
        /// <param name="offset">Offset of the glyph in pixels w.r.t.
        /// <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
        /// <param name="igTextureId">ImGui texture ID to draw from.</param>
        /// <param name="xy0">Left top corner of the glyph w.r.t. its glyph origin in the target draw list.</param>
        /// <param name="xy1">Right bottom corner of the glyph w.r.t. its glyph origin in the target draw list.</param>
        /// <param name="dyItalic">Transformation for <paramref name="xy0"/> and <paramref name="xy1"/> that will push
        /// top and bottom pixels to apply faux italicization.</param>
        /// <param name="uv0">Left top corner of the glyph w.r.t. its glyph origin in the source texture.</param>
        /// <param name="uv1">Right bottom corner of the glyph w.r.t. its glyph origin in the source texture.</param>
        /// <param name="color">Color of the glyph.</param>
        public readonly void Draw(
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
            ImGuiNative.ImDrawList_AddImageQuad(
                this.Params.DrawList,
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

        /// <summary>Draws an underline, for links.</summary>
        /// <param name="offset">Offset of the glyph in pixels w.r.t.
        /// <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
        /// <param name="advanceWidth">Advance width of the glyph.</param>
        /// <param name="color">Color of the underline.</param>
        /// <param name="shadowColor">Color of the shadow under the underline.</param>
        public readonly void DrawLinkUnderline(Vector2 offset, float advanceWidth, uint color, uint shadowColor)
        {
            if (this.Params.LinkUnderlineThickness < 1f)
                return;

            var dy = (this.Params.LinkUnderlineThickness - 1) / 2f;
            dy += MathF.Round(
                (((this.Params.LineHeight - this.Params.FontSize) / 2) + this.Params.Font->Ascent) *
                this.FontSizeScale);
            this.SetCurrentChannel(ChannelLinkUnderline);
            ImGuiNative.ImDrawList_AddLine(
                this.Params.DrawList,
                this.ScreenOffset + offset + new Vector2(0, dy),
                this.ScreenOffset + offset + new Vector2(advanceWidth, dy),
                color,
                this.Params.LinkUnderlineThickness);

            if (this.Params.Shadow && shadowColor >= 0x1000000)
            {
                this.SetCurrentChannel(ChannelShadow);
                ImGuiNative.ImDrawList_AddLine(
                    this.Params.DrawList,
                    this.ScreenOffset + offset + new Vector2(0, dy + 1),
                    this.ScreenOffset + offset + new Vector2(advanceWidth, dy + 1),
                    shadowColor,
                    this.Params.LinkUnderlineThickness);
            }
        }

        /// <summary>Creates a text fragment.</summary>
        /// <param name="renderer">Associated renderer.</param>
        /// <param name="from">Starting byte offset (inclusive) in <see cref="Raw"/> that this fragment deals with.
        /// </param>
        /// <param name="to">Ending byte offset (exclusive) in <see cref="Raw"/> that this fragment deals with.</param>
        /// <param name="breakAfter">Whether to break line after this fragment.</param>
        /// <param name="offset">Offset in pixels w.r.t. <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
        /// <param name="activeLinkOffset">Byte offset of the link payload in <see cref="Raw"/> that decorates this
        /// text fragment.</param>
        /// <param name="wrapWidth">Optional wrap width to stop at while creating this text fragment. Note that at least
        /// one visible character needs to be there in a single text fragment, in which case it is allowed to exceed
        /// the wrap width.</param>
        /// <returns>Newly created text fragment.</returns>
        public readonly TextFragment CreateFragment(
            SeStringRenderer renderer,
            int from,
            int to,
            bool breakAfter,
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
            int firstRune = char.MaxValue;
            var lastRune1 = default(int);
            var lastRune2 = default(int);
            var endsWithSoftHyphen = false;
            foreach (var c in UtfEnumerator.From(this.Raw.Data[from..to], UtfEnumeratorFlags.Utf8SeString))
            {
                prevAdvanceWidth = x;
                lastRune2 = lastRune1;
                endsWithSoftHyphen = c.EffectiveChar == SoftHyphen;

                var byteOffset = from + c.ByteOffset;
                var isBreakableWhitespace = false;
                if (c is { IsSeStringPayload: true, MacroCode: MacroCode.Icon or MacroCode.Icon2 } &&
                    renderer.GetBitmapFontIconFor(this.Raw.Data[byteOffset..]) is var icon and not None &&
                    renderer.gfd.TryGetEntry((uint)icon, out var gfdEntry) &&
                    !gfdEntry.IsEmpty)
                {
                    // This is an icon payload.
                    var size = this.CalculateGfdEntrySize(gfdEntry, out _);
                    w = Math.Max(w, x + size.X);
                    x += MathF.Round(size.X);
                    lastRune1 = default;
                }
                else if (ToPrintableRune(c.EffectiveChar) is { } rune)
                {
                    // This is a printable character, or a standard whitespace character.
                    var dist = this.CalculateDistance(lastRune1, rune.Value);
                    ref var g = ref this.FindGlyph(rune.Value);
                    w = Math.Max(w, x + ((dist + g.X1) * this.FontSizeScale));
                    x += MathF.Round((dist + g.AdvanceX) * this.FontSizeScale);

                    isBreakableWhitespace = Rune.IsWhiteSpace(rune) &&
                                            UnicodeData.LineBreak[rune.Value] is not UnicodeLineBreakClass.GL;
                    lastRune1 = (char)g.Codepoint;
                }
                else
                {
                    continue;
                }

                if (firstRune == char.MaxValue)
                    firstRune = lastRune1;

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
                breakAfter,
                endsWithSoftHyphen,
                firstRune,
                lastRune1,
                lastRune2);
        }

        /// <summary>Gets the glyph corresponding to the given codepoint.</summary>
        /// <param name="codepoint">Codepoint of the glyph.</param>
        /// <returns>Corresponding glyph, or glyph of a fallback character specified from
        /// <see cref="ImFont.FallbackChar"/>.</returns>
        public readonly ref ImGuiHelpers.ImFontGlyphReal FindGlyph(int codepoint)
        {
            var p = codepoint is >= ushort.MinValue and <= ushort.MaxValue
                        ? ImGuiNative.ImFont_FindGlyph(this.Params.Font, (ushort)codepoint)
                        : this.Params.Font->FallbackGlyph;
            return ref *(ImGuiHelpers.ImFontGlyphReal*)p;
        }

        /// <summary>Gets the kerning adjustment between two glyphs in a succession corresponding to the given
        /// codepoints.</summary>
        /// <param name="codepoint1">Codepoint of the glyph on the left side of a pair.</param>
        /// <param name="codepoint2">Codepoint of the glyph on the right side of a pair.</param>
        /// <returns>Distance adjustment in pixels, scaled to the size specified from
        /// <see cref="SeStringDrawParams.FontSize"/>, and rounded.</returns>
        public readonly float CalculateDistance(int codepoint1, int codepoint2)
        {
            if (codepoint1 is <= 0 or > char.MaxValue)
                return 0;
            if (codepoint2 is <= 0 or > char.MaxValue)
                return 0;
            return MathF.Round(
                ImGuiNative.ImFont_GetDistanceAdjustmentForPair(
                    this.Params.Font,
                    (ushort)codepoint1,
                    (ushort)codepoint2) * this.FontSizeScale);
        }
    }
}
