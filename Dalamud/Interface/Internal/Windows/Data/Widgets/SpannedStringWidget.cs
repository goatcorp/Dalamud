using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.SpannedStrings;
using Dalamud.Interface.SpannedStrings.Enums;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying <see cref="SpannedString"/> test.
/// </summary>
internal class SpannedStringWidget : IDataWindowWidget, IDisposable
{
    private readonly Stopwatch stopwatch = new();
    private readonly IFontHandle?[] spannableTestFontHandle = new IFontHandle?[37];

    private SpannedString? prebuiltSpannable;
    private IFontAtlas? spannableTestAtlas;

    private ImVectorWrapper<byte> testStringBuffer;
    private int numLinkClicks;
    private VerticalAlignment valign;
    private float vertOffset;
    private bool useItalic;
    private bool usePrebuiltSpannable;
    private bool useWrapMarkers;
    private bool useVisibleControlCharacters;
    private bool showComplicatedTextTest;
    private bool showDynamicOffsetTest;
    private bool showParseTest;
    private WordBreakType wordBreakType;
    private long catchMeBegin;

    private (SpannedString? Parsed, Exception? Exception) parseAttempt;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "spannedstring" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Spanned Strings";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.numLinkClicks = 0;
        this.valign = VerticalAlignment.Baseline;
        this.vertOffset = 0;
        this.useItalic = false;
        this.prebuiltSpannable = null;
        this.usePrebuiltSpannable = this.useWrapMarkers = this.useVisibleControlCharacters = false;
        this.showComplicatedTextTest = this.showDynamicOffsetTest = false;
        this.parseAttempt = default;

        this.testStringBuffer.Dispose();
        this.testStringBuffer = new(65536);
        this.testStringBuffer.StorageSpan.Clear();
        this.testStringBuffer.Clear();
        this.testStringBuffer.AddRange(
            """
            {bw 1}{ec rgba(128 0 255 / 50%)}a{bw 2}s{bw 3}d{bw 4}f{/bw}{/bw}{/bw}{/bw}
            {size 48}{icon dpadleft}{/size}{size 48}{icon dpadright}{/size}
            {va top}top {va bottom}bottom {va baseline}{link "a"}baseline{/link} {size 32}32{/size}{/va}{/va}{/va}{br}
            {va middle}test {vo 0.3} {b}bold{/b} {vo -0.3}{i}italic{/i} {/vo}{/vo} {i}{b}italic {size 48}bold{/i}{/b}
            """u8);

        foreach (ref var e in this.spannableTestFontHandle.AsSpan())
        {
            e?.Dispose();
            e = null;
        }

        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (ref var e in this.spannableTestFontHandle.AsSpan())
        {
            e?.Dispose();
            e = null;
        }

        this.spannableTestAtlas?.Dispose();
        this.spannableTestAtlas = null;
        this.testStringBuffer.Dispose();
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        var interfaceManager = Service<InterfaceManager>.Get();
        var p = new ISpannedStringRenderer.Options
        {
            WordBreak = this.wordBreakType,
            WrapMarker = this.useWrapMarkers ? FontAwesomeIcon.ArrowTurnDown.ToIconString() : string.Empty,
            ControlCharactersSpanParams =
                this.useVisibleControlCharacters
                    ? new()
                    {
                        Font = new(interfaceManager.MonoFontHandle),
                        BackColorU32 = 0xFF004400,
                        ForeColorU32 = 0xFFCCFFCC,
                        FontSize = ImGui.GetFont().FontSize * 0.6f,
                        VerticalAlignment = VerticalAlignment.Middle,
                    }
                    : null,
            WrapMarkerStyle = new()
            {
                Font = new(interfaceManager.IconFontHandle),
                EdgeColorU32 = 0xFF000044,
                BorderWidth = 1,
                ForeColorU32 = 0xFFCCCCFF,
                Italic = true,
                FontSize = ImGui.GetFont().FontSize * 0.6f,
                VerticalAlignment = VerticalAlignment.Middle,
            },
        };

        var bgpos = ImGui.GetWindowPos() + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
        ImGui.GetWindowDrawList()
             .AddImage(
                 Service<TextureManager>.Get().GetTextureFromGame("ui/uld/WindowA_BgSelected_HV_hr1.tex")!.ImGuiHandle,
                 bgpos + ImGui.GetWindowContentRegionMin(),
                 bgpos + ImGui.GetWindowContentRegionMax(),
                 Vector2.Zero,
                 (ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()) / 64);
        ImGui.Indent(8 * ImGuiHelpers.GlobalScale);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (8 * ImGuiHelpers.GlobalScale));
        var dynamicOffsetTestOffset = ImGui.GetCursorScreenPos();
        using (var renderer = Service<SpannableFactory>.Get().Rent("##config", p))
        {
            renderer.PushLink("copy"u8)
                    .PushEdgeColor(ImGuiColors.TankBlue)
                    .PushBorderWidth(1)
                    .Append("Copy ToString")
                    .PopBorderWidth()
                    .PopEdgeColor()
                    .PopLink()
                    .AppendLine()
                    .AppendLine();

            renderer.PushForeColor(0xFFC5E1EE)
                    .PushShadowColor(0xFF000000)
                    .PushShadowOffset(new(0, 1));

            renderer.PushForeColor(0xFFCCCCCC)
                    .Append("Options")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f);

            renderer.PushLink("usePrebuiltSpannable"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        Service<TextureManager>.Get().GetTextureFromGame("ui/uld/CheckBoxA_hr1.tex"),
                        this.usePrebuiltSpannable ? new(0.5f, 0) : Vector2.Zero,
                        this.usePrebuiltSpannable ? Vector2.One : new(0.5f, 1),
                        out var texIdCheckbox)
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Use Prebuilt Spannable")
                    .PopLink();

            renderer.PushLink("useWrapMarkers"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.useWrapMarkers ? new(0.5f, 0) : Vector2.Zero,
                        this.useWrapMarkers ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Use Wrap Markers")
                    .PopLink();

            renderer.PushLink("useVisibleControlCharacters"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.useVisibleControlCharacters ? new(0.5f, 0) : Vector2.Zero,
                        this.useVisibleControlCharacters ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Use Visible Control Characters")
                    .PopLink();

            renderer.AppendLine();

            renderer.PopHorizontalOffset()
                    .PushForeColor(0xFFCCCCCC)
                    .Append("Word Break Type")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f);

            renderer.PushLink("wordBreakTypeNormal"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        Service<TextureManager>.Get().GetTextureFromGame("ui/uld/RadioButtonA_hr1.tex"),
                        this.wordBreakType == WordBreakType.Normal ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.Normal ? Vector2.One : new(0.5f, 1),
                        out var texIdRadio)
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Normal")
                    .PopLink();

            renderer.PushLink("wordBreakTypeBreakAll"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.BreakAll ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.BreakAll ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Break All")
                    .PopLink();

            renderer.PushLink("wordBreakTypeKeepAll"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.KeepAll ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.KeepAll ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Keep All")
                    .PopLink();

            renderer.PushLink("wordBreakTypeBreakWord"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.BreakWord ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.BreakWord ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Break Word")
                    .PopLink();

            renderer.AppendLine();

            renderer.PopHorizontalOffset()
                    .PushForeColor(0xFFCCCCCC)
                    .Append("Tests")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f);

            renderer.PushLink("showComplicatedTextTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showComplicatedTextTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showComplicatedTextTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Complicated Text")
                    .PopLink();

            renderer.PushLink("showDynamicOffsetTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showDynamicOffsetTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showDynamicOffsetTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Dynamic Horizontal and Vertical Offsets")
                    .PopLink();

            renderer.PushLink("showParseTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showParseTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showParseTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Parsing")
                    .PopLink();

            if (renderer.Render(out var state, out var link) && state.ClickedMouseButton == ImGuiMouseButton.Left)
            {
                if (link.SequenceEqual("copy"u8))
                    this.CopyMe(renderer.Build().ToString());
                else if (link.SequenceEqual("usePrebuiltSpannable"u8))
                    this.usePrebuiltSpannable ^= true;
                else if (link.SequenceEqual("useWrapMarkers"u8))
                    this.useWrapMarkers ^= true;
                else if (link.SequenceEqual("useVisibleControlCharacters"u8))
                    this.useVisibleControlCharacters ^= true;
                else if (link.SequenceEqual("showComplicatedTextTest"u8))
                    this.showComplicatedTextTest ^= true;
                else if (link.SequenceEqual("showDynamicOffsetTest"u8))
                    this.showDynamicOffsetTest ^= true;
                else if (link.SequenceEqual("showParseTest"u8))
                    this.showParseTest ^= true;
                else if (link.SequenceEqual("wordBreakTypeNormal"u8))
                    this.wordBreakType = WordBreakType.Normal;
                else if (link.SequenceEqual("wordBreakTypeBreakAll"u8))
                    this.wordBreakType = WordBreakType.BreakAll;
                else if (link.SequenceEqual("wordBreakTypeKeepAll"u8))
                    this.wordBreakType = WordBreakType.KeepAll;
                else if (link.SequenceEqual("wordBreakTypeBreakWord"u8))
                    this.wordBreakType = WordBreakType.BreakWord;
            }
        }

        ImGuiHelpers.ScaledDummy(8);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8);

        this.stopwatch.Restart();
        if (this.showComplicatedTextTest)
        {
            if (this.usePrebuiltSpannable && this.prebuiltSpannable is null)
            {
                var t = this.AddTextTo(new SpannedStringBuilder()).Build();
                var test = t.ToString(CultureInfo.InvariantCulture);
                if (SpannedString.TryParse(test, CultureInfo.InvariantCulture, out var t2))
                {
                    var buf = new byte[t2.Encode(default)];
                    t2.Encode(buf);
                    if (SpannedString.TryDecode(buf, out this.prebuiltSpannable))
                    {
                        for (var i = 0; i < t.Textures.Count; i++)
                            this.prebuiltSpannable.Textures[i] = t.Textures[i];
                        for (var i = 0; i < t.FontHandleSets.Count; i++)
                            this.prebuiltSpannable.FontHandleSets[i] = t.FontHandleSets[i];
                        for (var i = 0; i < t.Callbacks.Count; i++)
                            this.prebuiltSpannable.Callbacks[i] = t.Callbacks[i];
                    }
                }
            }

            using var renderer = Service<SpannableFactory>.Get().Rent("##test", p);
            var result =
                this.usePrebuiltSpannable && this.prebuiltSpannable is not null
                    ? renderer.Render(this.prebuiltSpannable, out var state, out var hovered)
                    : this.AddTextTo(renderer).Render(out state, out hovered);

            if (result)
            {
                if (state.ClickedMouseButton == ImGuiMouseButton.Left)
                {
                    if (hovered.SequenceEqual("copy"u8))
                    {
                        if (this.usePrebuiltSpannable && this.prebuiltSpannable is not null)
                            this.CopyMe(this.prebuiltSpannable.ToString());
                        else
                            this.CopyMe(renderer.Build().ToString());
                    }
                    else if (hovered.SequenceEqual("Link 1"u8))
                    {
                        this.prebuiltSpannable = null;
                        this.numLinkClicks++;
                    }
                    else if (hovered.SequenceEqual("valign_up"u8))
                    {
                        this.prebuiltSpannable = null;
                        this.vertOffset -= 1 / 8f;
                    }
                    else if (hovered.SequenceEqual("valign_next"u8))
                    {
                        this.prebuiltSpannable = null;
                        this.valign =
                            (VerticalAlignment)(((int)this.valign + 1) %
                                                Enum.GetValues<VerticalAlignment>().Length);
                    }
                    else if (hovered.SequenceEqual("valign_down"u8))
                    {
                        this.prebuiltSpannable = null;
                        this.vertOffset += 1 / 8f;
                    }
                    else if (hovered.SequenceEqual("italic_toggle"u8))
                    {
                        this.prebuiltSpannable = null;
                        this.useItalic ^= true;
                    }
                }

                if (!hovered.SequenceEqual("a"u8))
                {
                    fixed (byte* p2 = hovered)
                        ImGuiNative.igSetTooltip(p2);
                }
            }
        }

        if (this.showParseTest)
        {
            fixed (byte* labelPtr = "##Test"u8)
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (ImGuiNative.igInputTextMultiline(
                        labelPtr,
                        this.testStringBuffer.Data,
                        (uint)this.testStringBuffer.Capacity,
                        new(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 3),
                        0,
                        null,
                        null) != 0)
                {
                    var len = this.testStringBuffer.StorageSpan.IndexOf((byte)0);
                    if (len + 4 >= this.testStringBuffer.Capacity)
                        this.testStringBuffer.EnsureCapacityExponential(len + 4);
                    if (len < this.testStringBuffer.Capacity)
                    {
                        this.testStringBuffer.LengthUnsafe = len;
                        this.testStringBuffer.StorageSpan[len] = default;
                    }

                    if (SpannedString.TryParse(
                            Encoding.UTF8.GetString(this.testStringBuffer.DataSpan),
                            CultureInfo.InvariantCulture,
                            out var r,
                            out var e))
                        this.parseAttempt = (r, null);
                    else
                        this.parseAttempt = (null, e);
                }
            }

            using var renderer = Service<SpannableFactory>.Get().Rent("##parsedPreview", p);
            if (this.parseAttempt.Parsed is { } parsed)
            {
                renderer.Render(parsed, out _, out _);
            }
            else if (this.parseAttempt.Exception is { } e)
            {
                renderer.PushEdgeColor(new Rgba32(ImGuiColors.DalamudRed).MultiplyOpacity(0.5f))
                        .Append(e.ToString())
                        .Render();
            }
        }

        if (this.showDynamicOffsetTest)
        {
            var prevPos = ImGui.GetCursorScreenPos();
            ImGui.SetCursorScreenPos(dynamicOffsetTestOffset);
            using var renderer = Service<SpannableFactory>.Get().Rent("##catchme", p);
            const float interval = 2000;
            var v = ((this.catchMeBegin + Environment.TickCount64) / interval) % (2 * MathF.PI);
            renderer.PushHorizontalAlignment(HorizontalAlignment.Center)
                    .PushHorizontalOffset(MathF.Sin(v) * 8)
                    .PushVerticalOffset((1 + MathF.Cos(v)) * 8)
                    .PushBorderWidth(1)
                    .PushEdgeColor(new(new Vector4(0.3f, 0.3f, 1f, 0.5f + (MathF.Sin(v) * 0.5f))))
                    .PushLink("a"u8)
                    .Append("Text\ngoing\nround");
            if (renderer.Render(out _, out _))
                this.catchMeBegin += 50;
            ImGui.SetCursorScreenPos(prevPos);
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.TextUnformatted($"Took {this.stopwatch.Elapsed.TotalMilliseconds:g}ms");
    }

    private static void CustomDrawCallback(in SpannedStringCallbackArgs args)
    {
        var hover = ImGui.IsMouseHoveringRect(
            args.RenderState.StartScreenOffset + args.Xy0,
            args.RenderState.StartScreenOffset + args.Xy1);
        args.SwitchToForegroundChannel();
        args.DrawListPtr.PushClipRect(
            args.RenderState.StartScreenOffset + args.Xy0,
            args.RenderState.StartScreenOffset + args.Xy1);
        args.DrawListPtr.AddText(
            args.FontPtr,
            args.FontSize,
            args.RenderState.StartScreenOffset + args.Xy0,
            (Rgba32)(hover ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed),
            $"@line {args.RenderState.LastLineIndex}");
        args.DrawListPtr.PopClipRect();
    }

    private void CopyMe(string what)
    {
        ImGui.SetClipboardText(what);
        Service<NotificationManager>.Get().AddNotification(
            $"Copied parseable representation. (Length: {what.Length})",
            this.DisplayName,
            NotificationType.Info);
    }

    private T AddTextTo<T>(T target) where T : ISpannedStringBuilder<T>
    {
        var interfaceManager = Service<InterfaceManager>.Get();

        var atlas = this.spannableTestAtlas ??=
                        Service<FontAtlasFactory>.Get().CreateFontAtlas(
                            nameof(ImGuiWidget),
                            FontAtlasAutoRebuildMode.Async);

        target.PushLink("copy"u8)
              .PushEdgeColor(ImGuiColors.HealerGreen)
              .PushBorderWidth(1)
              .Append("Copy ToString")
              .PopBorderWidth()
              .PopEdgeColor()
              .PopLink()
              .AppendLine()
              .AppendLine();

        target.PushLineHeight(2)
              .AppendCallback(CustomDrawCallback, 4, out _)
              .PopLineHeight()
              .AppendLine()
              .AppendLine();

        var fontSizeCounter = 9;
        target.PushLink("valign_next"u8)
              .PushItalic(this.useItalic)
              .PushVerticalAlignment(this.valign)
              .PushVerticalOffset(this.vertOffset);
        foreach (var c in $"Vertical Align: {this.valign}")
        {
            ref var fh = ref this.spannableTestFontHandle[fontSizeCounter];
            var fontSizeCounterPx = (fontSizeCounter * 4f) / 3;
            fh ??= atlas.NewDelegateFontHandle(
                e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(fontSizeCounterPx)));
            target.PushFontSet(new(fh), out _)
                  .PushFontSize(fontSizeCounterPx)
                  .PushBackColor(0xFF111111)
                  .Append(c)
                  .PopBackColor()
                  .PopFontSize()
                  .PopFontSet();
            fontSizeCounter++;
        }

        target.PopLink()
              .PopVerticalAlignment()
              .PopVerticalOffset()
              .Append(' ')
              .PushBackColor(0xFF000044)
              .PushFontSize(18)
              .PushVerticalAlignment(VerticalAlignment.Middle)
              .PushLink("valign_up"u8)
              .AppendIconGfd(GfdIcon.RelativeLocationUp)
              .PopLink()
              .PushLink("valign_down"u8)
              .AppendIconGfd(GfdIcon.RelativeLocationDown)
              .PopLink()
              .PushLink("italic_toggle"u8)
              .PushFontSet(new(interfaceManager.IconFontHandle), out _)
              .PushFontSize(18)
              .PushVerticalAlignment(VerticalAlignment.Middle)
              .Append(FontAwesomeIcon.Italic.ToIconChar())
              .PopVerticalAlignment()
              .PopFontSize()
              .PopFontSet()
              .PopFontSize()
              .PopVerticalAlignment()
              .PopBackColor()
              .PopLink()
              .AppendLine()
              .PopItalic()
              .AppendLine();

        target.PushBackColor(0xFF313131)
              .PushForeColor(0xFFC5E1EE)
              .PushShadowColor(0xFF000000)
              .PushShadowOffset(new(0, 1))
              .Append("Soft Hyphen test:"u8);
        for (var c = 'a'; c <= 'z'; c++)
        {
            for (var i = 0; i < 10; i++)
            {
                target.Append(i == 0 ? ' ' : '\u00AD')
                      .PushLink("a"u8)
                      .PushItalic(i % 2 == 0)
                      .Append(c, 5)
                      .PopItalic()
                      .PopLink();
            }
        }

        target.PopShadowOffset()
              .PopShadowColor()
              .AppendLine()
              .PopForeColor()
              .PopBackColor()
              .AppendLine();

        target.PushLink("Link 1"u8)
              .PushItalic(this.useItalic)
              .PushHorizontalAlignment(HorizontalAlignment.Center)
              .Append("This link is clicked "u8)
              .PushBold(true)
              .Append(this.numLinkClicks)
              .PopBold()
              .Append(" times."u8)
              .PopHorizontalAlignment()
              .PopItalic()
              .PopLink()
              .AppendLine()
              .AppendLine();

        target.PushLink("Link 2"u8)
              .PushItalic(this.useItalic)
              .PushForeColor(0xFF00CC00)
              .PushEdgeColor(0xFF005500)
              .PushBorderWidth(1)
              .PushHorizontalAlignment(HorizontalAlignment.Right)
              .Append("Another "u8)
              .PushFontSet(new(interfaceManager.MonoFontHandle), out _)
              .PushForeColor(0xFF9999FF)
              .PushVerticalOffset(-0.4f).Append('M').PopVerticalOffset()
              .PushVerticalOffset(-0.3f).Append('o').PopVerticalOffset()
              .PushVerticalOffset(-0.2f).Append('n').PopVerticalOffset()
              .PushVerticalOffset(-0.1f).Append('o').PopVerticalOffset()
              .PushVerticalOffset(+0.0f).Append('s').PopVerticalOffset()
              .PushVerticalOffset(+0.1f).Append('p').PopVerticalOffset()
              .PushVerticalOffset(+0.2f).Append('a').PopVerticalOffset()
              .PushVerticalOffset(+0.3f).Append('c').PopVerticalOffset()
              .PushVerticalOffset(+0.4f).Append('e').PopVerticalOffset()
              .PopForeColor()
              .PopFontSet()
              .Append(" Link.")
              .PopHorizontalAlignment()
              .PopItalic()
              .PopBorderWidth()
              .PopEdgeColor()
              .PopForeColor()
              .PopLink()
              .AppendLine()
              .AppendLine();

        for (var i = 0; i < 30; i++)
        {
            var tex = Service<TextureManager>.Get().GetTextureFromGame($"ui/icon/000000/{i:000000}.tex");
            target.AppendTexture(tex, out _)
                  .Append("UI#")
                  .PushForeColor(0xFF9999FF)
                  .Append(i)
                  .PopForeColor()
                  .Append(' ');
        }

        target.AppendLine()
              .AppendLine();

        foreach (var e in Enum.GetValues<GfdIcon>())
        {
            target.AppendIconGfd(e)
                  .Append('#')
                  .PushForeColor(0xFFFF9999)
                  .Append((int)e)
                  .PopForeColor()
                  .Append('\u00A0')
                  .Append(Enum.GetName(e))
                  .Append(' ');
        }

        return target;
    }
}
