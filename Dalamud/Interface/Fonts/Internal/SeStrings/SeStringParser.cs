using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Interface.Fonts.Internal.Sources;
using Lumina.Text;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

using static Dalamud.Game.Text.BitmapFontIcon;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>Reads a SeString into styled text. It makes the text displayed, styled runs, inline objects for icons and
/// entities, and links.</summary>
/// <remarks>Main thread only.</remarks>
internal sealed class SeStringParser
{
    private const int SoftHyphen = 0xAD;
    private const int NoBreakSpace = 0xA0;
    private const int ObjectReplacementCharacter = 0xFFFC;

    private readonly SeStringColorStackSet colorStacks = new();
    private StyledText output = null!;
    private int sourceOffset;
    private ulong currentUserData;
    private TextStyle currentStyle;
    private int currentRunBegin;
    private bool hasRun;

    // Whether a payload changed the style since the current run started.
    private bool styleChanged;

    // The cached results of StyleNow and FontNow. A payload that changes the style clears them.
    private TextStyle? styleNow;
    private FontImpl? fontNow;

    private FontImpl font = null!;
    private float fontSize;
    private float blockSize;
    private FontMetrics metrics;
    private ITextMarkupCallback[] callbacks = [];
    private TextMarkup markup;
    private int themeIndex;
    private TextEdge edgeTemplate;
    private TextShadow shadowTemplate;
    private TextStyle startStyle;

    // The style of StyledText.DefaultFont. Text in it outside links needs no run.
    private TextStyle defaultStyle;
    private (int Link, TextStyle Style, TextStyle LinkStyle) lastLinkStyle = (-1, default, default);
    private bool edge;
    private bool shadow;
    private bool bold;
    private bool italic;

    /// <summary>Reads a SeString.</summary>
    /// <param name="seString">The SeString, which becomes <see cref="StyledText.Source"/>.</param>
    /// <param name="textMarkup">The markup the text is read for.</param>
    /// <param name="styledFont">The font, whose style the text starts with.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="chain">The callbacks, in order.</param>
    /// <param name="theme">The theme for <see cref="MacroCode.ColorType"/> and <see cref="MacroCode.EdgeColorType"/>.
    /// </param>
    /// <param name="styled">Receives the styled text, cleared.</param>
    /// <remarks>It keeps no reference to the callbacks or the styled text afterwards, so that plugins can unload.
    /// </remarks>
    public void Parse(
        byte[] seString, TextMarkup textMarkup, FontImpl styledFont, in TextLayoutDesc desc,
        ITextMarkupCallback[] chain, int theme, StyledText styled)
    {
        try
        {
            this.ParseCore(seString, textMarkup, styledFont, desc, chain, theme, styled);
        }
        finally
        {
            this.output = null!;
            this.callbacks = [];
            this.font = null!;
            this.startStyle = this.defaultStyle = this.currentStyle = default;
            this.lastLinkStyle = (-1, default, default);
            this.styleNow = null;
            this.fontNow = null;
        }
    }

    // The bitmap icon to display for an icon payload, with gamepad key mapping for Icon2.
    private static BitmapFontIcon GetBitmapFontIconFor(ReadOnlySePayloadSpan payload)
    {
        if (!payload.TryGetExpression(out var icon) || !icon.TryGetInt(out var iconId))
            return None;
        if (payload.MacroCode == MacroCode.Icon2)
            iconId = SeStringGameData.RemapIcon2(iconId);
        return (BitmapFontIcon)iconId;
    }

    // The entity data is the ID of the icon's entry.
    private static unsafe void DrawIcon(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context)
    {
        if (SeStringGameData.Gfd is not { } gfd || !gfd.TryGetEntry((uint)context.Data, out var entry))
            return;

        // The texture may not exist when the layout is made, so it is asked whenever an icon is drawn.
        var texture = SeStringGameData.GetGfdTexture();
        if (texture.TexData is null && texture.TexID.IsNull)
            return;
        entry.CalculateScaledSize(context.FontSize, out var hq);
        var tint = ColorHelpers.ApplyOpacity(uint.MaxValue, (context.Color >> 24) / 255f);
        drawList.AddImage(
            texture,
            topLeft,
            topLeft + context.Size,
            hq ? entry.HqUv0 : entry.Uv0,
            hq ? entry.HqUv1 : entry.Uv1,
            tint);
    }

    // The top of an object centered in the line, above the baseline. A line places its baseline in it as
    // FontMetrics.BaselineIn does, so the height of the line cancels out.
    private float CenteredTop(float height) =>
        MathF.Round(this.metrics.Ascent) - ((this.metrics.Ascent + this.metrics.Descent) / 2) + (height / 2);

    private void ParseCore(
        byte[] seString, TextMarkup textMarkup, FontImpl styledFont, in TextLayoutDesc desc,
        ITextMarkupCallback[] chain, int theme, StyledText styled)
    {
        this.output = styled;
        styled.Source = seString;
        this.hasRun = false;

        this.markup = textMarkup;
        this.font = styledFont;
        this.fontSize = desc.EmSize;
        this.metrics = styledFont.Font.GetMetrics(desc.EmSize, desc.Density);
        this.blockSize = FontMetrics.BlockSize(desc.LineHeight, desc.Size);
        this.callbacks = chain;
        this.themeIndex = theme;
        this.lastLinkStyle = (-1, default, default);

        // The font's style is where the text starts. Macros may turn on edges and shadows it does not have.
        ref readonly var style = ref styledFont.Style;
        var fontEdge = style.Edge;
        var fontShadow = style.Shadow;
        this.edge = fontEdge.IsOn;
        this.shadow = fontShadow.IsOn;
        this.colorStacks.Initialize(
            style.Color,
            this.edge ? fontEdge.Color : 0xFF000000,
            this.shadow ? fontShadow.Color : 0xFF000000);
        // Edges that macros turn on are the font's in their own color, or the default edge if the font has none.
        this.edgeTemplate = this.edge ? fontEdge : new(0xFF000000);
        this.shadowTemplate = this.shadow ? fontShadow : new(0xFF000000);

        // The family, sizes, background, lines and extra decoration stay as they are.
        this.startStyle = style;
        this.bold = style.Bold;
        this.italic = style.Italic;
        this.InvalidateStyle();
        this.defaultStyle = this.StyleNow();
        styled.DefaultFont = this.FontNow();

        var link = -1;
        var skipUntil = 0;
        foreach (var p in new ReadOnlySeStringSpan(seString).GetOffsetEnumerator())
        {
            // An entity may cover only the start of a text payload. The rest of it is read rune by rune.
            if (p.Offset < skipUntil &&
                (p.Payload.Type != ReadOnlySePayloadType.Text || p.Offset + p.Payload.Body.Length <= skipUntil))
            {
                continue;
            }

            switch (p.Payload.Type)
            {
                case ReadOnlySePayloadType.Text:
                    // The same flags as the layout. Byte order marks are text, and U+FFFE is yielded rather than
                    // skipped.
                    var body = p.Payload.Body;
                    var nextFound = styled.Links.Count;
                    var foundEnd = link < 0 && chain.Length != 0
                                       ? nextFound + styled.FindLinks(textMarkup, chain, p.Offset, body.Length)
                                       : nextFound;
                    foreach (var c in UtfEnumerator.From(
                                 body,
                                 UtfEnumeratorFlags.DisrespectByteOrderMask | UtfEnumeratorFlags.YieldByteOrderMask |
                                 UtfEnumeratorFlags.Utf8))
                    {
                        var at = p.Offset + c.ByteOffset;
                        if (at < skipUntil)
                            continue;
                        this.sourceOffset = at;
                        var runLink = link >= 0 ? link : this.FoundLinkAt(at, ref nextFound, foundEnd);
                        if (this.TryAddEntity(seString, at, runLink, out var length))
                        {
                            skipUntil = at + length;
                            continue;
                        }

                        this.AddRune(c.EffectiveRune, runLink);
                    }

                    break;

                case ReadOnlySePayloadType.Macro:
                    this.sourceOffset = p.Offset;
                    if (this.TryAddEntity(seString, p.Offset, link, out var macroLength))
                    {
                        skipUntil = p.Offset + macroLength;
                        break;
                    }

                    if (!this.ShouldHandle(p.Payload) || this.HandleStyleAdjustingPayload(p.Payload))
                        break;

                    switch (p.Payload.MacroCode)
                    {
                        case MacroCode.NewLine:
                            this.AddRune(new('\n'), link);
                            break;

                        case MacroCode.SoftHyphen:
                            this.AddRune(new(SoftHyphen), link);
                            break;

                        case MacroCode.NonBreakingSpace:
                            this.AddRune(new(NoBreakSpace), link);
                            break;

                        case MacroCode.Hyphen:
                            this.AddRune(new('-'), link);
                            break;

                        case MacroCode.Link
                            when p.Payload.TryGetExpression(out var e) &&
                                 e.TryGetUInt(out var u) &&
                                 u == (uint)LinkMacroPayloadType.Terminator:
                            link = -1;
                            break;

                        case MacroCode.Link:
                        {
                            var length = p.Payload.EnvelopeByteLength;
                            link = styled.Links.Count;
                            styled.Links.Add(new(p.Offset, length, seString.AsMemory(p.Offset, length)));
                            break;
                        }

                        case MacroCode.Icon or MacroCode.Icon2
                            when GetBitmapFontIconFor(p.Payload) is var icon and not None &&
                                 SeStringGameData.Gfd is { } gfd &&
                                 gfd.TryGetEntry((uint)icon, out var gfdEntry):
                        {
                            var iconSize = gfdEntry.CalculateScaledSize(this.fontSize, out _);
                            var top = this.CenteredTop(iconSize.Y);
                            var entity = new TextEntity(p.Payload.EnvelopeByteLength, iconSize, DrawIcon)
                            {
                                Data = gfdEntry.Id,
                            };

                            this.AddObject(
                                new(new(0, -top, MathF.Round(iconSize.X), iconSize.Y - top)),
                                link,
                                new(entity, p.Offset, this.FontNow(), iconSize));
                            break;
                        }
                    }

                    break;

                case ReadOnlySePayloadType.Invalid:
                default:
                    break;
            }
        }

        this.EndRun();
    }

    private bool ShouldHandle(ReadOnlySePayloadSpan payload)
    {
        foreach (var callback in this.callbacks)
        {
            if (callback is not ISeStringCallback seStringCallback)
                continue;
            try
            {
                if (seStringCallback.ShouldHandle(payload) is { } answer)
                    return answer;
            }
            catch (Exception e)
            {
                StyledTextHook.ReportCallbackFailure(callback.GetType(), e);
            }
        }

        return true;
    }

    private bool HandleStyleAdjustingPayload(ReadOnlySePayloadSpan payload)
    {
        if (!this.colorStacks.TryHandle(payload, this.themeIndex))
        {
            switch (payload.MacroCode)
            {
                case MacroCode.Bold when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                    this.bold = u != 0;
                    break;
                case MacroCode.Italic when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                    this.italic = u != 0;
                    break;
                case MacroCode.Edge when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                    this.edge = u != 0;
                    break;
                case MacroCode.Shadow when payload.TryGetExpression(out var e) && e.TryGetUInt(out var u):
                    this.shadow = u != 0;
                    break;
                default:
                    return false;
            }
        }

        this.InvalidateStyle();
        return true;
    }

    private void InvalidateStyle()
    {
        this.styleChanged = true;
        this.styleNow = null;
        this.fontNow = null;
    }

    private FontImpl FontOf(in TextStyle style) => this.font.Resolver.GetImpl(style);

    // Returns the active style, cached until a payload mutates state. Color 0 inherits the draw color, and any other
    // zero-alpha color hides text. Disabled edges and shadows normalize to default for style equality, and payload
    // edge and shadow colors inherit the initial alpha.
    private TextStyle StyleNow()
    {
        if (this.styleNow is { } cached)
            return cached;

        var stacks = this.colorStacks;
        var color = stacks.Color;
        var style = this.startStyle with
        {
            Color = color is 0 or >= 0x1000000 ? color : 0x00FFFFFFu,
            Edge = (this.edge || stacks.HasAdditionalEdgeColor) && stacks.EdgeColor >= 0x1000000
                       ? this.edgeTemplate with
                       {
                           Color = (stacks.EdgeColor & 0x00FFFFFFu) | (this.edgeTemplate.Color & 0xFF000000u),
                       }
                       : default,
            Shadow = this.shadow && stacks.ShadowColor >= 0x1000000
                         ? this.shadowTemplate with
                         {
                             Color = (stacks.ShadowColor & 0x00FFFFFFu) | (this.shadowTemplate.Color & 0xFF000000u),
                         }
                         : default,
            Bold = this.bold,
            Italic = this.italic,
        };
        this.styleNow = style;
        return style;
    }

    private FontImpl FontNow() => this.fontNow ??= this.FontOf(this.StyleNow());

    private bool TryAddEntity(ReadOnlySpan<byte> seString, int byteOffset, int link, out int byteLength)
    {
        byteLength = 0;
        if (this.callbacks.Length == 0)
            return false;
        var font = this.FontNow();
        var context = new TextEntityContext(this.markup, seString, byteOffset, font, this.fontSize, 0, default, 0);
        foreach (var callback in this.callbacks)
        {
            TextEntity entity;
            try
            {
                if (!callback.TryGetEntity(context, out entity))
                    continue;
            }
            catch (Exception e)
            {
                StyledTextHook.ReportCallbackFailure(callback.GetType(), e);
                continue;
            }

            if (entity.Length <= 0 || entity.Length > seString.Length - byteOffset)
                continue;

            var size = entity.Size.Y < 0 ? entity.Size with { Y = -entity.Size.Y * this.blockSize } : entity.Size;

            // How far its top is above the baseline.
            var top = entity.Alignment switch
            {
                TextEntityAlignment.TextTop => this.metrics.Ascent,
                TextEntityAlignment.Baseline => size.Y,
                TextEntityAlignment.TextBottom => size.Y - this.metrics.Descent,
                _ => this.CenteredTop(size.Y),
            };
            this.AddObject(new(new(0, -top, size.X, size.Y - top)), link, new(entity, byteOffset, font, size));
            byteLength = entity.Length;
            return true;
        }

        return false;
    }

    private void AddRune(Rune rune, int link)
    {
        switch (rune.Value)
        {
            case 0 or char.MaxValue:
                return;

            // U+FFFC is reserved as the inline-object sentinel in StyledText.Text.
            case ObjectReplacementCharacter:
                rune = Rune.ReplacementChar;
                break;
        }

        this.StartRun(link);
        Span<byte> buf = stackalloc byte[4];
        this.output.Append(buf[..rune.EncodeToUtf8(buf)], this.sourceOffset);
    }

    private void AddObject(InlineObject obj, int link, TextObjectContent content)
    {
        this.StartRun(link);
        this.output.Objects.Add(obj with { UserData = (ulong)(link + 1) });
        this.output.Contents.Add(content);
        this.output.Append("\uFFFC"u8, this.sourceOffset);
    }

    private void StartRun(int link)
    {
        var userData = (ulong)(link + 1);
        if (this.hasRun && userData == this.currentUserData && !this.styleChanged)
            return;
        var style = link >= 0 ? this.LinkStyleOf(link, this.StyleNow()) : this.StyleNow();
        this.styleChanged = false;
        if (this.hasRun && userData == this.currentUserData && style == this.currentStyle)
            return;
        this.EndRun();
        this.hasRun = true;
        this.currentStyle = style;
        this.currentUserData = userData;
        this.currentRunBegin = this.output.Text.Length;
    }

    private TextStyle LinkStyleOf(int link, TextStyle style)
    {
        if (this.callbacks.Length == 0)
            return style;
        if (this.lastLinkStyle.Link == link && this.lastLinkStyle.Style == style)
            return this.lastLinkStyle.LinkStyle;

        var linkStyle = this.output.LinkStyleOf(this.markup, this.callbacks, link, style);
        this.lastLinkStyle = (link, style, linkStyle);
        return linkStyle;
    }

    // Returns the detected link index at byte offset at within [next, end), advancing next monotonically.
    private int FoundLinkAt(int at, ref int next, int end)
    {
        var links = this.output.Links;
        while (next < end && links[next].Offset + links[next].Length <= at)
            next++;
        return next < end && links[next].Offset <= at ? next : -1;
    }

    private void EndRun()
    {
        var end = this.output.Text.Length;
        if (!this.hasRun || this.currentRunBegin == end)
            return;

        // Text outside runs gets the default font.
        if (this.currentUserData != 0 || this.currentStyle != this.defaultStyle)
            this.output.Runs.Add(new(this.currentRunBegin, end, this.FontOf(this.currentStyle), this.currentUserData));
        this.currentRunBegin = end;
    }
}
