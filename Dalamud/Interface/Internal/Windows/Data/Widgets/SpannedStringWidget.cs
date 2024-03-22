using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Controls.Buttons;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Rendering.Internal;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying <see cref="SpannedString"/> test.
/// </summary>
internal class SpannedStringWidget : IDataWindowWidget, IDisposable
{
    private readonly Stopwatch stopwatch = new();

    private ISpannable ellipsisSpannable = null!;
    private ISpannable wrapMarkerSpannable = null!;

    private RenderContext.Options renderContextOptions;

    private ImVectorWrapper<byte> testStringBuffer;
    private int numLinkClicks;
    private VerticalAlignment valign;
    private float vertOffset;
    private float wrapLeftWidthRatio;
    private float wrapRightWidthRatio;
    private bool useImages;
    private bool useWrapMarkers;
    private bool useVisibleControlCharacters;
    private bool showComplicatedTextTest;
    private bool showDynamicOffsetTest;
    private bool showTransformationTest;
    private bool showParseTest;
    private WordBreakType wordBreakType;
    private long catchMeBegin;

    private SpannableButton[] spannableButton = null!;

    private (SpannedString? Parsed, Exception? Exception) parseAttempt;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "spannedstring" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Spanned Strings";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    private TextState.Options TextStateOptions => new()
    {
        WordBreak = this.wordBreakType,
        WrapMarker =
            this.useWrapMarkers
                ? this.wordBreakType == WordBreakType.KeepAll
                      ? this.ellipsisSpannable
                      : this.wrapMarkerSpannable
                : null,
        ControlCharactersStyle =
            this.useVisibleControlCharacters
                ? new()
                {
                    Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
                    BackColor = 0xFF004400,
                    ForeColor = 0xFFCCFFCC,
                    FontSize = ImGui.GetFont().FontSize * 0.6f,
                    VerticalAlignment = 0.5f,
                }
                : null,
    };

    /// <inheritdoc/>
    public void Load()
    {
        this.numLinkClicks = 0;
        this.valign = VerticalAlignment.Baseline;
        this.vertOffset = 0;
        this.useImages = false;
        this.wrapLeftWidthRatio = 0f;
        this.wrapRightWidthRatio = 1f;
        this.showComplicatedTextTest = this.showDynamicOffsetTest = this.showTransformationTest = false;
        this.parseAttempt = default;

        this.ellipsisSpannable = new SpannedStringBuilder().PushForeColor(0x80FFFFFF).Append("…");
        this.wrapMarkerSpannable = new SpannedStringBuilder()
                                   .PushFontSet(
                                       new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
                                       out _)
                                   .PushEdgeColor(0xFF000044)
                                   .PushBorderWidth(1)
                                   .PushForeColor(0xFFCCCCFF)
                                   .PushItalic(true)
                                   .PushFontSize(-0.6f)
                                   .PushVerticalAlignment(VerticalAlignment.Middle)
                                   .Append(FontAwesomeIcon.ArrowTurnDown.ToIconString());

        this.spannableButton = new SpannableButton[12];
        for (var i = 0; i < this.spannableButton.Length; i++)
        {
            this.spannableButton[i] = new()
            {
                Margin = new(64, 0, 0, 0),
                SpannableText = new SpannedStringBuilder()
                                .PushVerticalAlignment(0.5f)
                                .PushLink("tlink"u8)
                                .PushBorderWidth(1)
                                .PushEdgeColor(0xFF2222AA)
                                .Append("Link ")
                                .PopEdgeColor()
                                .PopBorderWidth()
                                .PopLink()
                                .PushItalic()
                                .Append("Italic ")
                                .AppendSpannable(
                                    new SpannableButton
                                    {
                                        SpannableText = new SpannedStringBuilder()
                                                        .Append("Inner ")
                                                        .PushSystemFontFamilyIfAvailable("Comic Sans MS")
                                                        .Append("Comic"),
                                    },
                                    null,
                                    out _)
                                .Append(' ')
                                .PushItalic()
                                .Append(i)
                                .PopItalic()
                                .Append(" end"),
            };
        }

        this.testStringBuffer.Dispose();
        this.testStringBuffer = new(65536);
        this.testStringBuffer.StorageSpan.Clear();
        this.testStringBuffer.Clear();
        this.testStringBuffer.AddRange(
            """
            {font-default}Default{/font} {font-asset notojp}NotoJP{/font} {font-game axis}Axis{/font} {font-game miedingermid}Miedinger{/font} {font-system "Comic Sans MS"}Comic{b}Bold{/b}{i}Italic{b}Bold Italic{font-default}DefaulE{/i}{/b}
            Mo{font-asset mono}no{b}bo{/font}ld{/b} {font-asset fa}{\xE4AB}{/font}
            {bw 1}{ec rgba(128 0 255 / 50%)}a{bw 2}s{bw 3}d{bw 4}f{/bw}{/bw}{/bw}{/bw}
            {size 48}{icon dpadleft}{/size}{size 48}{icon dpadright}{/size}
            {va top}top {va bottom}bottom {va baseline}{link "a"}baseline{/link} {size 32}32{/size}{/va}{/va}{/va}{br}
            {va middle}test {vo 0.3} {b}bold{/b} {vo -0.3}{i}italic{/i} {/vo}{/vo} {i}{b}italic {size 48}bold{/i}{/b}{/size}
            {tdc #FFFF9900}
            {tds Double}1/16 Double {tdt 0.0625}{td _}Underlined{/td} {td -}Strikethrough{/td} {td ^}Over{/td} {td below,above,mid}all{/td}{br}
            {tds solid}1/16 Solid {tdt 0.0625}{td _}Underlined{/td} {td -}Strikethrough{/td} {td ^}Over{/td} {td below,above,mid}all{/td}{br}
            {tds Double}1/8 Double {tdt 0.125}{td _}Underlined{/td} {td -}Strikethrough{/td} {td ^}Over{/td} {td below,above,mid}all{/td}{br}
            {tds solid}1/8 Solid {tdt 0.125}{td _}Underlined{/td} {td -}Strikethrough{/td} {td ^}Over{/td} {td below,above,mid}all{/td}{br}
            """u8);

        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.testStringBuffer.Dispose();
    }

    private float angle = 0;

    /// <inheritdoc/>
    public void Draw()
    {
        var renderer = Service<SpannableRenderer>.Get();

        var bgpos = ImGui.GetWindowPos() + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
        ImGui.GetWindowDrawList()
             .AddImage(
                 Service<TextureManager>.Get().GetTextureFromGame("ui/uld/WindowA_BgSelected_HV_hr1.tex")!.ImGuiHandle,
                 bgpos + ImGui.GetWindowContentRegionMin(),
                 bgpos + ImGui.GetWindowContentRegionMax(),
                 Vector2.Zero,
                 (ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()) / 64);
        
        ImGui.SliderFloat("##angle", ref this.angle, 0f, MathF.PI * 2);
        
        var off = ImGui.GetCursorPos();
        ImGui.SetCursorPos(off);
        for (var i = 0; i < this.spannableButton.Length; i++)
        {
            ImGui.SetCursorPos(off);
            renderer.Render(
                this.spannableButton[i],
                new(
                    $"TestButton{i}",
                    new()
                    {
                        ScreenOffset = ImGui.GetCursorScreenPos() + new Vector2(480, 320),
                        TransformationOrigin = new(0, 0.5f),
                        Transformation = new(
                            Vector2.Zero,
                            Quaternion.CreateFromAxisAngle(Vector3.UnitZ, this.angle + ((i / 6f) * MathF.PI)),
                            Vector2.One,
                            Vector2.Zero),
                    }));
        }
        
        ImGui.SetCursorPos(off + new Vector2(0, 640));

        var dynamicOffsetTestOffset = ImGui.GetCursorScreenPos();
        var pad = MathF.Round(8 * ImGuiHelpers.GlobalScale);
        ImGui.Indent(pad);

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + pad);

        var validWidth = ImGui.GetColumnWidth() - pad;
        ImGui.PushItemWidth(validWidth);
        if (ImGui.SliderFloat("##wrapLeftWidthRatio", ref this.wrapLeftWidthRatio, 0f, 1f))
        {
            if (this.wrapRightWidthRatio < this.wrapLeftWidthRatio)
                this.wrapRightWidthRatio = this.wrapLeftWidthRatio;
        }

        ImGui.PushItemWidth(validWidth);
        ImGui.SliderFloat("##wrapRightWidthRatio", ref this.wrapRightWidthRatio, 0f, 1f);
        {
            if (this.wrapRightWidthRatio < this.wrapLeftWidthRatio)
                this.wrapLeftWidthRatio = this.wrapRightWidthRatio;
        }

        ImGui.GetWindowDrawList()
             .AddLine(
                 new(
                     ImGui.GetCursorScreenPos().X + (validWidth * this.wrapLeftWidthRatio),
                     ImGui.GetWindowPos().Y),
                 new(
                     ImGui.GetCursorScreenPos().X + (validWidth * this.wrapLeftWidthRatio),
                     ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y),
                 0xFFFFFFFF,
                 1);
        ImGui.GetWindowDrawList()
             .AddLine(
                 new(
                     ImGui.GetCursorScreenPos().X + (validWidth * this.wrapRightWidthRatio),
                     ImGui.GetWindowPos().Y),
                 new(
                     ImGui.GetCursorScreenPos().X + (validWidth * this.wrapRightWidthRatio),
                     ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y),
                 0xFFFFFFFF,
                 1);

        dynamicOffsetTestOffset.X += pad + MathF.Round(validWidth * this.wrapLeftWidthRatio);
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = dynamicOffsetTestOffset.X });

        var ssb = renderer.RentBuilder();

        this.renderContextOptions.MaxSize = new(
            validWidth * (this.wrapRightWidthRatio - this.wrapLeftWidthRatio),
            float.MaxValue);
        this.DrawTestConfigBlock(ssb);

        ImGuiHelpers.ScaledDummy(8);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8);

        this.stopwatch.Restart();
        if (this.showComplicatedTextTest)
            this.DrawTestComplicatedTextBlock(ssb, dynamicOffsetTestOffset.X);
        if (this.showParseTest)
            this.DrawParseTest(ssb, dynamicOffsetTestOffset.X);
        if (this.showDynamicOffsetTest)
            this.DrawDynamicOffsetTest(ssb);
        if (this.showTransformationTest)
            this.DrawTransformationTest(ssb);

        renderer.ReturnBuilder(ssb);

        ImGuiHelpers.ScaledDummy(8);
        ImGui.TextUnformatted($"Took {this.stopwatch.Elapsed.TotalMilliseconds:g}ms");
    }

    private static SpannedString? TestEncodeDecode(SpannedString ss)
    {
        var buf = new byte[ss.Encode(default)];
        ss.Encode(buf);
        if (!SpannedString.TryDecode(buf, out var decoded))
            return null;

        for (var i = 0; i < ss.Textures.Count; i++)
            decoded.Textures[i] = ss.Textures[i];
        for (var i = 0; i < ss.FontHandleSets.Count; i++)
            decoded.FontHandleSets[i] = ss.FontHandleSets[i];
        for (var i = 0; i < ss.Spannables.Count; i++)
            decoded.Spannables[i] = ss.Spannables[i];
        return decoded;
    }

    private static SpannedString? TestToStringParse(SpannedString ss)
    {
        var str = ss.ToString(CultureInfo.InvariantCulture);
        if (!SpannedString.TryParse(str, CultureInfo.InvariantCulture, out var decoded))
            return null;

        for (var i = 0; i < ss.Textures.Count; i++)
            decoded.Textures[i] = ss.Textures[i];
        for (var i = 0; i < ss.FontHandleSets.Count; i++)
            decoded.FontHandleSets[i] = ss.FontHandleSets[i];
        for (var i = 0; i < ss.Spannables.Count; i++)
            decoded.Spannables[i] = ss.Spannables[i];
        return decoded;
    }

    // private static void CustomDrawCallback(in SpannedStringCallbackArgs args)
    // {
    //     var hover = ImGui.IsMouseHoveringRect(
    //         args.ScreenOffset + args.Xy0,
    //         args.ScreenOffset + args.Xy1);
    //     args.SwitchToForegroundChannel();
    //     args.DrawListPtr.PushClipRect(
    //         args.ScreenOffset + args.Xy0,
    //         args.ScreenOffset + args.Xy1);
    //     args.DrawListPtr.AddText(
    //         args.FontPtr,
    //         args.FontSize,
    //         args.ScreenOffset + args.Xy0,
    //         (Rgba32)(hover ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed),
    //         $"@line {args.RenderState.LastLineIndex}");
    //     args.DrawListPtr.PopClipRect();
    // }

    private void DrawTestConfigBlock(SpannedStringBuilder ssb)
    {
        ssb.Clear();

        ssb.PushLink("copy"u8)
           .PushEdgeColor(ImGuiColors.TankBlue)
           .PushTextDecoration(TextDecoration.Underline)
           .PushTextDecorationColor(ImGuiColors.TankBlue)
           .PushTextDecorationThickness(1 / ImGui.GetFont().FontSize)
           .PushTextDecorationStyle(TextDecorationStyle.Double)
           .PushBorderWidth(1)
           .Append("Copy ToString")
           .PopBorderWidth()
           .PopTextDecoration()
           .PopTextDecorationColor()
           .PopTextDecorationThickness()
           .PopTextDecorationStyle()
           .PopEdgeColor()
           .PopLink();

        ssb.AppendLine()
           .AppendLine();

        ssb.PushForeColor(0xFFC5E1EE)
           .PushShadowColor(0xFF000000)
           .PushShadowOffset(new(0, 1));

        ssb.PushForeColor(0xFFCCCCCC)
           .Append("Options")
           .PopForeColor()
           .PushLineHeight(0.2f)
           .AppendLine()
           .AppendLine()
           .PopLineHeight()
           .PushHorizontalOffset(1.5f);

        ssb.PushLink("useWrapMarkers"u8)
           .PushForeColor(0xFFFFFFFF)
           .PushShadowOffset(Vector2.Zero)
           .AppendTexture(
               Service<TextureManager>.Get().GetTextureFromGame("ui/uld/CheckBoxA_hr1.tex"),
               this.useWrapMarkers ? new(0.5f, 0) : Vector2.Zero,
               this.useWrapMarkers ? Vector2.One : new(0.5f, 1),
               out var texIdCheckbox)
           .PopShadowOffset()
           .PopForeColor()
           .AppendLine("\u00A0Use Wrap Markers")
           .PopLink();

        ssb.PushLink("useVisibleControlCharacters"u8)
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

        ssb.AppendLine();

        ssb.PopHorizontalOffset()
           .PushForeColor(0xFFCCCCCC)
           .Append("Word Break Type")
           .PopForeColor()
           .PushLineHeight(0.2f)
           .AppendLine()
           .AppendLine()
           .PopLineHeight()
           .PushHorizontalOffset(1.5f);

        ssb.PushLink("wordBreakTypeNormal"u8)
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

        ssb.PushLink("wordBreakTypeBreakAll"u8)
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

        ssb.PushLink("wordBreakTypeKeepAll"u8)
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

        ssb.PushLink("wordBreakTypeBreakWord"u8)
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

        ssb.AppendLine();

        ssb.PopHorizontalOffset()
           .PushForeColor(0xFFCCCCCC)
           .Append("Tests")
           .PopForeColor()
           .PushLineHeight(0.2f)
           .AppendLine()
           .AppendLine()
           .PopLineHeight()
           .PushHorizontalOffset(1.5f);

        ssb.PushLink("showComplicatedTextTest"u8)
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

        ssb.PushLink("showDynamicOffsetTest"u8)
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

        ssb.PushLink("showTransformationTest"u8)
           .PushForeColor(0xFFFFFFFF)
           .PushShadowOffset(Vector2.Zero)
           .AppendTexture(
               texIdCheckbox,
               this.showTransformationTest ? new(0.5f, 0) : Vector2.Zero,
               this.showTransformationTest ? Vector2.One : new(0.5f, 1))
           .PopShadowOffset()
           .PopForeColor()
           .AppendLine("\u00A0Test Transformation")
           .PopLink();

        ssb.PushLink("showParseTest"u8)
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

        var mtx = Matrix4x4.Identity;
        mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateRotationZ((15 / 180f) * MathF.PI));

        var renderResult = Service<SpannableRenderer>.Get().Render(
            ssb,
            new(nameof(this.DrawTestConfigBlock), this.renderContextOptions with { Transformation = new(mtx) }),
            this.TextStateOptions);
        if (renderResult.TryGetLinkOnClick(out var link))
        {
            if (link.SequenceEqual("copy"u8))
                this.CopyMe(ssb.Build().ToString());
            else if (link.SequenceEqual("useWrapMarkers"u8))
                this.useWrapMarkers ^= true;
            else if (link.SequenceEqual("useVisibleControlCharacters"u8))
                this.useVisibleControlCharacters ^= true;
            else if (link.SequenceEqual("showComplicatedTextTest"u8))
                this.showComplicatedTextTest ^= true;
            else if (link.SequenceEqual("showDynamicOffsetTest"u8))
                this.showDynamicOffsetTest ^= true;
            else if (link.SequenceEqual("showTransformationTest"u8))
                this.showTransformationTest ^= true;
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

    private void DrawTestComplicatedTextBlock(SpannedStringBuilder ssb, float x)
    {
        ssb.Clear()
           .PushLink("copy"u8)
           .PushEdgeColor(ImGuiColors.HealerGreen)
           .PushBorderWidth(1)
           .Append("Copy ToString")
           .PopBorderWidth()
           .PopEdgeColor()
           .PopLink()
           .AppendLine()
           .AppendLine();

        // TODO
        // ssb.PushLineHeight(2)
        //    .AppendSpannable(CustomDrawCallback, 4, out _)
        //    .PopLineHeight()
        //    .AppendLine()
        //    .AppendLine();

        var fontSizeCounter = 9;
        ssb.PushLink("valign_next"u8)
           .PushVerticalAlignment(this.valign)
           .PushVerticalOffset(this.vertOffset);
        foreach (var c in $"Vertical Align: {this.valign}")
        {
            ssb.PushFontSet(new(DalamudDefaultFontAndFamilyId.Instance), out _)
               .PushFontSize((fontSizeCounter * 4f) / 3)
               .PushBackColor(0xFF111111)
               .Append(c)
               .PopBackColor()
               .PopFontSize()
               .PopFontSet();
            fontSizeCounter++;
        }

        ssb.PopLink()
           .PopVerticalAlignment()
           .PopVerticalOffset()
           .Append(' ')
           .PushBackColor(0xFF000044)
           .PushFontSize(18)
           .PushVerticalAlignment(VerticalAlignment.Middle)
           .PushLink("valign_up"u8)
           .AppendIcon(GfdIcon.RelativeLocationUp)
           .PopLink()
           .PushLink("valign_down"u8)
           .AppendIcon(GfdIcon.RelativeLocationDown)
           .PopLink()
           .PushLink("image_toggle"u8)
           .PushFontSet(new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)), out _)
           .PushFontSize(18)
           .PushVerticalAlignment(VerticalAlignment.Middle)
           .Append(FontAwesomeIcon.Image.ToIconChar())
           .PopVerticalAlignment()
           .PopFontSize()
           .PopFontSet()
           .PopFontSize()
           .PopVerticalAlignment()
           .PopBackColor()
           .PopLink()
           .AppendLine()
           .AppendLine();

        ssb.PushForeColor(0xFFC5E1EE)
           .PushShadowColor(0xFF000000)
           .PushShadowOffset(new(0, 1))
           .Append("Soft Hyphen test:"u8);
        for (var c = 'a'; c <= 'z'; c++)
        {
            for (var i = 0; i < 10; i++)
            {
                ssb.AppendChar(i == 0 ? ' ' : '\u00AD', i == 0 ? (c - 'a') + 1 : 1)
                   .PushLink("a"u8)
                   .PushItalic(i % 2 == 0)
                   .Append(c, 5)
                   .PopItalic()
                   .PopLink();
            }
        }

        ssb.PopShadowOffset()
           .PopShadowColor()
           .AppendLine()
           .PopForeColor()
           .AppendLine();

        ssb.PushLink("Link 1"u8)
           .PushHorizontalAlignment(HorizontalAlignment.Center)
           .Append("This link is clicked "u8)
           .PushBold(true)
           .Append(this.numLinkClicks)
           .PopBold()
           .Append(" times."u8)
           .PopHorizontalAlignment()
           .PopLink()
           .AppendLine()
           .AppendLine();

        ssb.PushLink("Link 2"u8)
           .PushForeColor(0xFF00CC00)
           .PushEdgeColor(0xFF005500)
           .PushBorderWidth(1)
           .PushHorizontalAlignment(HorizontalAlignment.Right)
           .Append("Another "u8)
           .PushFontSet(new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)), out _)
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
           .PopBorderWidth()
           .PopEdgeColor()
           .PopForeColor()
           .PopLink()
           .AppendLine()
           .AppendLine();

        if (this.useImages)
        {
            for (var i = 0; i < 30; i++)
            {
                var tex = Service<TextureManager>.Get().GetTextureFromGame($"ui/icon/000000/{i:000000}.tex");
                ssb.AppendTexture(tex, out _)
                   .Append("UI#")
                   .PushForeColor(0xFF9999FF)
                   .Append(i)
                   .PopForeColor()
                   .Append(' ');
            }

            ssb.AppendLine()
               .AppendLine();

            foreach (var e in Enum.GetValues<GfdIcon>())
            {
                ssb.AppendIcon(e)
                   .Append('#')
                   .PushForeColor(0xFFFF9999)
                   .Append((int)e)
                   .PopForeColor()
                   .Append('\u00A0')
                   .Append(Enum.GetName(e))
                   .Append(' ');
            }
        }

        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
        if (Service<SpannableRenderer>.Get().Render(
                ssb,
                new(nameof(this.DrawTestComplicatedTextBlock), this.renderContextOptions),
                this.TextStateOptions).TryGetLinkOnClick(out var link))
        {
            if (link.SequenceEqual("copy"u8))
            {
                this.CopyMe(ssb.Build().ToString());
            }
            else if (link.SequenceEqual("Link 1"u8))
            {
                this.numLinkClicks++;
            }
            else if (link.SequenceEqual("valign_up"u8))
            {
                this.vertOffset -= 1 / 8f;
            }
            else if (link.SequenceEqual("valign_next"u8))
            {
                this.valign =
                    (VerticalAlignment)(((int)this.valign + 1) %
                                        Enum.GetValues<VerticalAlignment>().Length);
            }
            else if (link.SequenceEqual("valign_down"u8))
            {
                this.vertOffset += 1 / 8f;
            }
            else if (link.SequenceEqual("image_toggle"u8))
            {
                this.useImages ^= true;
            }

            if (!link.SequenceEqual("a"u8))
            {
                unsafe
                {
                    fixed (byte* p2 = link)
                        ImGuiNative.igSetTooltip(p2);
                }
            }
        }
    }

    private unsafe void DrawParseTest(SpannedStringBuilder ssb, float x)
    {
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        fixed (byte* labelPtr = "##DrawParseTestTextInput"u8)
        {
            if (ImGuiNative.igInputTextMultiline(
                    labelPtr,
                    this.testStringBuffer.Data,
                    (uint)this.testStringBuffer.Capacity,
                    new(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 8),
                    0,
                    null,
                    null) != 0
                || this.parseAttempt == default)
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

        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
        if (this.parseAttempt.Parsed is { } parsed)
        {
            Service<SpannableRenderer>.Get().Render(
                parsed,
                new(nameof(this.DrawParseTest), this.renderContextOptions),
                this.TextStateOptions);
        }
        else if (this.parseAttempt.Exception is { } e)
        {
            Service<SpannableRenderer>.Get().Render(
                ssb.Clear()
                   .PushEdgeColor(new Rgba32(ImGuiColors.DalamudRed).MultiplyOpacity(0.5f))
                   .Append(e.ToString()),
                new(nameof(this.DrawParseTest), this.renderContextOptions),
                this.TextStateOptions);
        }
        else
        {
            Service<SpannableRenderer>.Get().Render(
                ssb.Clear().Append("Try writing something to the above text box."),
                new(nameof(this.DrawParseTest), this.renderContextOptions),
                this.TextStateOptions);
        }
    }

    private void DrawDynamicOffsetTest(SpannedStringBuilder ssb)
    {
        const float interval = 2000;
        var v = ((this.catchMeBegin + Environment.TickCount64) / interval) % (2 * MathF.PI);
        var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

        ssb.Clear()
           .PushHorizontalOffset(MathF.Sin(v) * 8)
           .PushVerticalOffset(MathF.Cos(v) * 8)
           .PushBorderWidth(1)
           .PushEdgeColor(new(new Vector4(0.3f, 0.3f, 1f, 0.5f + (MathF.Sin(v) * 0.5f))))
           .PushLink("a"u8)
           .Append("Text\ngoing\nround");

        var prevPos = ImGui.GetCursorScreenPos();
        if (Service<SpannableRenderer>.Get().Render(
                ssb,
                new(
                    nameof(this.DrawDynamicOffsetTest),
                    this.renderContextOptions with
                    {
                        MaxSize = size,
                        ScreenOffset = ImGui.GetWindowPos(),
                    }),
                this.TextStateOptions with
                {
                    VerticalAlignment = 0.5f,
                    InitialStyle = TextStyle.FromContext with
                    {
                        HorizontalAlignment = 0.5f,
                    },
                }).TryGetLink(out _))
            this.catchMeBegin += 50;
        ImGui.SetCursorScreenPos(prevPos);
    }

    private void DrawTransformationTest(SpannedStringBuilder ssb)
    {
        const float interval = 2000;
        var v = ((this.catchMeBegin + Environment.TickCount64) / interval) % (2 * MathF.PI);
        var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
        var minDim = Math.Min(size.X, size.Y);

        ssb.Clear()
           .PushLink("a"u8)
           .Append(
               "Text\ngoing\nround\nusing\nmatrix\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|\n|");

        var prevPos = ImGui.GetCursorScreenPos();

        var mtx = Matrix4x4.Identity;
        mtx = Matrix4x4.Multiply(mtx, Matrix4x4.CreateRotationZ(v));
        mtx = Matrix4x4.Multiply(
            mtx,
            Matrix4x4.CreateTranslation(
                (minDim * (1 + MathF.Cos(v - (MathF.PI / 2)) / 4)) / 2,
                (minDim * (1 + MathF.Sin(v - (MathF.PI / 2)) / 4)) / 2,
                0));

        if (Service<SpannableRenderer>.Get().Render(
                ssb,
                new(
                    nameof(this.DrawDynamicOffsetTest),
                    this.renderContextOptions with
                    {
                        ScreenOffset = ImGui.GetWindowPos(),
                        Transformation = new(mtx),
                        MaxSize = size with { X = 0 },
                    }),
                this.TextStateOptions with
                {
                    WordBreak = WordBreakType.KeepAll,
                    InitialStyle = TextStyle.FromContext with
                    {
                        BorderWidth = 1f,
                        EdgeColor = new Vector4(0.3f, 0.3f, 1f, 0.5f + (MathF.Sin(v) * 0.5f)),
                        HorizontalAlignment = 0.5f,
                    },
                }).TryGetLink(out _))
            this.catchMeBegin += 50;
        ImGui.SetCursorScreenPos(prevPos);
    }

    private void CopyMe(string what)
    {
        ImGui.SetClipboardText(what);
        Service<NotificationManager>.Get().AddNotification(
            $"Copied parseable representation. (Length: {what.Length})",
            this.DisplayName,
            NotificationType.Info);
    }
}
