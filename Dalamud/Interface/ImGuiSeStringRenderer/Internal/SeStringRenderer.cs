using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using BitFaster.Caching.Lru;
using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal.TextProcessing;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.Parse;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using static Dalamud.Game.Text.SeStringHandling.BitmapFontIcon;

namespace Dalamud.Interface.ImGuiSeStringRenderer.Internal;

/// <summary>Draws SeString.</summary>
[ServiceManager.EarlyLoadedService]
internal class SeStringRenderer : IServiceType
{
    private const int ImGuiContextCurrentWindowOffset = 0x3FF0;
    private const int ImGuiWindowDcOffset = 0x118;
    private const int ImGuiWindowTempDataCurrLineTextBaseOffset = 0x38;

    /// <summary>Soft hyphen character, which signifies that a word can be broken here, and will display a standard
    /// hyphen when it is broken there.</summary>
    private const int SoftHyphen = '\u00AD';

    /// <summary>Object replacement character, which signifies that there should be something else displayed in place
    /// of this placeholder. On its own, usually displayed like <c>[OBJ]</c>.</summary>
    private const int ObjectReplacementCharacter = '\uFFFC';

    /// <summary>Cache of compiled SeStrings from <see cref="CompileAndCache"/>.</summary>
    private readonly ConcurrentLru<string, ReadOnlySeString> cache = new(1024);

    /// <summary>Parsed <c>gfdata.gfd</c> file, containing bitmap font icon lookup table.</summary>
    private readonly GfdFile gfd;

    /// <summary>Parsed text fragments from a SeString.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly List<TextFragment> fragmentsMainThread = [];

    /// <summary>Color stacks to use while evaluating a SeString for rendering.</summary>
    /// <remarks>Touched only from the main thread.</remarks>
    private readonly SeStringColorStackSet colorStackSetMainThread;

    [ServiceManager.ServiceConstructor]
    private SeStringRenderer(DataManager dm, TargetSigScanner sigScanner)
    {
        this.colorStackSetMainThread = new(dm.Excel.GetSheet<UIColor>());
        this.gfd = dm.GetFile<GfdFile>("common/font/gfdata.gfd")!;
    }

    /// <summary>Compiles and caches a SeString from a text macro representation.</summary>
    /// <param name="text">SeString text macro representation.
    /// Newline characters will be normalized to newline payloads.</param>
    /// <returns>Compiled SeString.</returns>
    public ReadOnlySeString CompileAndCache(string text) =>
        this.cache.GetOrAdd(
            text,
            static text => ReadOnlySeString.FromMacroString(
                text.ReplaceLineEndings("<br>"),
                new() { ExceptionMode = MacroStringParseExceptionMode.EmbedError }));

    /// <summary>Creates a draw data that will draw the given SeString onto it.</summary>
    /// <param name="sss">SeString to render.</param>
    /// <param name="drawParams">Parameters for drawing.</param>
    /// <returns>A new self-contained draw data.</returns>
    public unsafe BufferBackedImDrawData CreateDrawData(
        ReadOnlySeStringSpan sss,
        scoped in SeStringDrawParams drawParams = default)
    {
        if (drawParams.TargetDrawList is not null)
        {
            throw new ArgumentException(
                $"{nameof(SeStringDrawParams.TargetDrawList)} may not be specified.",
                nameof(drawParams));
        }

        var dd = BufferBackedImDrawData.Create();

        try
        {
            var size = this.Draw(sss, drawParams with { TargetDrawList = dd.ListPtr }).Size;

            var offset = drawParams.ScreenOffset ?? Vector2.Zero;
            foreach (var vtx in new Span<ImDrawVert>(dd.ListPtr.VtxBuffer.Data, dd.ListPtr.VtxBuffer.Size))
                offset = Vector2.Min(offset, vtx.Pos);

            dd.Data.DisplayPos = offset;
            dd.Data.DisplaySize = size - offset;
            dd.Data.Valid = 1;
            dd.UpdateDrawDataStatistics();
            return dd;
        }
        catch
        {
            dd.Dispose();
            throw;
        }
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
        scoped in SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault) =>
        this.Draw(this.CompileAndCache(text).AsSpan(), drawParams, imGuiId, buttonFlags);

    /// <summary>Draws a SeString.</summary>
    /// <param name="utf8String">SeString to draw.</param>
    /// <param name="drawParams">Parameters for drawing.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <returns>Interaction result of the rendered text.</returns>
    public SeStringDrawResult Draw(
        scoped in Utf8String utf8String,
        scoped in SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault) =>
        this.Draw(utf8String.AsSpan(), drawParams, imGuiId, buttonFlags);

    /// <summary>Draws a SeString.</summary>
    /// <param name="sss">SeString to draw.</param>
    /// <param name="drawParams">Parameters for drawing.</param>
    /// <param name="imGuiId">ImGui ID, if link functionality is desired.</param>
    /// <param name="buttonFlags">Button flags to use on link interaction.</param>
    /// <returns>Interaction result of the rendered text.</returns>
    public unsafe SeStringDrawResult Draw(
        ReadOnlySeStringSpan sss,
        scoped in SeStringDrawParams drawParams = default,
        ImGuiId imGuiId = default,
        ImGuiButtonFlags buttonFlags = ImGuiButtonFlags.MouseButtonDefault)
    {
        // Interactivity is supported only from the main thread.
        if (!imGuiId.IsEmpty())
            ThreadSafety.AssertMainThread();

        if (drawParams.TargetDrawList is not null && imGuiId)
            throw new ArgumentException("ImGuiId cannot be set if TargetDrawList is manually set.", nameof(imGuiId));

        using var cleanup = new DisposeSafety.ScopedFinalizer();

        ImFont* font = null;
        if (drawParams.Font.HasValue)
            font = drawParams.Font.Value;

        // API14: Remove commented out code
        if (ThreadSafety.IsMainThread /* && drawParams.TargetDrawList is null */ && font is null)
            font = ImGui.GetFont();
        if (font is null)
            throw new ArgumentException("Specified font is empty.");

        // This also does argument validation for drawParams. Do it here.
        // `using var` makes a struct read-only, but we do want to modify it.
        using var stateStorage = new SeStringDrawState(
            sss,
            drawParams,
            ThreadSafety.IsMainThread ? this.colorStackSetMainThread : new(this.colorStackSetMainThread.ColorTypes),
            ThreadSafety.IsMainThread ? this.fragmentsMainThread : [],
            font);
        ref var state = ref Unsafe.AsRef(in stateStorage);

        // Analyze the provided SeString and break it up to text fragments.
        this.CreateTextFragments(ref state);
        var fragmentSpan = CollectionsMarshal.AsSpan(state.Fragments);

        // Calculate size.
        var size = Vector2.Zero;
        foreach (ref var fragment in fragmentSpan)
            size = Vector2.Max(size, fragment.Offset + new Vector2(fragment.VisibleWidth, state.LineHeight));

        // If we're not drawing at all, stop further processing.
        if (state.DrawList.Handle is null)
            return new() { Size = size };

        state.SplitDrawList();

        var itemSize = size;
        if (drawParams.TargetDrawList is null)
        {
            // Handle cases where ImGui.AlignTextToFramePadding has been called.
            var currLineTextBaseOffset = ImGui.GetCurrentContext().CurrentWindow.DC.CurrLineTextBaseOffset;
            if (currLineTextBaseOffset != 0f)
            {
                itemSize.Y += 2 * currLineTextBaseOffset;
                foreach (ref var f in fragmentSpan)
                    f.Offset += new Vector2(0, currLineTextBaseOffset);
            }
        }

        // Draw all text fragments.
        var lastRune = default(Rune);
        foreach (ref var f in fragmentSpan)
        {
            var data = state.Span[f.From..f.To];
            if (f.Entity)
                f.Entity.Draw(state, f.From, f.Offset);
            else
                this.DrawTextFragment(ref state, f.Offset, f.IsSoftHyphenVisible, data, lastRune, f.Link);
            lastRune = f.LastRune;
        }

        // Create an ImGui item, if a target draw list is not manually set.
        if (drawParams.TargetDrawList is null)
            ImGui.Dummy(itemSize);

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
                var sz = new Vector2(f.AdvanceWidth, state.LineHeight);
                if (pos is { X: >= 0, Y: >= 0 } && pos.X <= sz.X && pos.Y <= sz.Y)
                {
                    invisibleButtonDrawn = true;

                    var cursorPosBackup = ImGui.GetCursorScreenPos();
                    ImGui.SetCursorScreenPos(state.ScreenOffset + f.Offset);
                    clicked = ImGui.InvisibleButton("##link"u8, sz, buttonFlags);
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
                clicked = ImGui.InvisibleButton("##text"u8, itemSize, buttonFlags);
            }

            ImGui.PopID();
        }

        // If any link is being interacted, draw rectangles behind the relevant text fragments.
        if (hoveredLinkOffset != -1 || activeLinkOffset != -1)
        {
            state.SetCurrentChannel(SeStringDrawChannel.Background);
            var color = activeLinkOffset == -1 ? state.LinkHoverBackColor : state.LinkActiveBackColor;
            color = ColorHelpers.ApplyOpacity(color, state.Opacity);
            foreach (ref readonly var fragment in fragmentSpan)
            {
                if (fragment.Link != hoveredLinkOffset && hoveredLinkOffset != -1)
                    continue;
                if (fragment.Link != activeLinkOffset && activeLinkOffset != -1)
                    continue;
                var offset = state.ScreenOffset + fragment.Offset;
                state.DrawList.AddRectFilled(
                    offset,
                    offset + new Vector2(fragment.AdvanceWidth, state.LineHeight),
                    color);
            }
        }

        state.MergeDrawList();

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

    /// <summary>Gets the effective char for the given char, or null(\0) if it should not be handled at all.</summary>
    /// <param name="rune">Character to determine.</param>
    /// <param name="displayRune">Corresponding rune.</param>
    /// <param name="displaySoftHyphen">Whether to display soft hyphens.</param>
    /// <returns>Rune corresponding to the unicode codepoint to process, or null(\0) if none.</returns>
    private static bool TryGetDisplayRune(Rune rune, out Rune displayRune, bool displaySoftHyphen = true)
    {
        displayRune = rune.Value switch
        {
            0 or char.MaxValue => default,
            SoftHyphen => displaySoftHyphen ? new('-') : default,
            _ when UnicodeData.LineBreak[rune.Value]
                       is UnicodeLineBreakClass.BK
                       or UnicodeLineBreakClass.CR
                       or UnicodeLineBreakClass.LF
                       or UnicodeLineBreakClass.NL => new(0),
            _ => rune,
        };
        return displayRune.Value != 0;
    }

    /// <summary>Creates text fragment, taking line and word breaking into account.</summary>
    /// <param name="state">Draw state.</param>
    private void CreateTextFragments(ref SeStringDrawState state)
    {
        var prev = 0;
        var xy = Vector2.Zero;
        var w = 0f;
        var link = -1;
        foreach (var (breakAt, mandatory) in new LineBreakEnumerator(state.Span, UtfEnumeratorFlags.Utf8SeString))
        {
            // Might have happened if custom entity was longer than the previous break unit.
            if (prev > breakAt)
                continue;

            var nextLink = link;
            for (var first = true; prev < breakAt; first = false)
            {
                var curr = breakAt;
                var entity = default(SeStringReplacementEntity);

                // Try to split by link payloads and custom entities.
                foreach (var p in new ReadOnlySeStringSpan(state.Span[prev..breakAt]).GetOffsetEnumerator())
                {
                    var break2 = false;
                    switch (p.Payload.Type)
                    {
                        case ReadOnlySePayloadType.Text when state.GetEntity is { } getEntity:
                            foreach (var oe in UtfEnumerator.From(p.Payload.Body, UtfEnumeratorFlags.Utf8))
                            {
                                var entityOffset = prev + p.Offset + oe.ByteOffset;
                                entity = getEntity(state, entityOffset);
                                if (!entity)
                                    continue;

                                if (prev == entityOffset)
                                {
                                    curr = entityOffset + entity.ByteLength;
                                }
                                else
                                {
                                    entity = default;
                                    curr = entityOffset;
                                }

                                break2 = true;
                                break;
                            }

                            break;

                        case ReadOnlySePayloadType.Macro when
                            state.GetEntity is { } getEntity &&
                            getEntity(state, prev + p.Offset) is { ByteLength: > 0 } entity1:
                            entity = entity1;
                            if (p.Offset == 0)
                            {
                                curr = prev + p.Offset + entity.ByteLength;
                            }
                            else
                            {
                                entity = default;
                                curr = prev + p.Offset;
                            }

                            break2 = true;
                            break;

                        case ReadOnlySePayloadType.Macro when p.Payload.MacroCode == MacroCode.Link:
                        {
                            nextLink =
                                p.Payload.TryGetExpression(out var e) &&
                                e.TryGetUInt(out var u) &&
                                u == (uint)LinkMacroPayloadType.Terminator
                                    ? -1
                                    : prev + p.Offset;

                            // Split only if we're not splitting at the beginning.
                            if (p.Offset != 0)
                            {
                                curr = prev + p.Offset;
                                break2 = true;
                                break;
                            }

                            link = nextLink;

                            break;
                        }

                        case ReadOnlySePayloadType.Invalid:
                        default:
                            break;
                    }

                    if (break2) break;
                }

                // Create a text fragment without applying wrap width limits for testing.
                var fragment = this.CreateFragment(state, prev, curr, curr == breakAt && mandatory, xy, link, entity);
                var overflows = Math.Max(w, xy.X + fragment.VisibleWidth) > state.WrapWidth;

                // Test if the fragment does not fit into the current line and the current line is not empty.
                if (xy.X != 0 && state.Fragments.Count > 0 && !state.Fragments[^1].BreakAfter && overflows)
                {
                    // Introduce break if this is the first time testing the current break unit or the current fragment
                    // is an entity.
                    if (first || entity)
                    {
                        // The break unit as a whole does not fit into the current line. Advance to the next line.
                        xy.X = 0;
                        xy.Y += state.LineHeight;
                        w = 0;
                        CollectionsMarshal.AsSpan(state.Fragments)[^1].BreakAfter = true;
                        fragment.Offset = xy;

                        // Now that the fragment is given its own line, test if it overflows again.
                        overflows = fragment.VisibleWidth > state.WrapWidth;
                    }
                }

                if (overflows)
                {
                    // A replacement entity may not be broken down further.
                    if (!entity)
                    {
                        // Create a fragment again that fits into the given width limit.
                        var remainingWidth = state.WrapWidth - xy.X;
                        fragment = this.CreateFragment(state, prev, curr, true, xy, link, entity, remainingWidth);
                    }
                }
                else if (state.Fragments.Count > 0 && xy.X != 0)
                {
                    // New fragment fits into the current line, and it has a previous fragment in the same line.
                    // If the previous fragment ends with a soft hyphen, adjust its width so that the width of its
                    // trailing soft hyphens are not considered.
                    if (state.Fragments[^1].EndsWithSoftHyphen)
                        xy.X += state.Fragments[^1].AdvanceWidthWithoutSoftHyphen - state.Fragments[^1].AdvanceWidth;

                    // Adjust this fragment's offset from kerning distance.
                    xy.X += state.CalculateScaledDistance(state.Fragments[^1].LastRune, fragment.FirstRune);
                    fragment.Offset = xy;
                }

                // If the fragment was not broken by wrap width, update the link payload offset.
                if (fragment.To == curr)
                    link = nextLink;

                w = Math.Max(w, xy.X + fragment.VisibleWidth);
                xy.X += fragment.AdvanceWidth;
                prev = fragment.To;
                state.Fragments.Add(fragment);

                if (fragment.BreakAfter)
                {
                    xy.X = w = 0;
                    xy.Y += state.LineHeight;
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
    /// <see cref="SeStringDrawState.Span"/>, or <c>-1</c> if none.</param>
    private void DrawTextFragment(
        ref SeStringDrawState state,
        Vector2 offset,
        bool displaySoftHyphen,
        ReadOnlySpan<byte> span,
        Rune lastRune,
        int link)
    {
        // This might temporarily return 0 while logging in.
        var gfdTextureSrv = GetGfdTextureSrv();
        var x = 0f;
        var width = 0f;
        foreach (var c in UtfEnumerator.From(span, UtfEnumeratorFlags.Utf8SeString))
        {
            if (c is { IsSeStringPayload: true, EffectiveInt: char.MaxValue or ObjectReplacementCharacter })
            {
                var enu = new ReadOnlySeStringSpan(span[c.ByteOffset..]).GetOffsetEnumerator();
                if (!enu.MoveNext())
                    continue;

                if (state.HandleStyleAdjustingPayloads(enu.Current.Payload))
                    continue;

                if (this.GetBitmapFontIconFor(span[c.ByteOffset..]) is var icon and not None &&
                    this.gfd.TryGetEntry((uint)icon, out var gfdEntry) &&
                    !gfdEntry.IsEmpty)
                {
                    var size = gfdEntry.CalculateScaledSize(state.FontSize, out var useHq);
                    state.SetCurrentChannel(SeStringDrawChannel.Foreground);
                    if (gfdTextureSrv != 0)
                    {
                        state.Draw(
                            new(gfdTextureSrv),
                            offset + new Vector2(x, MathF.Round((state.LineHeight - size.Y) / 2)),
                            size,
                            useHq ? gfdEntry.HqUv0 : gfdEntry.Uv0,
                            useHq ? gfdEntry.HqUv1 : gfdEntry.Uv1,
                            ColorHelpers.ApplyOpacity(uint.MaxValue, state.Opacity));
                    }

                    if (link != -1)
                        state.DrawLinkUnderline(offset + new Vector2(x, 0), size.X);

                    width = Math.Max(width, x + size.X);
                    x += MathF.Round(size.X);
                    lastRune = default;
                }

                continue;
            }

            if (!TryGetDisplayRune(c.EffectiveRune, out var rune, displaySoftHyphen))
                continue;

            ref var g = ref state.FindGlyph(ref rune);
            var dist = state.CalculateScaledDistance(lastRune, rune);
            var advanceWidth = MathF.Round(g.AdvanceX * state.FontSizeScale);
            lastRune = rune;

            state.DrawGlyph(g, offset + new Vector2(x + dist, 0));
            if (link != -1)
                state.DrawLinkUnderline(offset + new Vector2(x + dist, 0), advanceWidth);

            width = Math.Max(width, x + dist + (g.X1 * state.FontSizeScale));
            x += dist + advanceWidth;
        }

        return;

        static unsafe nint GetGfdTextureSrv()
        {
            var uim = UIModule.Instance();
            if (uim is null)
                return 0;

            var ram = uim->GetRaptureAtkModule();
            if (ram is null)
                return 0;

            var gfd = ram->AtkModule.AtkFontManager.Gfd;
            if (gfd is null)
                return 0;

            var tex = gfd->Texture;
            if (tex is null)
                return 0;

            return (nint)tex->D3D11ShaderResourceView;
        }
    }

    /// <summary>Determines a bitmap icon to display for the given SeString payload.</summary>
    /// <param name="sss">Byte span that should include a SeString payload.</param>
    /// <returns>Icon to display, or <see cref="None"/> if it should not be displayed as an icon.</returns>
    private unsafe BitmapFontIcon GetBitmapFontIconFor(ReadOnlySpan<byte> sss)
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
                ref var iconMapping = ref RaptureAtkModule.Instance()->AtkFontManager.Icon2RemapTable;
                for (var i = 0; i < 30; i++)
                {
                    if (iconMapping[i].IconId == iconId)
                    {
                        return (BitmapFontIcon)iconMapping[i].RemappedIconId;
                    }
                }

                return (BitmapFontIcon)iconId;
        }

        return None;
    }

    /// <summary>Creates a text fragment.</summary>
    /// <param name="state">Draw state.</param>
    /// <param name="from">Starting byte offset (inclusive) in <see cref="SeStringDrawState.Span"/> that this fragment
    ///     deals with.</param>
    /// <param name="to">Ending byte offset (exclusive) in <see cref="SeStringDrawState.Span"/> that this fragment deals
    ///     with.</param>
    /// <param name="breakAfter">Whether to break line after this fragment.</param>
    /// <param name="offset">Offset in pixels w.r.t. <see cref="SeStringDrawParams.ScreenOffset"/>.</param>
    /// <param name="link">Byte offset of the link payload in <see cref="SeStringDrawState.Span"/> that
    ///     decorates this text fragment.</param>
    /// <param name="entity">Entity to display in place of this fragment.</param>
    /// <param name="wrapWidth">Optional wrap width to stop at while creating this text fragment. Note that at least
    ///     one visible character needs to be there in a single text fragment, in which case it is allowed to exceed
    ///     the wrap width.</param>
    /// <returns>Newly created text fragment.</returns>
    private TextFragment CreateFragment(
        scoped in SeStringDrawState state,
        int from,
        int to,
        bool breakAfter,
        Vector2 offset,
        int link,
        SeStringReplacementEntity entity,
        float wrapWidth = float.MaxValue)
    {
        if (entity)
        {
            return new(
                from,
                to,
                link,
                offset,
                entity,
                entity.Size.X,
                entity.Size.X,
                entity.Size.X,
                false,
                false,
                default,
                default);
        }

        var x = 0f;
        var w = 0f;
        var visibleWidth = 0f;
        var advanceWidth = 0f;
        var advanceWidthWithoutSoftHyphen = 0f;
        var firstDisplayRune = default(Rune?);
        var lastDisplayRune = default(Rune);
        var lastNonSoftHyphenRune = default(Rune);
        var endsWithSoftHyphen = false;
        foreach (var c in UtfEnumerator.From(state.Span[from..to], UtfEnumeratorFlags.Utf8SeString))
        {
            var byteOffset = from + c.ByteOffset;
            var isBreakableWhitespace = false;
            var effectiveRune = c.EffectiveRune;
            Rune displayRune;
            if (c is { IsSeStringPayload: true, MacroCode: MacroCode.Icon or MacroCode.Icon2 } &&
                this.GetBitmapFontIconFor(state.Span[byteOffset..]) is var icon and not None &&
                this.gfd.TryGetEntry((uint)icon, out var gfdEntry) &&
                !gfdEntry.IsEmpty)
            {
                // This is an icon payload.
                var size = gfdEntry.CalculateScaledSize(state.FontSize, out _);
                w = Math.Max(w, x + size.X);
                x += MathF.Round(size.X);
                displayRune = default;
            }
            else if (TryGetDisplayRune(effectiveRune, out displayRune))
            {
                // This is a printable character, or a standard whitespace character.
                ref var g = ref state.FindGlyph(ref displayRune);
                var dist = state.CalculateScaledDistance(lastDisplayRune, displayRune);
                w = Math.Max(w, x + dist + MathF.Round(g.X1 * state.FontSizeScale));
                x += dist + MathF.Round(g.AdvanceX * state.FontSizeScale);

                isBreakableWhitespace =
                    Rune.IsWhiteSpace(displayRune) &&
                    UnicodeData.LineBreak[displayRune.Value] is not UnicodeLineBreakClass.GL;
            }
            else
            {
                continue;
            }

            if (isBreakableWhitespace)
            {
                advanceWidth = x;
            }
            else
            {
                if (firstDisplayRune is not null && w > wrapWidth && effectiveRune.Value != SoftHyphen)
                {
                    to = byteOffset;
                    break;
                }

                advanceWidth = x;
                visibleWidth = w;
            }

            firstDisplayRune ??= displayRune;
            lastDisplayRune = displayRune;
            endsWithSoftHyphen = effectiveRune.Value == SoftHyphen;
            if (!endsWithSoftHyphen)
            {
                advanceWidthWithoutSoftHyphen = x;
                lastNonSoftHyphenRune = displayRune;
            }
        }

        return new(
            from,
            to,
            link,
            offset,
            entity,
            visibleWidth,
            advanceWidth,
            advanceWidthWithoutSoftHyphen,
            breakAfter,
            endsWithSoftHyphen,
            firstDisplayRune ?? default,
            lastNonSoftHyphenRune);
    }
}
