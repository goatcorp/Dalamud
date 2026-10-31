using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Fonts.Internal.Glyphs;
using Dalamud.Interface.Utility.Raii;

using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>Shows samples of fonts drawn with ImGui.</summary>
internal partial class FontSamplesImGuiWidget : IDataWindowWidget, ITextMarkupCallback, IDisposable
{
    private const uint LinkColor = 0xFFFFA64D;
    private const uint HoverLinkColor = 0xFFFFC880;
    private const uint ActiveLinkColor = 0xFF5050FF;

    private static readonly byte[] Icons = Encoding.UTF8.GetBytes(
        string.Concat(
            FontAwesomeIcon.Cog.ToIconString(),
            FontAwesomeIcon.Search.ToIconString(),
            FontAwesomeIcon.Home.ToIconString()));

    // Red to violet in ImGui's ABGR order, from the outermost band.
    private static readonly uint[] RainbowColors =
        [0xFF0000FF, 0xFF00A5FF, 0xFF00FFFF, 0xFF00FF00, 0xFFFF0000, 0xFF82004B, 0xFFEE82EE];

    // Registered on load and unregistered on dispose, as a plugin does through IUiBuilder.
    private TextDecorationRegistration? rainbowEdge;
    private float relativeSize = 1.5f;
    private float pixelSize = 24f;
    private int labelClicks;
    private string largeText = "The quick brown fox jumps over the lazy dog.\nC气氣J気K기 😀👍🎉";
    private float largeSize = 256f;
    private ITextMarkupCallback? seStringLinkHandler;
    private string clickedSeStringLink = "Not clicked yet.";

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["fontspecs"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font: Samples(ImGui)";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.rainbowEdge ??= new(
            TextDecorationRegistry.Register(DrawRainbowEdge, TextDecorationFlags.BelowFill, RainbowColors.Length));
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.rainbowEdge?.Dispose();
        this.rainbowEdge = null;
        this.monoRef = null;
        this.keptFont = default;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        if (ImGui.CollapsingHeader("Samples"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            Sample("Default"u8, TextStyle.Default, "The quick brown fox jumps over the lazy dog. C气氣J気K기"u8);
            Sample("Icon"u8, TextStyle.Icon, Icons);
            Sample("IconFixedWidth"u8, TextStyle.IconFixedWidth, Icons);
            Sample("Emoji"u8, TextStyle.Default, "😀👍🎉❤️🍣 🇯🇵 👨‍👩‍👧 ✋🏽"u8);
            Sample(
                "Segoe UI Emoji 32px"u8, TextStyle.From("Segoe UI Emoji", size: StyleLength.Px(32)), "😀👍🎉❤️🍣"u8);
            Sample("Mono"u8, TextStyle.Mono, "0123456789 iIlL1 oO0 -> C气氣J気K기"u8);
            Sample("AXIS 18pt"u8, TextStyle.From(GameFontFamilyAndSize.Axis18), "The quick brown fox"u8);
            Sample("TrumpGothic 23pt"u8, TextStyle.From(GameFontFamilyAndSize.TrumpGothic23), "The quick brown fox"u8);
            Sample(
                "Segoe UI(BI)+Malgun"u8,
                TextStyle.From(
                    new ChainFamilyId(
                        [new(new SystemFontFamilyId("Segoe UI")), new(new SystemFontFamilyId("Malgun Gothic"))]))
                    with { Bold = true, Italic = true },
                "Bold italic 맑은 C气氣J気K기"u8);

            Sample(
                "Comic+Times+Impact+Wing"u8,
                TextStyle.From(
                    new ChainFamilyId(
                    [
                        new(new SystemFontFamilyId("Comic Sans MS")) { CodepointRanges = ['A', 'Z'] },
                        new(new SystemFontFamilyId("Times New Roman")) { CodepointRanges = ['a', 'z'] },
                        new(new SystemFontFamilyId("Impact")),
                        new(new SystemFontFamilyId("Wingdings")),
                    ])) with { Italic = true },
                "¡Hello 142857 Worlds!"u8);

            // Also can do this if from plugins:
            // using (TextStyle.From(/* my plugin assembly */, "resource name", fontNo: 1).Push())
            //     ImGui.Text("From assembly resource");

            // An em size is a ratio of the current size. Pushing at a size keeps one font for all sizes.
            ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
            ImGui.SliderFloat("Relative size"u8, ref this.relativeSize, 0.5f, 3f, "%.2fx");
            using ((TextStyle.Default with { Size = StyleLength.Em(this.relativeSize) }).Push())
                ImGui.Text("Default, pushed at a ratio of the current size"u8);
            ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
            ImGui.SliderFloat("Pixel size"u8, ref this.pixelSize, 8f, 64f, "%.0f px");
            using (ImGui.PushFontSize(this.pixelSize))
                ImGui.Text("Default, pushed at a size (same font, other size)"u8);
        }

        bool open;
        using (ImGui.PushTextMarkup(TextMarkup.MacroString))
        {
            open = ImGui.CollapsingHeader(
                "<edge(1)><colortype(501)><edgecolortype(502)>T<edgecolortype(0)><colortype(0)><edge(0)>"u8 +
                "<bold(1)><color(0xFFFF8888)>e<color(stackcolor)><bold(0)>"u8 +
                "<italic(1)><color(0xFF88FF88)>x<color(stackcolor)><italic(0)>"u8 +
                "<color(0xFF8888FF)>t<color(stackcolor)>"u8 +
                " styles"u8,
                ImGuiTreeNodeFlags.DefaultOpen);
        }

        if (open)
            this.DrawTextStyles();

        ImGui.SeparatorText("Behavior Samples"u8);

        if (ImGui.CollapsingHeader("Large text"u8))
            this.DrawLargeText();

        this.DrawApiSamples();
    }

    /// <inheritdoc/>
    bool ITextMarkupCallback.TryGetLinkStyle(in TextLinkContext link, in TextStyle style, out TextStyle result)
    {
        result = style;
        result.Lines = new(TextLinePosition.Under);
        result.Color = link.State switch
        {
            TextLinkState.Hovered => HoverLinkColor,
            TextLinkState.Held => ActiveLinkColor,
            _ => LinkColor,
        };
        if (link.Held && ImGui.IsMouseDown(ImGuiMouseButton.Right))
            result.Background = ImGui.GetColorU32(ImGuiCol.TextSelectedBg);
        return true;
    }

    // A band of the rainbow, a pixel narrower than the one before.
    private static void DrawRainbowEdge(in TextDecorationSpan span) =>
        span.DrawGlyphs(RainbowColors[span.Band], RainbowColors.Length - span.Band);

    private static void StyleSamples(ReadOnlySpan<byte> label)
    {
        ImGui.Text(label);
        ImGui.SameLine(0, 0);
        using (ImGui.PushTextBold())
            ImGui.Text("Bold "u8);
        ImGui.SameLine(0, 0);
        using (ImGui.PushTextBold())
        using (ImGui.PushTextItalic())
            ImGui.Text("and "u8);
        ImGui.SameLine(0, 0);
        using (ImGui.PushTextStrikethrough())
            ImGui.Text("Brash "u8);
        ImGui.SameLine(0, 0);
        using (ImGui.PushTextItalic())
            ImGui.Text("Italic "u8);
    }

    private static void Sample(ReadOnlySpan<byte> label, TextStyle spec, ReadOnlySpan<byte> text)
    {
        ImGui.Text(label);
        ImGui.SameLine(ImGui.GetFontSize() * 10);
        SampleText(spec, text);
    }

    private static void SampleText(TextStyle spec, ReadOnlySpan<byte> text)
    {
        // A font none of whose fonts loaded pushes as the current font, which would look like a working sample.
        var font = spec.GetFont();
        if (font.LoadTask.IsFaulted)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "None of the fonts loaded"u8);
        }
        else
        {
            using (font.Push())
                ImGui.Text(text);

            // Fonts of the spec that could not be loaded are left out, and the rest draws.
            if (font.LoadErrors.Count == 0)
                return;
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudOrange, $"({font.LoadErrors.Count} left out)");
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(string.Join('\n', font.LoadErrors.Select(e => e.Message)));
    }

    private void DrawTextStyles()
    {
        using (ImGui.PushTextColor(ImGuiColors.TankBlue))
            ImGui.Text("PushTextColor"u8);
        using (ImGui.PushTextEdge(ImGuiColors.HealerGreen))
            ImGui.Text("PushTextEdge"u8);
        using (ImGui.PushTextEdge(ImGuiColors.DPSRed, StyleLength.Em(0.2f), 1))
            ImGui.Text("PushTextEdge, 0.2 em and soft"u8);
        using (ImGui.PushTextShadow(0xFF000000))
            ImGui.Text("PushTextShadow"u8);
        using (ImGui.PushTextShadow(0xC0000000, new(0.1f, 0.1f)))
            ImGui.Text("PushTextShadow, 0.1 em to the bottom right"u8);
        using (ImGui.PushTextStrikethrough(0xFF4040FF))
            ImGui.Text("PushTextStrikethrough"u8);
        using (ImGui.PushTextUnderline(0xFFFFFFFF))
            ImGui.Text("PushTextUnderline"u8);
        using (ImGui.PushTextBackground(0x804080FF))
            ImGui.Text("PushTextBackground"u8);
        using (ImGui.PushTextOverline())
            ImGui.Text("PushTextOverline"u8);
        using (ImGui.PushTextLines(TextLinePosition.Under, color: 0xFF40C0FF, count: 2))
            ImGui.Text("Double underline"u8);
        using (ImGui.PushTextLines(TextLinePosition.Under, TextLineStyle.Dotted))
            ImGui.Text("Dotted underline"u8);
        using (ImGui.PushTextLines(TextLinePosition.Under | TextLinePosition.Over, TextLineStyle.Dashed))
            ImGui.Text("Dashed underline and overline"u8);
        using (ImGui.PushTextLines(TextLinePosition.Under, TextLineStyle.Wavy, 0xFF4040FF))
            ImGui.Text("Wavy underline"u8);
        using (ImGui.PushTextLines(TextLinePosition.Through, TextLineStyle.Wavy, count: 3))
            ImGui.Text("Three wavy strikethroughs"u8);
        // The built-in decorations again as ExtraDecoration, on top of Lines and Background.
        var wavy = new TextLineDecoration(TextLinePosition.Under, TextLineStyle.Wavy, 0xFF4040FF);
        using (ImGui.PushTextUnderline())
        using (ImGui.PushTextStrikethrough())
        using (ImGui.PushTextDecoration(TextDecorationId.Line, wavy.Color, wavy.ToDecoration().Data))
            ImGui.Text("Underline and strikethrough, and a wavy underline as ExtraDecoration"u8);
        using ((ImGui.TextStyle with
                   {
                       Background = 0x804080FF,
                       ExtraDecoration = new(TextDecorationId.Background, 0x40FFFFFF),
                   }).Push())
        {
            ImGui.Text("Background, and another over it as ExtraDecoration"u8);
        }

        ReadOnlySpan<byte> ranged = "Ranges: highlighted, then squiggled"u8;
        var highlighted = "highlighted"u8;
        var squiggled = "squiggled"u8;
        ImGui.TextStyledRanges(
            ranged,
            [
                TextStyleRange.Color(ranged.IndexOf(highlighted), highlighted.Length, 0xFF40C0FF),
                TextStyleRange.ExtraDecoration(ranged.IndexOf(squiggled), squiggled.Length, wavy),
            ]);
        if (this.rainbowEdge is { } rainbow)
        {
            using (ImGui.PushTextDecoration(rainbow.Id))
                ImGui.Text("Rainbow edge, a decoration drawing the glyphs 7 times under their fill"u8);
        }

        using (ImGui.PushTextBold())
            ImGui.Text("PushTextBold C气氣J気K기"u8);
        using (ImGui.PushTextItalic())
            ImGui.Text("PushTextItalic C气氣J気K기"u8);
        using (ImGui.PushTextBold())
        using (ImGui.PushTextItalic())
        using (ImGui.PushTextEdge(0xFF000000))
        using (ImGui.PushTextShadow(0xFF000000))
            ImGui.Text("Bold, italic, edge and shadow, nested"u8);

        var macroString =
            "Macro string: <colortype(500)>color<colortype(0)> <icon(56)> <italic(1)>italic<italic(0)> "u8 +
            @"<edge(1)>edge<edge(0)> <link(0x0E,1,2,3,test)>a link<link(0xCE)> \<escaped"u8;
        ImGui.TextMacroString(macroString, this);
        if (ImGui.TryGetTextInteraction(out var interaction, TextInteractionFlags.All))
        {
            if ((interaction.Flags & TextInteractionFlags.Hover) != 0)
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            ImGui.SameLine();
            var link = macroString.Slice(interaction.DrawnOffset, interaction.DrawnLength);
            ImGui.Text($"{interaction.Flags}: {link}, target of {interaction.Link.Target.Length} bytes");
        }

        this.seStringLinkHandler ??= TextMarkupCallbacks.CreateSeStringLinkHandler(this.OnSeStringLinkClick);
        using (ImGui.PushTextMarkup(TextMarkup.MacroString, this.seStringLinkHandler))
            ImGui.TextMacroString("CreateSeStringLinkHandler: <link(0x0E,1,2,3,test)>click me<link(0xCE)>"u8, this);
        ImGui.SameLine();
        ImGui.Text(this.clickedSeStringLink);

        // A label with an explicit ID shows "##" in its text as it is, instead of taking it as the ID.
        using (ImGui.PushTextMarkup(TextMarkup.MacroString))
        {
            if (ImGui.Button(new("<colortype(500)>##<colortype(0)> in a macro string label"u8, "macroLabel"u8)))
                this.labelClicks++;
            ImGui.SameLine();

            // Without the text in the ID, the ID stays as the text changes, so the node stays open.
            using var node = ImRaii.TreeNode(
                new($"<italic(1)>##{this.labelClicks}<italic(0)> clicks", "macroLabelNode"u8, appendId: false));
            if (node)
                ImGui.Text("C# and #1 ## #2 show as they are."u8);
        }

        var size = StyleLength.Px(this.pixelSize);
        using (TextStyle.From("Comic Sans MS", size: size).Push())
            StyleSamples("Comic Sans: "u8);
        using (TextStyle.From(GameFontFamily.Axis, size).Push())
            StyleSamples("AXIS (Built-in): "u8);
        using (TextStyle.From("AXIS Basic ProN", size: size).Push())
            StyleSamples("AXIS Basic ProN: "u8);
    }

    private void OnSeStringLinkClick(ReadOnlySePayloadSpan link, ImGuiMouseButton button)
    {
        var macro = new StringBuilder();
        link.AppendMacroStringToStringBuilder(macro, false);
        this.clickedSeStringLink = $"Clicked with {button}: {macro}";
    }

    private void DrawLargeText()
    {
        ImGui.InputTextMultiline(
            "##largeText"u8,
            ref this.largeText,
            1 << 16,
            new(-1, ImGui.GetTextLineHeightWithSpacing() * 4));
        ImGui.SetNextItemWidth(ImGui.GetFontSize() * 20);
        ImGui.SliderFloat("Size"u8, ref this.largeSize, 8f, 4096f, "%.0f px", ImGuiSliderFlags.Logarithmic);
        var atlas = GlyphAtlas.Shared;
        ImGui.Text(
            $"Atlas: {atlas.GlyphCount} glyphs, {atlas.Large.Count} large images " +
            $"({atlas.Large.Bytes / 1048576f:0.#} MiB), {ConcurrentGlyphQueue.TotalPending} being rasterized");

        Vector2 textSize;
        using (ImGui.PushFontSize(this.largeSize))
            textSize = ImGui.CalcTextSize(this.largeText);
        if (textSize.X <= 0 || textSize.Y <= 0)
            return;

        var room = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFontSize() * 8);
        var scale = MathF.Min(room.X / textSize.X, room.Y / textSize.Y);
        var boxSize = Vector2.Max(textSize * scale, new(ImGui.GetFontSize()));
        var boxPos = ImGui.GetCursorScreenPos();
        ImGui.InvisibleButton("##largeTextBox"u8, boxSize);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRect(boxPos, boxPos + boxSize, ImGui.GetColorU32(ImGuiCol.Border));
        if (!ImGui.IsItemHovered())
            return;

        var viewport = ImGui.GetWindowViewport();
        var at = Vector2.Clamp((ImGui.GetMousePos() - boxPos) / boxSize, Vector2.Zero, Vector2.One);
        var origin = viewport.Pos - (at * Vector2.Max(textSize - viewport.Size, Vector2.Zero));

        var shown = boxPos + ((viewport.Pos - origin) * (boxSize / textSize));
        drawList.AddRect(
            Vector2.Max(shown, boxPos),
            Vector2.Min(shown + (viewport.Size * (boxSize / textSize)), boxPos + boxSize),
            ImGui.GetColorU32(ImGuiCol.PlotHistogram));

        var overlay = ImGui.GetForegroundDrawList(viewport);
        overlay.AddRectFilled(viewport.Pos, viewport.Pos + viewport.Size, ImGui.GetColorU32(ImGuiCol.PopupBg));
        using (ImGui.PushFontSize(this.largeSize))
            overlay.AddText(origin, ImGui.GetColorU32(ImGuiCol.Text), this.largeText);
    }
}
