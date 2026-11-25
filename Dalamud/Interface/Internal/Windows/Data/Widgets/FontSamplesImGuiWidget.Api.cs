using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <content>API sample rows for <see cref="FontSamplesImGuiWidget"/>.</content>
internal partial class FontSamplesImGuiWidget
{
    private const float BasisSizePx = 18;
    private const int RangeStart = 7;
    private const int RangeLength = 20;

    private static readonly byte[] RangeText = "Plain, styled office 直骨, plain"u8.ToArray();
    private static readonly byte[] HanText = "直骨角遍今令户 Ag"u8.ToArray();

    private static readonly SystemFontFamilyId SegoeUiFamily = new("Segoe UI");
    private static readonly SystemFontFamilyId ComicSansFamily = new("Comic Sans MS");

    private static readonly TextStyle GameAxis = TextStyle.From(GameFontFamily.Axis);
    private static readonly TextStyle AxisBasicProN = TextStyle.From("AXIS Basic ProN");
    private static readonly TextStyle SegoeUi = TextStyle.From(SegoeUiFamily);
    private static readonly TextStyle Bahnschrift = TextStyle.From("Bahnschrift");
    private static readonly TextStyle Calibri = TextStyle.From("Calibri");
    private static readonly TextStyle NotoCjk = TextStyle.From(DalamudAsset.NotoSansCjkRegular);
    private static readonly TextStyle MonoItalic = TextStyle.Mono with { Italic = true };

    private static readonly (byte[] Label, FontSizeBasis Basis)[] SizeBases =
    [
        ("Auto"u8.ToArray(), FontSizeBasis.Auto),
        ("LineBox"u8.ToArray(), FontSizeBasis.LineBox),
        ("EmBox"u8.ToArray(), FontSizeBasis.EmBox),
    ];

    private static readonly (byte[] Label, TextStyle Style)[] GameFontSizes =
    [
        .. Enum.GetValues<GameFontFamilyAndSize>()
               .Where(x => x != GameFontFamilyAndSize.Undefined)
               .Select(x => (Encoding.UTF8.GetBytes(x.ToString()), TextStyle.From(x))),
    ];

    private static readonly float[] AxisPoints = [9.6f, 10, 10.5f, 11, 12, 13, 14];
    private static readonly int[] Weights = [100, 200, 300, 350, 400, 500, 600, 700, 800, 900, 950, 1000];

    private static readonly ImmutableArray<FontFeature> SmallCaps = [new("smcp")];
    private static readonly ImmutableArray<FontFeature> NoLigatures = [new("liga", 0)];

    private static readonly SystemFontFamilyId MalgunFamily = new("Malgun Gothic");
    private static readonly TextStyle SegoeMalgun = ChainOf(SegoeUiFamily, MalgunFamily);
    private static readonly TextStyle SegoeMalgunScaled =
        ChainOf(SegoeUiFamily, Wrap(MalgunFamily, new() { SizeScale = 0.8f }));

    private static readonly TextStyle OffsetEm = ChainOf(
        Wrap(SegoeUiFamily, new() { CodepointRanges = ['0', '9'], GlyphOffset = new(0, -0.3f) }),
        SegoeUiFamily);

    private static readonly TextStyle OffsetPx = ChainOf(
        Wrap(
            SegoeUiFamily,
            new() { CodepointRanges = ['0', '9'], GlyphOffset = new(0, -4), GlyphOffsetUnit = StyleUnit.Px }),
        SegoeUiFamily);

    private static readonly TextStyle DigitSpacing = ChainOf(
        Wrap(SegoeUiFamily, new() { CodepointRanges = ['0', '9'], LetterSpacing = StyleLength.Em(0.3f) }),
        SegoeUiFamily);

    private static readonly TextStyle FixedAdvance =
        ChainOf(Wrap(SegoeUiFamily, FontAdjustments.FixedAdvance(StyleLength.Em(1))));

    private static readonly TextStyle MinAdvance =
        ChainOf(Wrap(SegoeUiFamily, new() { GlyphMinAdvanceX = StyleLength.Em(0.7f) }));

    private static readonly TextStyle MaxAdvance =
        ChainOf(Wrap(SegoeUiFamily, new() { GlyphMaxAdvanceX = StyleLength.Em(0.4f) }));

    private static readonly TextStyle FitToSquare =
        ChainOf(Wrap(SegoeUiFamily, FontAdjustments.FixedAdvance(StyleLength.Em(1)) with { FitToSquare = true }));

    private static readonly TextStyle CalibriSmallCaps =
        ChainOf(Wrap(new SystemFontFamilyId("Calibri"), new() { Features = SmallCaps }));

    private static readonly ReadOnlySeString SeStringSample =
        ReadOnlySeString.FromMacroString("PushTextMarkup(SeString): <italic(1)>italic<italic(0)> <colortype(500)>color<colortype(0)>");

    private static readonly string[] ParsedLengths =
        [.. new[] { "1.5em", "12px", "12", " 2.5EM ", "default", "-1px", "bad" }.Select(DescribeParse)];

    private ImFontRef? monoRef;
    private ImFontPtr keptFont;
    private int keptFrame;
    private IFontFaceId? comicFace;
    private string urlInput = "https://example.com/ in InputText";
    private ITextMarkupCallback? urlsWithHandler;
    private string clickedUrl = "Not clicked yet.";
    private bool openClickedUrl;

    private static string DescribeParse(string s)
    {
        if (!StyleLength.TryParse(s, null, out var parsed))
            return $"\"{s}\": not parsed";
        var again = StyleLength.TryParse(parsed.ToString(), null, out var reparsed) ? reparsed.ToString() : "not parsed";
        return $"\"{s}\": {parsed} ({parsed.Unit}), parsed again: {again}";
    }

    private static IFontFamilyId Wrap(IFontFamilyId family, FontAdjustments adjustments) =>
        FontRegistry.Wrap(adjustments, family);

    private static TextStyle ChainOf(params IFontFamilyId[] families) => TextStyle.From(ChainFamilyId.Of(families));

    private static void Label(ImU8String label)
    {
        ImGui.Text(label);
        ImGui.SameLine(ImGui.GetFontSize() * 10);
    }

    // Queries link interaction on the previous item, sets the hand cursor on hover, and stays on the same line.
    private static bool TryBeginLinkInteraction(out TextInteraction interaction)
    {
        if (!ImGui.TryGetTextInteraction(out interaction, TextInteractionFlags.All))
            return false;
        if ((interaction.Flags & TextInteractionFlags.Hover) != 0)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        ImGui.SameLine();
        return true;
    }

    private static void Row(ImU8String label, ReadOnlySpan<byte> text, params ReadOnlySpan<TextStyle> styles)
    {
        Label(label);
        for (var i = 0; i < styles.Length; i++)
        {
            if (i != 0)
                ImGui.SameLine();
            SampleText(styles[i], text);
        }
    }

    private static void BasisButton(ReadOnlySpan<byte> label, in TextStyle style, FontSizeBasis basis)
    {
        using ((style with { Size = StyleLength.Px(BasisSizePx), SizeBasis = basis }).Push())
            ImGui.Button(label);
    }

    private static void LineBox(ReadOnlySpan<byte> label, in TextStyle outer, TextStyle? inner = null)
    {
        Label(label);
        float lineHeight;
        using (outer.Push())
        using (inner?.Push())
        {
            ImGui.Text("Ag気 line 1\nAg気 line 2"u8);
            lineHeight = ImGui.GetTextLineHeight();
        }

        ImGui.GetWindowDrawList().AddRect(
            ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.PlotHistogram));
        ImGui.SameLine();
        ImGui.Text($"{lineHeight:0.##}px per line");
    }

    private static void RangeSample(ReadOnlySpan<byte> label, in TextStyleRange range, TextStyle? within = null)
    {
        Label(label);
        using (within?.PushFamily())
            ImGui.TextStyledRanges(RangeText, [range]);
    }

    private static void DrawSizeBasis()
    {
        ImGui.Text($"Each button is pushed at {BasisSizePx}px. LineBox makes the lines, and so the buttons, equal.");
        foreach (var (label, basis) in SizeBases)
        {
            using var id = ImRaii.PushId((int)basis);
            Label(label);
            BasisButton("AXIS (game) Ag気"u8, GameAxis, basis);
            ImGui.SameLine();
            BasisButton("AXIS Basic ProN Ag気"u8, AxisBasicProN, basis);
            ImGui.SameLine();
            BasisButton("Inconsolata Ag"u8, TextStyle.Mono, basis);
            ImGui.SameLine();
            BasisButton("Segoe UI Ag"u8, SegoeUi, basis);
        }
    }

    private static void DrawGameFontSizes()
    {
        foreach (var (label, style) in GameFontSizes)
            Row(label, "0123456789 The quick brown fox 気"u8, style);

        ImGui.Separator();
        ImGui.Text("AXIS between its native sizes. The widths grow with the size."u8);
        var text = "The quick brown fox"u8;
        foreach (var pt in AxisPoints)
        {
            var style = GameAxis with { Size = StyleLength.Px(FontSizes.PtToPx(pt)) };
            Label($"AXIS {pt}pt");
            SampleText(style, text);
            ImGui.SameLine();
            ImGui.Text($"{style.CalcTextSize(text).X:0.##}px wide");
        }
    }

    private static void DrawLineHeight()
    {
        var size = StyleLength.Px(18);
        LineBox("Default, natural"u8, TextStyle.Default with { Size = size });
        LineBox("Mono, natural"u8, TextStyle.Mono with { Size = size });
        LineBox("AXIS, natural"u8, GameAxis with { Size = size });
        LineBox("AXIS ProN, natural"u8, AxisBasicProN with { Size = size });
        LineBox("1 em"u8, TextStyle.Default with { Size = size, LineHeight = StyleLength.Em(1) });
        LineBox("1.5 em"u8, TextStyle.Default with { Size = size, LineHeight = StyleLength.Em(1.5f) });
        LineBox("30 px"u8, TextStyle.Default with { Size = size, LineHeight = StyleLength.Px(30) });

        ImGui.Separator();
        ImGui.Text("Pushed over a font with a line height, at another size or family:"u8);
        var em2 = TextStyle.Default with { Size = StyleLength.Px(16), LineHeight = StyleLength.Em(2) };
        var px32 = TextStyle.Default with { Size = StyleLength.Px(16), LineHeight = StyleLength.Px(32) };
        LineBox("2 em, then 24px"u8, em2, TextStyle.Default with { Size = StyleLength.Px(24) });
        LineBox("32 px, then 24px"u8, px32, TextStyle.Default with { Size = StyleLength.Px(24) });
        LineBox("2 em, then Mono"u8, em2, TextStyle.Mono);
        LineBox("32 px, then Mono"u8, px32, TextStyle.Mono);
    }

    private static void DrawSpacing()
    {
        var text = "Letter spacing 気기"u8;
        Row("0.2 em"u8, text, TextStyle.Default with { LetterSpacing = StyleLength.Em(0.2f) });
        Row("4 px"u8, text, TextStyle.Default with { LetterSpacing = StyleLength.Px(4) });
        Row("-0.05 em"u8, text, TextStyle.Default with { LetterSpacing = StyleLength.Em(-0.05f) });

        ImGui.Separator();
        ImGui.Text("At 16px and 32px. Em lengths grow with the size, and px lengths stay."u8);
        var small = TextStyle.Default with { Size = StyleLength.Px(16) };
        var large = TextStyle.Default with { Size = StyleLength.Px(32) };
        text = "Spacing"u8;
        var spacing = StyleLength.Em(0.2f);
        Row("Spacing 0.2 em"u8, text, small with { LetterSpacing = spacing }, large with { LetterSpacing = spacing });
        spacing = StyleLength.Px(4);
        Row("Spacing 4 px"u8, text, small with { LetterSpacing = spacing }, large with { LetterSpacing = spacing });

        text = "Edge"u8;
        var edge = new TextEdge(0xFF0000C0, StyleLength.Em(0.1f));
        Row("Edge 0.1 em"u8, text, small with { Edge = edge }, large with { Edge = edge });
        edge = new(0xFF0000C0, StyleLength.Px(2));
        Row("Edge 2 px"u8, text, small with { Edge = edge }, large with { Edge = edge });

        text = "Shadow"u8;
        var shadow = new TextShadow(0xFF0080FF, new(0.1f, 0.1f));
        Row("Shadow 0.1 em"u8, text, small with { Shadow = shadow }, large with { Shadow = shadow });
        shadow = new(0xFF0080FF, new(2, 2), StyleUnit.Px);
        Row("Shadow 2 px"u8, text, small with { Shadow = shadow }, large with { Shadow = shadow });
    }

    private static void DrawFaces()
    {
        foreach (var weight in Weights)
        {
            Label($"Weight {weight}");
            SampleText(SegoeUi with { Weight = weight }, "Segoe UI"u8);
            ImGui.SameLine();
            SampleText(Bahnschrift with { Weight = weight }, "Bahnschrift"u8);
            ImGui.SameLine();
            SampleText(NotoCjk with { Weight = weight }, "Noto Sans CJK 気"u8);
        }

        ImGui.Separator();
        for (var stretch = 1; stretch <= 9; stretch++)
        {
            Label($"Stretch {stretch}");
            SampleText(Bahnschrift with { Stretch = stretch }, "Bahnschrift"u8);
            ImGui.SameLine();
            SampleText(SegoeUi with { Stretch = stretch }, "Segoe UI"u8);
        }

        ImGui.Separator();
        var text = "Bold over a face"u8;
        Row("Light"u8, text, SegoeUi with { Weight = 300 });
        Row("Light, bold"u8, text, SegoeUi with { Weight = 300, Bold = true });
        Row("Regular, bold"u8, text, SegoeUi with { Weight = 400, Bold = true });
        Row("SemiBold, bold"u8, text, SegoeUi with { Weight = 600, Bold = true });
        Row("Light face, bold"u8, text, TextStyle.From("Segoe UI", FontStyle.Light) with { Bold = true });

        ImGui.Separator();
        text = "Italic 気기"u8;
        Row("Real italic"u8, text, SegoeUi with { Italic = true }, Calibri with { Italic = true });
        Row("Faux italic"u8, text, MonoItalic, AxisBasicProN with { Italic = true }, GameAxis with { Italic = true });
    }

    private static void DrawLocaleAndFeatures()
    {
        foreach (var locale in FontChooserDialog.Locales)
            Row(locale, HanText, NotoCjk with { Locale = locale }, TextStyle.Default with { Locale = locale });

        Row("No locale"u8, HanText, NotoCjk, TextStyle.Default);

        ImGui.Separator();
        Row("Calibri liga"u8, "office fifty flow"u8, Calibri, Calibri with { Features = NoLigatures });
        Row("Calibri smcp"u8, "Small Caps"u8, Calibri, Calibri with { Features = SmallCaps });
        Row("Calibri onum"u8, "0123456789"u8, Calibri, Calibri with { Features = [new("onum")] });
        Row(
            "Calibri tnum/pnum"u8,
            "1111.11 8888.88"u8,
            Calibri with { Features = [new("tnum")] },
            Calibri with { Features = [new("pnum")] });
        Row("Calibri frac"u8, "1/2 3/4"u8, Calibri, Calibri with { Features = [new("frac")] });
        Row("Mono zero"u8, "0O0o"u8, TextStyle.Mono, TextStyle.Mono with { Features = [new("zero")] });
    }

    private static void DrawHinting()
    {
        Row("Subpixel / not"u8, "Subpixel text 気기"u8, TextStyle.Default, TextStyle.Default with { NoSubpixel = true });
        Row("Subpixel / not"u8, "Segoe UI 気기"u8, SegoeUi, SegoeUi with { NoSubpixel = true });

        ImGui.Separator();
        ImGui.Text("Hinted, then not hinted:"u8);
        for (var px = 12; px <= 16; px++)
        {
            var style = AxisBasicProN with { Size = StyleLength.Px(px) };
            Row($"AXIS ProN {px}px", "test 気기氣"u8, style, style with { NoHinting = true });
        }

        Bitmaps("MS Gothic", "Bitmap ゴシック test"u8);
        Bitmaps("SimSun", "Bitmap 宋体 test"u8);

        static void Bitmaps(string name, ReadOnlySpan<byte> text)
        {
            var font = TextStyle.From(name) with { SizeBasis = FontSizeBasis.EmBox };
            for (var px = 11; px <= 20; px++)
            {
                var style = font with { Size = StyleLength.Px(px) };
                Row($"{name} {px}px", text, style, style with { NoHinting = true });
            }
        }
    }

    private static void DrawChains()
    {
        Row("SizeScale 1, 0.8"u8, "Segoe 맑은 Hangul"u8, SegoeMalgun, SegoeMalgunScaled);

        var small = StyleLength.Px(16);
        var large = StyleLength.Px(32);
        var text = "x2 + y3 = 10"u8;
        Row("GlyphOffset em"u8, text, OffsetEm with { Size = small }, OffsetEm with { Size = large });
        Row("GlyphOffset px"u8, text, OffsetPx with { Size = small }, OffsetPx with { Size = large });
        Row("LetterSpacing"u8, "Digits 0123456789 only"u8, DigitSpacing);

        text = "iiWW..mm"u8;
        Row("Advance 1 em"u8, text, SegoeUi, FixedAdvance);
        Row("Min advance 0.7 em"u8, text, MinAdvance);
        Row("Max advance 0.4 em"u8, text, MaxAdvance);
        Row("FitToSquare"u8, "WM@%—"u8, SegoeUi, FitToSquare);
        Row(
            "Element features"u8,
            "Small Caps office"u8,
            Calibri,
            CalibriSmallCaps,
            CalibriSmallCaps with { Features = NoLigatures });
    }

    private static void DrawRanges()
    {
        RangeSample("FamilyId"u8, TextStyleRange.FamilyId(RangeStart, RangeLength, TextStyle.Mono.FamilyId));
        RangeSample("Color"u8, TextStyleRange.Color(RangeStart, RangeLength, 0xFF40C0FF));
        RangeSample(
            "Shadow"u8,
            TextStyleRange.Shadow(RangeStart, RangeLength, new(0xFF0080FF, new(0.1f, 0.1f))));
        RangeSample("Edge"u8, TextStyleRange.Edge(RangeStart, RangeLength, new(0xFF0000C0)));
        RangeSample("Background"u8, TextStyleRange.Background(RangeStart, RangeLength, 0x804080FF));
        RangeSample(
            "Lines"u8,
            TextStyleRange.Lines(
                RangeStart,
                RangeLength,
                new(TextLinePosition.Under, TextLineStyle.Wavy, 0xFF4040FF)));
        RangeSample(
            "ExtraDecoration"u8,
            TextStyleRange.ExtraDecoration(RangeStart, RangeLength, new(TextDecorationId.Background, 0x804080FF)));
        RangeSample("Bold"u8, TextStyleRange.Bold(RangeStart, RangeLength));
        RangeSample("Italic"u8, TextStyleRange.Italic(RangeStart, RangeLength));

        RangeSample("NoSubpixel"u8, TextStyleRange.NoSubpixel(RangeStart, RangeLength), SegoeUi);
        RangeSample("NoHinting"u8, TextStyleRange.NoHinting(RangeStart, RangeLength), AxisBasicProN);
        RangeSample("Weight"u8, TextStyleRange.Weight(RangeStart, RangeLength, 900), SegoeUi);
        RangeSample("Stretch"u8, TextStyleRange.Stretch(RangeStart, RangeLength, 1), Bahnschrift);
        RangeSample("LetterSpacing"u8, TextStyleRange.LetterSpacing(RangeStart, RangeLength, StyleLength.Em(0.2f)));
        RangeSample("Locale"u8, TextStyleRange.Locale(RangeStart, RangeLength, "zh-CN"), NotoCjk);
        RangeSample("Features"u8, TextStyleRange.Features(RangeStart, RangeLength, SmallCaps), Calibri);

        Label("Overlapping"u8);
        ImGui.TextStyledRanges(
            "Red, both (green wins), green, plain"u8,
            [
                TextStyleRange.Color(0, 24, 0xFF4040FF),
                TextStyleRange.Color(5, 26, 0xFF40FF40),
                TextStyleRange.Bold(0, 31),
                TextStyleRange.Bold(5, 4, false),
            ]);
    }

    private static void DrawLinkInteraction()
    {
        if (!TryBeginLinkInteraction(out var interaction))
            return;
        if (interaction.Link.Detected)
            ImGui.Text($"{interaction.Flags}: found, target {interaction.Link.Target}");
        else
            ImGui.Text($"{interaction.Flags}: of the markup, target of {interaction.Link.Target.Length} bytes");
    }

    private static void MeasuredLabel(string what, Vector2 size)
    {
        ImGui.Text($"{what}: {size.X:0.##}x{size.Y:0.##}");
        ImGui.SameLine();
    }

    private static void DrawnSize()
    {
        ImGui.SameLine();
        ImGui.Text($"drawn {ImGui.GetItemRectSize().X:0.##}x{ImGui.GetItemRectSize().Y:0.##}");
    }

    private static void DrawMeasuring(ImFontRef mono)
    {
        var text = "Measured 気기"u8;
        MeasuredLabel("TextStyle.CalcTextSize", MonoItalic.CalcTextSize(text));
        SampleText(MonoItalic, text);
        DrawnSize();
        MeasuredLabel("ImFontRef.CalcTextSize", mono.CalcTextSize(text));
        using (mono.Push())
            ImGui.Text(text);
        DrawnSize();
    }

    private static void DrawLayouts(ImFontRef mono)
    {
        Label("TextStyle.Push, CreateTextLayout"u8);
        using (MonoItalic.Push())
        {
            using var layout = ImGui.CreateTextLayout(
                RangeText,
                [TextStyleRange.Color(RangeStart, RangeLength, 0xFF40C0FF)]);
            ImGui.TextLayout(layout.Ptr);
        }

        Label("ImFontRef.Style.Push, CreateTextLayout"u8);
        using (mono.Style.Push())
        {
            using var layout = ImGui.CreateTextLayout(RangeText, [TextStyleRange.Bold(RangeStart, RangeLength)]);
            ImGui.TextLayout(layout.Ptr);
        }

        ImFontPtr monoPtr = mono;
        ImGui.Text(
            $"ImFontPtr.Ref of Mono: {monoPtr.Ref}, of a null font: {ImFontPtr.Null.Ref?.ToString() ?? "null"}");
    }

    private void DrawApiSamples()
    {
        if (ImGui.CollapsingHeader("Size basis"u8))
            DrawSizeBasis();
        if (ImGui.CollapsingHeader("Game font sizes"u8))
            DrawGameFontSizes();
        if (ImGui.CollapsingHeader("Line height"u8))
            DrawLineHeight();
        if (ImGui.CollapsingHeader("Spacing, edge and shadow"u8))
            DrawSpacing();
        if (ImGui.CollapsingHeader("Weight, stretch and slant"u8))
            DrawFaces();
        if (ImGui.CollapsingHeader("Locale and features"u8))
            DrawLocaleAndFeatures();
        if (ImGui.CollapsingHeader("Hinting and subpixel"u8))
            DrawHinting();
        if (ImGui.CollapsingHeader("Chain adjustments"u8))
            DrawChains();
        if (ImGui.CollapsingHeader("Style ranges"u8))
            DrawRanges();
        if (ImGui.CollapsingHeader("Pushing, measuring and layouts"u8))
            this.DrawPushing();
    }

    private void OnUrlClick(string url, ImGuiMouseButton button)
    {
        this.clickedUrl = $"Clicked with {button}: {url}";
        if (this.openClickedUrl && button == ImGuiMouseButton.Left)
            Util.OpenLink(url);
    }

    private void DrawPushing()
    {
        var mono = this.monoRef ??= (TextStyle.Mono with { Size = StyleLength.Px(20) }).GetFont();
        this.DrawPushedScopes();
        this.DrawUrls();

        ImGui.Separator();
        this.DrawFamilies(mono);
        this.DrawKeptFont();

        ImGui.Separator();
        DrawMeasuring(mono);
        DrawLayouts(mono);

        ImGui.Separator();
        foreach (var line in ParsedLengths)
            ImGui.Text(line);
    }

    private void DrawPushedScopes()
    {
        using (ImGui.PushFontScale(1.5f))
            ImGui.Text("PushFontScale(1.5)"u8);
        using (ImGui.PushFontSize(20, 1.5f))
            ImGui.Text("PushFontSize(20, 1.5)\nSecond line"u8);
        using (ImGui.PushFontSize(StyleLength.Em(0.8f), StyleLength.Em(2)))
            ImGui.Text("PushFontSize(0.8 em, 2 em)\nSecond line"u8);

        using (ImGui.PushTextMarkup(TextMarkup.MacroString))
            ImGui.Text("PushTextMarkup(MacroString): <italic(1)>italic<italic(0)>"u8);
        using (ImGui.PushTextMarkup(TextMarkup.MacroString))
        using (ImGui.PushTextMarkup(TextMarkup.Plain))
            ImGui.Text("PushTextMarkup(Plain) in a macro string: <italic(1)>as is<italic(0)>"u8);
        using (ImGui.PushTextMarkup(TextMarkup.SeString))
            ImGui.Text(SeStringSample.Data.Span);
        ImGui.TextStyled("TextStyled(Mono, italic)"u8, MonoItalic);
        using (ImGui.PushTextMarkup(TextMarkup.MacroString, this))
        {
            ImGui.Text("PushTextMarkup(MacroString, this): <link(0x0E,1,2,3,test)>a styled link<link(0xCE)>"u8);
            using (ImGui.PushTextCallback(null, inherit: false))
                ImGui.Text("PushTextCallback(null, inherit: false): <link(0x0E,1,2,3,test)>cut off<link(0xCE)>"u8);
        }
    }

    private void DrawUrls()
    {
        using (ImGui.PushTextMarkup(TextMarkup.Plain, TextMarkupCallbacks.Urls))
        {
            ImGui.TextMarkup(
                "Plain text with TextMarkupCallbacks.Urls: https://example.com/a_(b), or www.example.com."u8,
                TextMarkup.Plain,
                this);
            DrawLinkInteraction();
            ImGui.TextMacroString(
                "Macro string: <italic(1)>https://example.com/<italic(0)>split, "u8 +
                "<link(0x0E,1,2,3,test)>a link https://example.com/<link(0xCE)>, (http://example.com/x)"u8,
                this);
            DrawLinkInteraction();
            ImGui.InputText("URL in an InputText"u8, ref this.urlInput, 256);
        }

        this.urlsWithHandler ??= TextMarkupCallbacks.CreateUrlsWithHandler(this.OnUrlClick);
        using (ImGui.PushTextMarkup(TextMarkup.Plain, this.urlsWithHandler))
            ImGui.Text("CreateUrlsWithHandler: click https://example.com/ with any button."u8);
        ImGui.Checkbox("Open in browser on left click"u8, ref this.openClickedUrl);
        ImGui.SameLine();
        ImGui.Text(this.clickedUrl);
    }

    private void DrawFamilies(ImFontRef mono)
    {
        ImFontPtr monoPtr = mono;
        using (ImGui.PushFontScale(1.25f))
        {
            using (ImGui.PushFamily(ComicSansFamily))
                ImGui.Text("PushFamily(IFontFamilyId), at 1.25x"u8);

            this.comicFace ??= ComicSansFamily.Fonts.FirstOrDefault();
            if (this.comicFace is { } face)
            {
                using (ImGui.PushFamily(face))
                    ImGui.Text("PushFamily(IFontFaceId), at 1.25x"u8);
            }

            using (TextStyle.Mono.PushFamily())
                ImGui.Text("TextStyle.PushFamily, at 1.25x"u8);
            using (monoPtr.Style.PushFamily())
                ImGui.Text("ImFontPtr.Style.PushFamily, at 1.25x"u8);
            using (mono.Push())
                ImGui.Text("ImFontRef.Push, at its own 20px"u8);
            using (monoPtr.Push())
                ImGui.Text("ImFontPtr.Push, at its own 20px"u8);
        }
    }

    private void DrawKeptFont()
    {
        if (this.keptFont.IsNull)
        {
            this.keptFont = (TextStyle.Mono with { Italic = true, Bold = true }).GetFont();
            this.keptFrame = ImGui.GetFrameCount();
        }

        using (this.keptFont.Push())
        {
            ImGui.Text(
                $"ImFontPtr kept from frame {this.keptFrame}: mono bold italic then, the current font now");
        }

        ImGui.SameLine();
        if (ImGui.SmallButton("Keep again"u8))
            this.keptFont = default;
    }
}
