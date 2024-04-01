using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

using Dalamud.Game;
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
using Dalamud.Interface.Spannables.Controls.Gestures;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.Controls.RecyclerViews;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Rendering.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying <see cref="StyledText"/> test.
/// </summary>
internal class TextSpannableWidget : IDataWindowWidget, IDisposable
{
    private readonly Stopwatch stopwatch = new();

    private ISpannableTemplate ellipsisSpannableTemplate = null!;
    private ISpannableTemplate wrapMarkerSpannableTemplate = null!;

    private RenderContext.Options renderContextOptions;

    private ImVectorWrapper<byte> testStringBuffer;
    private bool setupControlNeeded;
    private int numLinkClicks;
    private VerticalAlignment valign;
    private float vertOffset;
    private bool useImages;
    private bool useWrapMarkers;
    private bool useVisibleControlCharacters;
    private bool showComplicatedTextTest;
    private bool showDynamicOffsetTest;
    private bool showTransformationTest;
    private bool showParseTest;
    private WordBreakType wordBreakType;

    private ContainerControl rootContainer = null!;
    private LabelControl lblStopwatch = null!;

    private LabelControl? lblComplicated;

    private (AbstractStyledText.TextSpannable? Parsed, Exception? Exception) parseAttempt;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "TextSpannable" };

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
        this.useImages = false;
        this.showComplicatedTextTest = this.showDynamicOffsetTest = this.showTransformationTest = false;
        this.showParseTest = false;
        this.parseAttempt = default;
        this.setupControlNeeded = true;

        this.ellipsisSpannableTemplate = new StyledTextBuilder().PushForeColor(0x80FFFFFF).Append("…");
        this.wrapMarkerSpannableTemplate = new StyledTextBuilder()
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

        this.lblComplicated = null;

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
        this.lblStopwatch.Text =
            $"{this.stopwatch.Elapsed.TotalMilliseconds:0}ms {this.stopwatch.Elapsed.Microseconds:000}us {this.stopwatch.Elapsed.Nanoseconds:000}ns";

        var bgpos = ImGui.GetWindowPos() + new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
        ImGui.GetWindowDrawList()
             .AddImage(
                 Service<TextureManager>.Get().GetTextureFromGame("ui/uld/WindowA_BgSelected_HV_hr1.tex")!.ImGuiHandle,
                 bgpos + ImGui.GetWindowContentRegionMin(),
                 bgpos + ImGui.GetWindowContentRegionMax(),
                 Vector2.Zero,
                 (ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()) / 64);

        var myopt = (AbstractStyledText.Options)(this.renderContextOptions.RootOptions ??=
                                                     new AbstractStyledText.Options());

        myopt.Style = TextStyle.FromContext;
        myopt.WordBreak = this.wordBreakType;
        myopt.WrapMarker =
            this.useWrapMarkers
                ? this.wordBreakType == WordBreakType.KeepAll
                      ? this.ellipsisSpannableTemplate
                      : this.wrapMarkerSpannableTemplate
                : null;
        myopt.ControlCharactersStyle =
            this.useVisibleControlCharacters
                ? new()
                {
                    Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
                    BackColor = 0xFF004400,
                    ForeColor = 0xFFCCFFCC,
                    FontSize = ImGui.GetFont().FontSize * 0.6f,
                    VerticalAlignment = 0.5f,
                }
                : default;

        renderer.Draw(
            this.rootContainer,
            new("LinearContainerTest", this.renderContextOptions with { Size = ImGui.GetContentRegionAvail() }));

        var dynamicOffsetTestOffset = ImGui.GetCursorScreenPos();
        var pad = MathF.Round(8 * ImGuiHelpers.GlobalScale);
        dynamicOffsetTestOffset.X += pad;
        this.renderContextOptions.Size = new(ImGui.GetColumnWidth() - pad, float.PositiveInfinity);

        ImGuiHelpers.ScaledDummy(8);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(8);

        var ssb = renderer.RentBuilder();

        this.stopwatch.Restart();
        if (this.showComplicatedTextTest)
            this.DrawTestComplicatedTextBlock(ssb, dynamicOffsetTestOffset.X);
        if (this.showParseTest)
            this.DrawParseTest(ssb, dynamicOffsetTestOffset.X);
        if (this.showDynamicOffsetTest)
            this.DrawDynamicOffsetTest(ssb);
        if (this.showTransformationTest)
            this.DrawTransformationTest(ssb);
        this.stopwatch.Stop();

        renderer.ReturnBuilder(ssb);
    }

    private void SetupControls()
    {
        if (!this.setupControlNeeded)
            return;

        var animTimeSpan = TimeSpan.FromMilliseconds(300);
        this.setupControlNeeded = false;

        var defaultStyle = new TextStyle
        {
            ShadowOffset = new(0, 1),
            ShadowColor = 0xFF000000,
            ForeColor = 0xFFC5E1EE,
        };
        var directionTextStyle = defaultStyle with
        {
            Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.FontAwesomeFreeSolid)),
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
        var notLinkStyle = linkStyle with
        {
            EdgeColor = ImGuiColors.DPSRed,
            TextDecoration = TextDecoration.LineThrough,
            TextDecorationColor = ImGuiColors.DPSRed,
            TextDecorationStyle = TextDecorationStyle.Solid,
            TextDecorationThickness = 1 / 8f,
        };
        var h2Style = new TextStyle
        {
            ShadowOffset = new(0, 1),
            ShadowColor = 0xFF000000,
            ForeColor = 0xFFCCCCCC,
        };
        var linearContainer = new LinearContainer
        {
            Name = "columns",
            Direction = LinearDirection.LeftToRight,
            Size = new(ControlSpannable.WrapContent),
            Padding = new(16f),
            TextStyle = defaultStyle,
            ShowAnimation = new SpannableSizeAnimator
            {
                BeforeRatio = new(-0.7f, -0.7f, 0.7f, 0.7f),
                BeforeOpacity = 0f,
                TransformationEasing = new OutCubic(animTimeSpan),
                OpacityEasing = new OutCubic(animTimeSpan),
            },
            TransformationChangeAnimation = new()
            {
                TransformationEasing = new OutCubic(animTimeSpan),
                OpacityEasing = new OutCubic(animTimeSpan),
            },
            ChildrenList =
            {
                new ObservingRecyclerViewControl<ObservableCollection<int>>
                {
                    Size = new(160, 480),
                    LayoutManager = new LinearLayoutManager
                    {
                        Direction = LinearDirection.TopToBottom,
                    },
                }.GetAsOut(out var rvc),
                new LinearContainer
                {
                    Name = "linearContainerTests",
                    Direction = LinearDirection.TopToBottom,
                    Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                    ChildrenList =
                    {
                        new LabelControl().GetAsOut(out this.lblStopwatch),
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblLinearContainerTestsDirection",
                            Text = "Direction",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerTestsDirection",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.LeftToRight,
                            ChildrenList =
                            {
                                new LinearContainer
                                {
                                    Direction = LinearDirection.LeftToRight,
                                    ChildrenList =
                                    {
                                        new RadioControl
                                            {
                                                Text = FontAwesomeIcon.ArrowRight.ToIconString(),
                                                TextStyle = directionTextStyle,
                                                Side = BooleanControl.IconSide.Top,
                                                Alignment = new(0.5f, 0),
                                                Checked = true,
                                            }
                                            .GetAsOut(out var optLinearContainerLtr),
                                        new RadioControl
                                            {
                                                Text = FontAwesomeIcon.ArrowLeft.ToIconString(),
                                                TextStyle = directionTextStyle,
                                                Side = BooleanControl.IconSide.Top,
                                                Alignment = new(0.5f, 0),
                                            }
                                            .GetAsOut(out _).WithBound(optLinearContainerLtr),
                                    },
                                },
                                new ControlSpannable { Size = new(12, 0) },
                                new LinearContainer
                                {
                                    Direction = LinearDirection.LeftToRight,
                                    ChildrenList =
                                    {
                                        new RadioControl
                                            {
                                                Text = FontAwesomeIcon.ArrowDown.ToIconString(),
                                                TextStyle = directionTextStyle,
                                                Side = BooleanControl.IconSide.Top,
                                                Alignment = new(0.5f, 0),
                                                Checked = true,
                                            }
                                            .GetAsOut(out var optLinearContainerTtb),
                                        new RadioControl
                                            {
                                                Text = FontAwesomeIcon.ArrowUp.ToIconString(),
                                                TextStyle = directionTextStyle,
                                                Side = BooleanControl.IconSide.Top,
                                                Alignment = new(0.5f, 0),
                                            }
                                            .GetAsOut(out _).WithBound(optLinearContainerTtb),
                                    },
                                },
                            },
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblLinearContainerTestsAlignment",
                            Text = "Alignment",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerTestsAlignment",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.LeftToRight,
                            ChildrenList =
                            {
                                new RadioControl
                                    {
                                        Text = "L",
                                        Checked = true,
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignHorzLeft),
                                new RadioControl
                                    {
                                        Text = "M",
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignHorzMid).WithBound(optAlignHorzLeft),
                                new RadioControl
                                    {
                                        Text = "R",
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignHorzRight).WithBound(optAlignHorzLeft),
                                new ControlSpannable { Size = new(12, 0) },
                                new RadioControl
                                    {
                                        Text = "T",
                                        Checked = true,
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignVertTop),
                                new RadioControl
                                    {
                                        Text = "M",
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignVertMid).WithBound(optAlignVertTop),
                                new RadioControl
                                    {
                                        Text = "B",
                                        Side = BooleanControl.IconSide.Top,
                                        Alignment = new(0.5f, 0),
                                        TextStyle = defaultStyle with { HorizontalAlignment = 0.5f },
                                    }
                                    .GetAsOut(out var optAlignVertBottom).WithBound(optAlignVertTop),
                            },
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblLinearContainerTestsBias",
                            Text = "Bias",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerTestsBias",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.LeftToRight,
                            ChildrenList =
                            {
                                new RadioControl
                                    {
                                        Text = "0/4",
                                        Checked = true,
                                        Side = BooleanControl.IconSide.Top,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBias0),
                                new RadioControl
                                    {
                                        Text = "1/4",
                                        Side = BooleanControl.IconSide.Top,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBias1).WithBound(optBias0),
                                new RadioControl
                                    {
                                        Text = "2/4",
                                        Side = BooleanControl.IconSide.Top,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBias2).WithBound(optBias0),
                                new RadioControl
                                    {
                                        Text = "3/4",
                                        Side = BooleanControl.IconSide.Top,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBias3).WithBound(optBias0),
                                new RadioControl
                                    {
                                        Text = "4/4",
                                        Side = BooleanControl.IconSide.Top,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBias4).WithBound(optBias0),
                            },
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new ButtonControl
                        {
                            Text = "Rotate it",
                            Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                            TextStyle = defaultStyle,
                        }.GetAsOut(out var cmdRotate),
                    },
                },
                new LinearContainer
                {
                    Name = "textSpannableTests",
                    Direction = LinearDirection.TopToBottom,
                    Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                    ChildrenList =
                    {
                        new LabelControl
                        {
                            Name = "lblNotALink",
                            SpannableText = new StyledTextBuilder().Append("Not a Link"),
                            TextStyle = notLinkStyle,
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblSpannableTestsOptions",
                            Text = "Options",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerSpannableTestsOptions",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new CheckboxControl
                                    {
                                        Text = "Use Wrap Markers",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkWrapMarker),
                                new CheckboxControl
                                    {
                                        Text = "Show Control Characters",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkVisibleControlCharacters),
                            },
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblSpannableTestsWordBreakType",
                            Text = "Word Break Type",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerSpannableTestsWordBreakType",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new RadioControl
                                    {
                                        Text = "Normal",
                                        Checked = true,
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBreakNormal),
                                new RadioControl
                                    {
                                        Text = "Break All",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBreakAll).WithBound(optBreakNormal),
                                new RadioControl
                                    {
                                        Text = "Keep All",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optKeepAll).WithBound(optBreakNormal),
                                new RadioControl
                                    {
                                        Text = "Break Word",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var optBreakWord).WithBound(optKeepAll),
                            },
                        },
                        new ControlSpannable { Size = new(0, 12) },
                        new LabelControl
                        {
                            Name = "lblSpannableTestsMiscTests",
                            Text = "Miscellaneous Tests",
                            Margin = new(0, 4),
                            TextStyle = h2Style,
                        },
                        new LinearContainer
                        {
                            Name = "linearContainerSpannableTestsMiscTests",
                            Margin = new(16f, 0f),
                            Direction = LinearDirection.TopToBottom,
                            ChildrenList =
                            {
                                new CheckboxControl
                                    {
                                        Text = "Complicated Text",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkComplicatedText),
                                new CheckboxControl
                                    {
                                        Text = "Dynamic Offset",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkDynamicOffset),
                                new CheckboxControl
                                    {
                                        Text = "Transformation",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkTransformation),
                                new CheckboxControl
                                    {
                                        Text = "Parsing",
                                        TextStyle = defaultStyle,
                                    }
                                    .GetAsOut(out var chkParsing),
                            },
                        },
                    },
                },
                new LabelControl().GetAsOut(out var lblOptions),
            },
        };

        rvc.Collection = new(Enumerable.Range(0, 100));
        rvc.DecideSpannableType += e => e.SpannableType = e.Index % 3;
        rvc.AddMoreSpannables += e => rvc.AddPlaceholder(
            e.SpannableType,
            new LabelControl
            {
                Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
                Padding = new(8 * e.SpannableType),
            });
        rvc.PopulateSpannable += e => ((LabelControl)e.Spannable).Text = $"{e.Index} ({e.SpannableType})";

        this.rootContainer = new()
        {
            Name = "root",
            Size = new(ControlSpannable.MatchParent),
            ChildrenList = { linearContainer },
            UseDefaultScrollHandling = false,
        };

        foreach (var lc in this.rootContainer.EnumerateHierarchy<LinearContainer>())
        {
            foreach (var dc in lc.ChildrenReadOnlyList.OfType<ControlSpannable>())
                dc.MoveAnimation = new SpannableSizeAnimator { TransformationEasing = new InOutCubic(animTimeSpan) };
        }

        // TODO
        var mat = new MouseActivityTracker(this.rootContainer);
        var pzt = new PanZoomTracker(mat);
        mat.UseLeftDrag = true;
        mat.UseMiddleDrag = true;
        mat.UseRightDrag = true;
        mat.UseLeftDouble = true;
        mat.UseWheelZoom = MouseActivityTracker.WheelZoomMode.RequireControlKey;
        mat.UseDoubleClickDragZoom = true;
        mat.UseInfiniteLeftDrag = true;
        mat.UseInfiniteRightDrag = true;
        mat.UseInfiniteMiddleDrag = true;
        pzt.PanExtraRange = new(64);
        pzt.ViewportChanged += () =>
        {
            this.rootContainer.ScrollBoundary = new(new Vector2(-float.PositiveInfinity), new(float.PositiveInfinity));
            // this.rootContainer.Scroll = linearContainer.MeasuredContentBox.Center -
            //                             this.rootContainer.MeasuredContentBox.Center - pzt.Pan;
            this.rootContainer.Scroll =
                linearContainer.MeasuredContentBox.Center -
                this.rootContainer.MeasuredContentBox.Center - pzt.Pan;
            linearContainer.Scale = pzt.EffectiveZoom;
            linearContainer.Transformation = Matrix4x4.CreateRotationZ(pzt.Rotation);
        };
        this.rootContainer.MeasuredBoundaryBoxChange += _ =>
        {
            pzt.Size = linearContainer.MeasuredBoundaryBox.Size * ImGuiHelpers.GlobalScale;
        };
        cmdRotate.Click += _ => pzt.Rotation += MathF.PI / 16f;

        optLinearContainerLtr.CheckedChange += e =>
            linearContainer.Direction = e.NewValue
                                            ? LinearDirection.LeftToRight
                                            : LinearDirection.RightToLeft;
        optLinearContainerTtb.CheckedChange += e =>
        {
            foreach (var x in linearContainer.ChildrenList.OfType<LinearContainer>())
            {
                x.Direction =
                    e.NewValue
                        ? LinearDirection.TopToBottom
                        : LinearDirection.BottomToTop;
            }
        };

        optAlignHorzLeft.CheckedChange += UpdateHorzAlignment;
        optAlignHorzMid.CheckedChange += UpdateHorzAlignment;
        optAlignHorzRight.CheckedChange += UpdateHorzAlignment;
        optAlignVertTop.CheckedChange += UpdateVertAlignment;
        optAlignVertMid.CheckedChange += UpdateVertAlignment;
        optAlignVertBottom.CheckedChange += UpdateVertAlignment;
        optBias0.CheckedChange += UpdateBias;
        optBias1.CheckedChange += UpdateBias;
        optBias2.CheckedChange += UpdateBias;
        optBias3.CheckedChange += UpdateBias;
        optBias4.CheckedChange += UpdateBias;

        linearContainer.SetChildLayout(0, new() { Weight = 0.25f });
        linearContainer.SetChildLayout(1, new() { Weight = 0.25f });
        linearContainer.SetChildLayout(2, new() { Weight = 0.00f });
        linearContainer.SuppressNextAnimation();

        this.lblStopwatch.LinkMouseClick += e => Log.Information($"Clicked with {e.Button}");

        chkWrapMarker.CheckedChange += e => this.useWrapMarkers = e.NewValue;
        chkVisibleControlCharacters.CheckedChange += e => this.useVisibleControlCharacters = e.NewValue;
        chkWrapMarker.CheckedChange += UpdateLblOptions;
        chkVisibleControlCharacters.CheckedChange += UpdateLblOptions;

        optBreakNormal.CheckedChange += e =>
        {
            if (e.NewValue) this.wordBreakType = WordBreakType.Normal;
        };
        optBreakAll.CheckedChange += e =>
        {
            if (e.NewValue) this.wordBreakType = WordBreakType.BreakAll;
        };
        optKeepAll.CheckedChange += e =>
        {
            if (e.NewValue) this.wordBreakType = WordBreakType.KeepAll;
        };
        optBreakWord.CheckedChange += e =>
        {
            if (e.NewValue) this.wordBreakType = WordBreakType.BreakWord;
        };
        optBreakNormal.CheckedChange += UpdateLblOptions;
        optBreakAll.CheckedChange += UpdateLblOptions;
        optKeepAll.CheckedChange += UpdateLblOptions;
        optBreakWord.CheckedChange += UpdateLblOptions;

        chkComplicatedText.CheckedChange += e => this.showComplicatedTextTest = e.NewValue;
        chkDynamicOffset.CheckedChange += e => this.showDynamicOffsetTest = e.NewValue;
        chkTransformation.CheckedChange += e => this.showTransformationTest = e.NewValue;
        chkComplicatedText.CheckedChange += UpdateLblOptions;
        chkDynamicOffset.CheckedChange += UpdateLblOptions;
        chkTransformation.CheckedChange += UpdateLblOptions;

        chkParsing.CheckedChange += e => this.showParseTest = e.NewValue;
        chkParsing.CheckedChange += UpdateLblOptions;

        lblOptions.LinkMouseClick += e =>
        {
            var linkSpan = e.Link.Span;
            if (linkSpan.SequenceEqual("copy"u8))
                this.CopyMe(lblOptions.SpannableText?.ToString() ?? string.Empty);
            else if (linkSpan.SequenceEqual("useWrapMarkers"u8))
                chkWrapMarker.Checked = this.useWrapMarkers ^= true;
            else if (linkSpan.SequenceEqual("useVisibleControlCharacters"u8))
                chkVisibleControlCharacters.Checked = this.useVisibleControlCharacters ^= true;
            else if (linkSpan.SequenceEqual("showComplicatedTextTest"u8))
                chkComplicatedText.Checked = this.showComplicatedTextTest ^= true;
            else if (linkSpan.SequenceEqual("showDynamicOffsetTest"u8))
                chkDynamicOffset.Checked = this.showDynamicOffsetTest ^= true;
            else if (linkSpan.SequenceEqual("showTransformationTest"u8))
                chkTransformation.Checked = this.showTransformationTest ^= true;
            else if (linkSpan.SequenceEqual("showParseTest"u8))
                chkParsing.Checked = this.showParseTest ^= true;

            if (linkSpan.SequenceEqual("wordBreakTypeNormal"u8))
            {
                optBreakNormal.Checked = true;
                this.wordBreakType = WordBreakType.Normal;
            }
            else if (linkSpan.SequenceEqual("wordBreakTypeBreakAll"u8))
            {
                optBreakAll.Checked = true;
                this.wordBreakType = WordBreakType.BreakAll;
            }
            else if (linkSpan.SequenceEqual("wordBreakTypeKeepAll"u8))
            {
                optKeepAll.Checked = true;
                this.wordBreakType = WordBreakType.KeepAll;
            }
            else if (linkSpan.SequenceEqual("wordBreakTypeBreakWord"u8))
            {
                optBreakWord.Checked = true;
                this.wordBreakType = WordBreakType.BreakWord;
            }
        };

        lblOptions.LinkMouseDown += e => Log.Information($"LinkMouseDown: {Encoding.UTF8.GetString(e.Link.Span)}");
        lblOptions.LinkMouseUp += e => Log.Information($"LinkMouseUp: {Encoding.UTF8.GetString(e.Link.Span)}");
        lblOptions.LinkMouseEnter += e => Log.Information($"LinkMouseEnter: {Encoding.UTF8.GetString(e.Link.Span)}");
        lblOptions.LinkMouseLeave += e => Log.Information($"LinkMouseLeave: {Encoding.UTF8.GetString(e.Link.Span)}");

        UpdateLblOptions(null);
        return;

        void UpdateHorzAlignment(PropertyChangeEventArgs<bool> args)
        {
            if (args.State != PropertyChangeState.After)
                return;
            var n = optAlignHorzLeft.Checked ? 0f : optAlignHorzMid.Checked ? 0.5f : 1f;
            foreach (var x in linearContainer.EnumerateHierarchy<LinearContainer>())
            {
                if (x.Direction is LinearDirection.TopToBottom
                    or LinearDirection.BottomToTop)
                {
                    for (var i = 0; i < x.ChildrenList.Count; i++)
                        x.SetChildLayout(i, x.GetChildLayout(i) with { Alignment = n });
                }
            }
        }

        void UpdateVertAlignment(PropertyChangeEventArgs<bool> args)
        {
            if (args.State != PropertyChangeState.After)
                return;
            var n = optAlignVertTop.Checked ? 0f : optAlignVertMid.Checked ? 0.5f : 1f;
            foreach (var x in linearContainer.EnumerateHierarchy<LinearContainer>())
            {
                if (x.Direction is LinearDirection.LeftToRight
                    or LinearDirection.RightToLeft)
                {
                    for (var i = 0; i < x.ChildrenList.Count; i++)
                        x.SetChildLayout(i, x.GetChildLayout(i) with { Alignment = n });
                }
            }
        }

        void UpdateBias(PropertyChangeEventArgs<bool> args)
        {
            if (args.State != PropertyChangeState.After)
                return;
            var n = optBias0.Checked ? 0f : optBias1.Checked ? 1f : optBias2.Checked ? 2f : optBias3.Checked ? 3f : 4f;
            foreach (var x in linearContainer.EnumerateHierarchy<LinearContainer>())
                x.ContentBias = n / 4f;
        }

        void UpdateLblOptions(PropertyChangeEventArgs<bool>? e)
        {
            if (e?.State is PropertyChangeState.Before)
                return;
            if (this.renderContextOptions.RootOptions is null)
            {
                Service<Framework>.Get().RunOnTick(() => UpdateLblOptions(null));
                return;
            }

            var opt = (AbstractStyledText.Options)this.renderContextOptions.RootOptions!;
            opt.Style = TextStyle.FromContext;
            lblOptions.SpannableTextOptions = opt;
            lblOptions.TextStyle = opt.Style;
            lblOptions.SpannableText =
                new StyledTextBuilder()
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
                    .PopLink();
        }
    }

    private void DrawTestComplicatedTextBlock(StyledTextBuilder ssb, float x)
    {
        if (this.lblComplicated is null)
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

            this.lblComplicated = new()
            {
                SpannableText = ssb.Build(),
                Size = new(ControlSpannable.MatchParent, ControlSpannable.WrapContent),
            };
            this.lblComplicated.LinkMouseClick += e =>
            {
                var link = e.Link.Span;
                if (link.SequenceEqual("copy"u8))
                {
                    this.CopyMe(ssb.Build().ToString());
                }
                else if (link.SequenceEqual("Link 1"u8))
                {
                    this.numLinkClicks++;
                    this.lblComplicated = null;
                }
                else if (link.SequenceEqual("valign_up"u8))
                {
                    this.vertOffset -= 1 / 8f;
                    this.lblComplicated = null;
                }
                else if (link.SequenceEqual("valign_next"u8))
                {
                    this.valign =
                        (VerticalAlignment)(((int)this.valign + 1) %
                                            Enum.GetValues<VerticalAlignment>().Length);
                    this.lblComplicated = null;
                }
                else if (link.SequenceEqual("valign_down"u8))
                {
                    this.vertOffset += 1 / 8f;
                    this.lblComplicated = null;
                }
                else if (link.SequenceEqual("image_toggle"u8))
                {
                    this.useImages ^= true;
                    this.lblComplicated = null;
                }

                if (!link.SequenceEqual("a"u8))
                {
                    unsafe
                    {
                        fixed (byte* p2 = link)
                            ImGuiNative.igSetTooltip(p2);
                    }
                }
            };
        }

        if (this.lblComplicated is null)
            return;

        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
        Service<SpannableRenderer>.Get().Draw(
            this.lblComplicated,
            new(nameof(this.DrawTestComplicatedTextBlock), this.renderContextOptions));
    }

    private unsafe void DrawParseTest(StyledTextBuilder ssb, float x)
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

                if (StyledText.TryParse(
                        Encoding.UTF8.GetString(this.testStringBuffer.DataSpan),
                        CultureInfo.InvariantCulture,
                        out var r,
                        out var e))
                    this.parseAttempt = (r.CreateSpannable(), null);
                else
                    this.parseAttempt = (null, e);
            }
        }

        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = x });
        if (this.parseAttempt.Parsed is { } parsed)
        {
            Service<SpannableRenderer>.Get().Draw(
                parsed,
                new(nameof(this.DrawParseTest), this.renderContextOptions));
        }
        else if (this.parseAttempt.Exception is { } e)
        {
            ssb.RecycleSpannable(
                Service<SpannableRenderer>.Get().Draw(
                    ssb.Clear()
                       .PushEdgeColor(new Rgba32(ImGuiColors.DalamudRed).MultiplyOpacity(0.5f))
                       .Append(e.ToString())
                       .CreateSpannable(),
                    new(nameof(this.DrawParseTest), this.renderContextOptions)));
        }
        else
        {
            ssb.RecycleSpannable(
                Service<SpannableRenderer>.Get().Draw(
                    ssb.Clear().Append("Try writing something to the above text box.").CreateSpannable(),
                    new(nameof(this.DrawParseTest), this.renderContextOptions)));
        }
    }

    private void DrawDynamicOffsetTest(StyledTextBuilder ssb)
    {
        const float interval = 2000;
        var v = (Environment.TickCount64 / interval) % (2 * MathF.PI);
        var size = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();

        ssb.Clear()
           .PushHorizontalOffset(MathF.Sin(v) * 8)
           .PushVerticalOffset(MathF.Cos(v) * 8)
           .PushEdgeWidth(1)
           .PushEdgeColor(new(new Vector4(0.3f, 0.3f, 1f, 0.5f + (MathF.Sin(v) * 0.5f))))
           .PushLink("a"u8)
           .Append("Text\ngoing\nround");

        var prevPos = ImGui.GetCursorScreenPos();
        var o2 = (AbstractStyledText.Options)this.renderContextOptions.RootOptions!;
        o2.VerticalAlignment = 0.5f;
        o2.Style = TextStyle.FromContext with { HorizontalAlignment = 0.5f };
        ssb.RecycleSpannable(
            Service<SpannableRenderer>.Get().Draw(
                ssb.CreateSpannable(),
                new(
                    nameof(this.DrawDynamicOffsetTest),
                    this.renderContextOptions with
                    {
                        Size = size,
                        ScreenOffset = ImGui.GetWindowPos(),
                        RootOptions = o2,
                    })));
        ImGui.SetCursorScreenPos(prevPos);
    }

    private void DrawTransformationTest(StyledTextBuilder ssb)
    {
        const float interval = 2000;
        var v = (Environment.TickCount64 / interval) % (2 * MathF.PI);
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

        var o2 = (AbstractStyledText.Options)this.renderContextOptions.RootOptions!;
        o2.WordBreak = WordBreakType.KeepAll;
        o2.Style = TextStyle.FromContext with
        {
            EdgeWidth = 1f,
            EdgeColor = new Vector4(0.3f, 0.3f, 1f, 0.5f + (MathF.Sin(v) * 0.5f)),
            HorizontalAlignment = 0.5f,
        };
        ssb.RecycleSpannable(
            Service<SpannableRenderer>.Get().Draw(
                ssb.CreateSpannable(),
                new(
                    nameof(this.DrawDynamicOffsetTest),
                    this.renderContextOptions with
                    {
                        ScreenOffset = ImGui.GetWindowPos(),
                        Transformation = mtx,
                        Size = size with { X = 0 },
                        RootOptions = o2,
                    })));
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
