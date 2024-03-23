using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Game.Text;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Controls;
using Dalamud.Interface.Spannables.Controls.Animations;
using Dalamud.Interface.Spannables.Controls.Containers;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Patterns;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Rendering.Internal;
using Dalamud.Interface.Spannables.Strings;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

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
    private bool setupControlNeeded;
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
    private bool showFlowerTest;
    private WordBreakType wordBreakType;
    private long catchMeBegin;

    private float buttonFlowerAngle;
    private float buttonFlowerScale;

    private ButtonControl[] spannableButton = null!;
    private LinearContainer linearContainer = null!;

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
        this.showFlowerTest = this.showParseTest = false;
        this.parseAttempt = default;
        this.buttonFlowerAngle = 0;
        this.buttonFlowerScale = 1f;
        this.setupControlNeeded = true;

        this.ellipsisSpannable = new SpannedStringBuilder().PushForeColor(0x80FFFFFF).Append("…");
        this.wrapMarkerSpannable = new SpannedStringBuilder()
                                   .PushFontSet(
                                       new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
                                       out _)
                                   .PushEdgeColor(0xFF000044)
                                   .PushEdgeWidth(1)
                                   .PushForeColor(0xFFCCCCFF)
                                   .PushItalic(true)
                                   .PushFontSize(-0.4f)
                                   .PushLineHeight(2.5f)
                                   .PushVerticalAlignment(VerticalAlignment.Middle)
                                   .Append(FontAwesomeIcon.ArrowTurnDown.ToIconString());

        this.testStringBuffer.Dispose();
        this.testStringBuffer = new(65536);
        this.testStringBuffer.StorageSpan.Clear();
        this.testStringBuffer.Clear();
        this.testStringBuffer.AddRange(
            """
            {font-default}Default{/font} {font-asset notojp}NotoJP{/font} {font-game axis}Axis{/font} {font-game miedingermid}Miedinger{/font} {font-system "Comic Sans MS"}Comic{b}Bold{/b}{i}Italic{b}Bold Italic{font-default}DefaulE{/i}{/b}
            Mo{font-asset mono}no{b}bo{/font}ld{/b} {font-asset fa}{\xE4AB}{/font}
            {ew 1}{ec rgba(128 0 255 / 50%)}a{ew 2}s{ew 3}d{ew 4}f{/ew}{/ew}{/ew}{/ew}
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

    /// <inheritdoc/>
    public void Draw()
    {
        var renderer = Service<SpannableRenderer>.Get();

        this.SetupControls();

        var bgpos = ImGui.GetWindowPos() + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
        ImGui.GetWindowDrawList()
             .AddImage(
                 Service<TextureManager>.Get().GetTextureFromGame("ui/uld/WindowA_BgSelected_HV_hr1.tex")!.ImGuiHandle,
                 bgpos + ImGui.GetWindowContentRegionMin(),
                 bgpos + ImGui.GetWindowContentRegionMax(),
                 Vector2.Zero,
                 (ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()) / 64);

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

        this.renderContextOptions.MaxSize = new(
            validWidth * (this.wrapRightWidthRatio - this.wrapLeftWidthRatio),
            float.PositiveInfinity);

        renderer.Render(
            this.linearContainer,
            new("LinearContainerTest", this.renderContextOptions),
            this.TextStateOptions);

        ImGuiHelpers.ScaledDummy(8);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8);

        var ssb = renderer.RentBuilder();
        this.stopwatch.Restart();
        if (this.showComplicatedTextTest)
            this.DrawTestComplicatedTextBlock(ssb, dynamicOffsetTestOffset.X);
        if (this.showParseTest)
            this.DrawParseTest(ssb, dynamicOffsetTestOffset.X);
        if (this.showFlowerTest)
            this.DrawFlowerTest();
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

    private void SetupControls()
    {
        if (!this.setupControlNeeded)
            return;

        this.setupControlNeeded = false;
        this.spannableButton = new ButtonControl[12];
        for (var i = 0; i < this.spannableButton.Length; i++)
        {
            this.spannableButton[i] = new()
            {
                Margin = new(64, 0, 0, 0),
                // Margin = new(0, 0, 0, 0),
                Enabled = i % 4 != 3,
                ShowAnimation = new SpannableSizeAnimator
                {
                    BeforeRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
                    BeforeOpacity = 0f,
                    TransformationEasing = new OutCubic(TimeSpan.FromMilliseconds(3000)),
                    OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(3000)),
                },
                HideAnimation = new SpannableSizeAnimator
                {
                    AfterRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
                    AfterOpacity = 0f,
                    TransformationEasing = new InCubic(TimeSpan.FromMilliseconds(300)),
                    OpacityEasing = new InCubic(TimeSpan.FromMilliseconds(300)),
                },
                SpannableText = new SpannedStringBuilder()
                                .PushVerticalAlignment(0.5f)
                                .PushLink("tlink"u8)
                                .PushEdgeWidth(1)
                                .PushEdgeColor(0xFF2222AA)
                                .Append("Link ")
                                .PopEdgeColor()
                                .PopEdgeWidth()
                                .PopLink()
                                .PushItalic()
                                .Append("Italic ")
                                .AppendSpannable(
                                    new ButtonControl
                                    {
                                        SpannableText = new SpannedStringBuilder()
                                                        .Append("Inner ")
                                                        .PushSystemFontFamilyIfAvailable("Comic Sans MS")
                                                        .Append("Comic"),
                                    },
                                    out _)
                                .Append(' ')
                                .PushItalic()
                                .Append(i)
                                .PopItalic()
                                .Append(" end")
                                .Build(),
            };

            if (i % 3 == 0)
            {
                this.spannableButton[i].NormalBackground =
                    new LayeredPattern
                    {
                        ChildrenList =
                        {
                            new ShapePattern { ImGuiColor = ImGuiCol.Button, Type = ShapePattern.Shape.RectFilled },
                            new ShapePattern { Color = 0xFF000000, Type = ShapePattern.Shape.Rect },
                        },
                    };
            }
        }

        var defaultStyle = new TextStyle
        {
            ShadowOffset = new(0, 1),
            ShadowColor = 0xFF000000,
            ForeColor = 0xFFC5E1EE,
        };
        var linkStyle = new TextStyle
        {
            EdgeColor = ImGuiColors.TankBlue,
            EdgeWidth = 1,
            ForeColor = ImGuiColors.DalamudWhite,
            TextDecoration = TextDecoration.Underline,
            TextDecorationColor = ImGuiColors.TankBlue,
            TextDecorationThickness = 1 / 8f,
            TextDecorationStyle = TextDecorationStyle.Double,
        };
        var h2Style = new TextStyle
        {
            ShadowOffset = new(0, 1),
            ShadowColor = 0xFF000000,
            ForeColor = 0xFFCCCCCC,
        };
        this.linearContainer = new()
        {
            ContentBias = 0.3f,
            Direction = LinearContainer.LinearDirection.LeftToRight,
            Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
            Padding = new(16f),
            TextStateOptions = new() { InitialStyle = defaultStyle },
            ShowAnimation = new SpannableSizeAnimator
            {
                BeforeRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
                BeforeOpacity = 0f,
                TransformationEasing = new OutCubic(TimeSpan.FromMilliseconds(500)),
                OpacityEasing = new OutCubic(TimeSpan.FromMilliseconds(500)),
            },
            ChildrenList =
            {
                new LinearContainer
                {
                    Direction = LinearContainer.LinearDirection.TopToBottom,
                    Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                    ChildrenList =
                    {
                        new LabelControl
                        {
                            SpannableText = new SpannedStringBuilder().PushLink("test"u8).Append("Test Link"),
                            TextStateOptions = new() { InitialStyle = linkStyle },
                        }.GetAsOut(out var linkLabel),
                        new LabelControl
                        {
                            Text = "Horizontal LinearContainer",
                            Margin = new(0, 16, 0, 4),
                            TextStateOptions = new() { InitialStyle = h2Style },
                        },
                        new LinearContainer
                        {
                            Margin = new(16f, 0f),
                            Direction = LinearContainer.LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new RadioControl { Text = "Left to Right", Checked = true, }
                                    .GetAsOut(out var optLinearContainerLtr),
                                new RadioControl { Text = "Right to Left", }
                                    .GetAsOut(out _).WithBound(optLinearContainerLtr),
                            },
                        },
                        new LabelControl
                        {
                            Text = "Vertical LinearContainer",
                            Margin = new(0, 16, 0, 4),
                            TextStateOptions = new() { InitialStyle = h2Style },
                        },
                        new LinearContainer
                        {
                            Margin = new(16f, 0f),
                            Direction = LinearContainer.LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new RadioControl { Text = "Top to Bottom", Checked = true, }
                                    .GetAsOut(out var optLinearContainerTtb),
                                new RadioControl { Text = "Bottom to Top", }
                                    .GetAsOut(out _).WithBound(optLinearContainerTtb),
                            },
                        },
                        new LabelControl
                        {
                            Text = "Options",
                            Margin = new(0, 16, 0, 4),
                            TextStateOptions = new() { InitialStyle = h2Style },
                        },
                        new LinearContainer
                        {
                            Margin = new(16f, 0f),
                            Direction = LinearContainer.LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new CheckboxControl { Text = "Use Wrap Markers", }
                                    .GetAsOut(out var chkWrapMarker),
                                new CheckboxControl { Text = "Show Control Characters", }
                                    .GetAsOut(out var chkVisibleControlCharacters),
                            },
                        },
                        new LabelControl
                        {
                            Text = "Word Break Type",
                            Margin = new(0, 16, 0, 4),
                            TextStateOptions = new() { InitialStyle = h2Style },
                        },
                        new LinearContainer
                        {
                            Margin = new(16f, 0f),
                            Direction = LinearContainer.LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new RadioControl { Text = "Normal", Checked = true, }
                                    .GetAsOut(out var optBreakNormal),
                                new RadioControl { Text = "Break All", }
                                    .GetAsOut(out var optBreakAll).WithBound(optBreakNormal),
                                new RadioControl { Text = "Keep All", }
                                    .GetAsOut(out var optKeepAll).WithBound(optBreakNormal),
                                new RadioControl { Text = "Break Word", }
                                    .GetAsOut(out var optBreakWord).WithBound(optKeepAll),
                            },
                        },
                    },
                },
                new LinearContainer
                {
                    Direction = LinearContainer.LinearDirection.TopToBottom,
                    Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                    ChildrenList =
                    {
                        new LabelControl
                        {
                            Text = "Tests",
                            Margin = new(0, 16, 0, 4),
                            TextStateOptions = new() { InitialStyle = h2Style },
                        },
                        new LinearContainer
                        {
                            Margin = new(16f, 0f),
                            Direction = LinearContainer.LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new CheckboxControl { Text = "Complicated Text", }
                                    .GetAsOut(out var chkComplicatedText),
                                new CheckboxControl { Text = "Dynamic Offset", }
                                    .GetAsOut(out var chkDynamicOffset),
                                new CheckboxControl { Text = "Transformation", }
                                    .GetAsOut(out var chkTransformation),
                                new CheckboxControl { Text = "Parsing", }
                                    .GetAsOut(out var chkParsing),
                                new CheckboxControl { Text = "Button Flower", }
                                    .GetAsOut(out var chkFlower),
                            },
                        },
                    },
                },
                new LabelControl { ActiveTextState = new(this.TextStateOptions) }.GetAsOut(out var lblOptions),
            },
        };

        optLinearContainerLtr.CheckedChange += e =>
            this.linearContainer.Direction = e.NewValue is true
                                                 ? LinearContainer.LinearDirection.LeftToRight
                                                 : LinearContainer.LinearDirection.RightToLeft;
        optLinearContainerTtb.CheckedChange += e =>
        {
            var align = e.NewValue is true ? 0f : 1f;
            for (var i = 0; i < this.linearContainer.ChildrenList.Count; i++)
                this.linearContainer.SetChildLayout(i, new() { Weight = i < 2 ? 0.25f : 0f, Alignment = align });
            foreach (var x in this.linearContainer.ChildrenList.OfType<LinearContainer>())
            {
                x.Direction =
                    e.NewValue is true
                        ? LinearContainer.LinearDirection.TopToBottom
                        : LinearContainer.LinearDirection.BottomToTop;
            }
        };

        this.linearContainer.SetChildLayout(0, new() { Weight = 0.25f, Alignment = 0f });
        this.linearContainer.SetChildLayout(1, new() { Weight = 0.25f, Alignment = 0f });

        linkLabel.LinkMouseClick += e => Log.Information($"Clicked with {e.Button}");

        chkWrapMarker.CheckedChange += e => this.useWrapMarkers = e.NewValue is not false;
        chkVisibleControlCharacters.CheckedChange += e => this.useVisibleControlCharacters = e.NewValue is not false;
        chkWrapMarker.CheckedChange += UpdateLblOptions;
        chkVisibleControlCharacters.CheckedChange += UpdateLblOptions;

        optBreakNormal.CheckedChange += e =>
        {
            if (e.NewValue is true) this.wordBreakType = WordBreakType.Normal;
        };
        optBreakAll.CheckedChange += e =>
        {
            if (e.NewValue is true) this.wordBreakType = WordBreakType.BreakAll;
        };
        optKeepAll.CheckedChange += e =>
        {
            if (e.NewValue is true) this.wordBreakType = WordBreakType.KeepAll;
        };
        optBreakWord.CheckedChange += e =>
        {
            if (e.NewValue is true) this.wordBreakType = WordBreakType.BreakWord;
        };
        optBreakNormal.CheckedChange += UpdateLblOptions;
        optBreakAll.CheckedChange += UpdateLblOptions;
        optKeepAll.CheckedChange += UpdateLblOptions;
        optBreakWord.CheckedChange += UpdateLblOptions;

        chkComplicatedText.CheckedChange += e => this.showComplicatedTextTest = e.NewValue is not false;
        chkDynamicOffset.CheckedChange += e => this.showDynamicOffsetTest = e.NewValue is not false;
        chkTransformation.CheckedChange += e => this.showTransformationTest = e.NewValue is not false;
        chkComplicatedText.CheckedChange += UpdateLblOptions;
        chkDynamicOffset.CheckedChange += UpdateLblOptions;
        chkTransformation.CheckedChange += UpdateLblOptions;

        chkParsing.CheckedChange += e => this.showParseTest = e.NewValue is not false;
        chkFlower.CheckedChange += e => this.showFlowerTest = e.NewValue is not false;
        chkParsing.CheckedChange += UpdateLblOptions;
        chkFlower.CheckedChange += UpdateLblOptions;

        lblOptions.LinkMouseClick += e =>
        {
            if (e.Link.SequenceEqual("copy"u8))
                this.CopyMe(lblOptions.SpannableText?.ToString() ?? string.Empty);
            else if (e.Link.SequenceEqual("useWrapMarkers"u8))
                chkWrapMarker.Checked = this.useWrapMarkers ^= true;
            else if (e.Link.SequenceEqual("useVisibleControlCharacters"u8))
                chkVisibleControlCharacters.Checked = this.useVisibleControlCharacters ^= true;
            else if (e.Link.SequenceEqual("showComplicatedTextTest"u8))
                chkComplicatedText.Checked = this.showComplicatedTextTest ^= true;
            else if (e.Link.SequenceEqual("showDynamicOffsetTest"u8))
                chkDynamicOffset.Checked = this.showDynamicOffsetTest ^= true;
            else if (e.Link.SequenceEqual("showTransformationTest"u8))
                chkTransformation.Checked = this.showTransformationTest ^= true;
            else if (e.Link.SequenceEqual("showParseTest"u8))
                chkParsing.Checked = this.showParseTest ^= true;
            else if (e.Link.SequenceEqual("showFlowerTest"u8))
                chkFlower.Checked = this.showFlowerTest ^= true;

            if (e.Link.SequenceEqual("wordBreakTypeNormal"u8))
            {
                optBreakNormal.Checked = true;
                this.wordBreakType = WordBreakType.Normal;
            }
            else if (e.Link.SequenceEqual("wordBreakTypeBreakAll"u8))
            {
                optBreakAll.Checked = true;
                this.wordBreakType = WordBreakType.BreakAll;
            }
            else if (e.Link.SequenceEqual("wordBreakTypeKeepAll"u8))
            {
                optKeepAll.Checked = true;
                this.wordBreakType = WordBreakType.KeepAll;
            }
            else if (e.Link.SequenceEqual("wordBreakTypeBreakWord"u8))
            {
                optBreakWord.Checked = true;
                this.wordBreakType = WordBreakType.BreakWord;
            }
        };

        UpdateLblOptions(default);
        return;

        void UpdateLblOptions(PropertyChangeEventArgs<ControlSpannable, bool?> e)
        {
            lblOptions.ActiveTextState = new(this.TextStateOptions);
            lblOptions.SpannableText =
                new SpannedStringBuilder()
                    .PushLink("copy"u8)
                    .PushEdgeColor(ImGuiColors.TankBlue)
                    .PushTextDecoration(TextDecoration.Underline)
                    .PushTextDecorationColor(ImGuiColors.TankBlue)
                    .PushTextDecorationThickness(1 / 8f)
                    .PushTextDecorationStyle(TextDecorationStyle.Double)
                    .PushEdgeWidth(1)
                    .Append("Copy ToString")
                    .PopEdgeWidth()
                    .PopTextDecoration()
                    .PopTextDecorationColor()
                    .PopTextDecorationThickness()
                    .PopTextDecorationStyle()
                    .PopEdgeColor()
                    .PopLink()
                    .AppendLine()
                    .AppendLine()
                    .PushForeColor(0xFFC5E1EE)
                    .PushShadowColor(0xFF000000)
                    .PushShadowOffset(new(0, 1))
                    .PushForeColor(0xFFCCCCCC)
                    .Append("Options")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f)
                    .PushLink("useWrapMarkers"u8)
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
                    .PopLink()
                    .PushLink("useVisibleControlCharacters"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.useVisibleControlCharacters ? new(0.5f, 0) : Vector2.Zero,
                        this.useVisibleControlCharacters ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Use Visible Control Characters")
                    .PopLink()
                    .AppendLine()
                    .PopHorizontalOffset()
                    .PushForeColor(0xFFCCCCCC)
                    .Append("Word Break Type")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f)
                    .PushLink("wordBreakTypeNormal"u8)
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
                    .PopLink()
                    .PushLink("wordBreakTypeBreakAll"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.BreakAll ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.BreakAll ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Break All")
                    .PopLink()
                    .PushLink("wordBreakTypeKeepAll"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.KeepAll ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.KeepAll ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Keep All")
                    .PopLink()
                    .PushLink("wordBreakTypeBreakWord"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdRadio,
                        this.wordBreakType == WordBreakType.BreakWord ? new(0.5f, 0) : Vector2.Zero,
                        this.wordBreakType == WordBreakType.BreakWord ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Break Word")
                    .PopLink()
                    .AppendLine()
                    .PopHorizontalOffset()
                    .PushForeColor(0xFFCCCCCC)
                    .Append("Tests")
                    .PopForeColor()
                    .PushLineHeight(0.2f)
                    .AppendLine()
                    .AppendLine()
                    .PopLineHeight()
                    .PushHorizontalOffset(1.5f)
                    .PushLink("showComplicatedTextTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showComplicatedTextTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showComplicatedTextTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Complicated Text")
                    .PopLink()
                    .PushLink("showDynamicOffsetTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showDynamicOffsetTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showDynamicOffsetTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine(
                        "\u00A0Test Dynamic Horizontal and Vertical Offsets")
                    .PopLink()
                    .PushLink("showTransformationTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showTransformationTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showTransformationTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Transformation")
                    .PopLink()
                    .PushLink("showParseTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showParseTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showParseTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Parsing")
                    .PopLink()
                    .PushLink("showFlowerTest"u8)
                    .PushForeColor(0xFFFFFFFF)
                    .PushShadowOffset(Vector2.Zero)
                    .AppendTexture(
                        texIdCheckbox,
                        this.showFlowerTest ? new(0.5f, 0) : Vector2.Zero,
                        this.showFlowerTest ? Vector2.One : new(0.5f, 1))
                    .PopShadowOffset()
                    .PopForeColor()
                    .AppendLine("\u00A0Test Flower")
                    .PopLink();
        }
    }

    private void DrawTestComplicatedTextBlock(SpannedStringBuilder ssb, float x)
    {
        ssb.Clear()
           .PushLink("copy"u8)
           .PushEdgeColor(ImGuiColors.HealerGreen)
           .PushEdgeWidth(1)
           .Append("Copy ToString")
           .PopEdgeWidth()
           .PopEdgeColor()
           .PopLink()
           .AppendLine()
           .AppendLine();

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
           .PushEdgeWidth(1)
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
           .PopEdgeWidth()
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

    private void DrawFlowerTest()
    {
        ImGui.SliderFloat("Angle##angle", ref this.buttonFlowerAngle, 0f, MathF.PI * 2);
        ImGui.SliderFloat("Scale##angle", ref this.buttonFlowerScale, 0f, 2f);
        {
            var tmp = this.spannableButton[0].Visible;
            if (ImGui.Checkbox("Visibility?", ref tmp))
            {
                foreach (ref var v in this.spannableButton.AsSpan())
                    v.Visible = tmp;
            }
        }

        var renderer = Service<SpannableRenderer>.Get();
        var off = ImGui.GetCursorPos();
        ImGui.SetCursorPos(off);
        var origin = ImGui.GetCursorScreenPos() + new Vector2(
                         ImGui.GetWindowSize().X / 2f,
                         180 * ImGuiHelpers.GlobalScale);
        ImGui.GetWindowDrawList().AddCircle(origin, 5, uint.MaxValue);
        for (var i = 0; i < this.spannableButton.Length; i++)
        {
            var transformation =
                Matrix4x4.Multiply(
                    Matrix4x4.CreateScale(this.buttonFlowerScale, this.buttonFlowerScale, 1f),
                    Matrix4x4.CreateRotationZ(this.buttonFlowerAngle + ((i / 6f) * MathF.PI)));
            ImGui.SetCursorPos(off);
            renderer.Render(
                this.spannableButton[i],
                new(
                    $"TestButton{i}",
                    new()
                    {
                        ScreenOffset = origin,
                        InnerOrigin = new(0f, 0.5f),
                        Transformation = transformation,
                    }));
        }

        ImGui.SetCursorPos(off + (new Vector2(0, 480) * ImGuiHelpers.GlobalScale));
    }

    private void DrawDynamicOffsetTest(SpannedStringBuilder ssb)
    {
        const float interval = 2000;
        var v = ((this.catchMeBegin + Environment.TickCount64) / interval) % (2 * MathF.PI);
        var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

        ssb.Clear()
           .PushHorizontalOffset(MathF.Sin(v) * 8)
           .PushVerticalOffset(MathF.Cos(v) * 8)
           .PushEdgeWidth(1)
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
                        Transformation = mtx,
                        MaxSize = size with { X = 0 },
                    }),
                this.TextStateOptions with
                {
                    WordBreak = WordBreakType.KeepAll,
                    InitialStyle = TextStyle.FromContext with
                    {
                        EdgeWidth = 1f,
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
