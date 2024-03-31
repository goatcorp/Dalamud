using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Controls.Containers;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Spannables.Text;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>The window that displays the Dalamud log file in-game.</summary>
internal partial class ConsoleWindow
{
    [DebuggerDisplay("{entry}")]
    private class LogEntryControl : ContainerControl
    {
        private static readonly TextStyle MetaLabelStyleTiny = new()
        {
            ForeColor = 0xFFCCCCCC,
            FontSize = -0.7f,
            Font = new(DalamudDefaultFontAndFamilyId.Instance),
        };

        private static readonly TextStyle MetaLabelStyleFull = new()
        {
            ForeColor = 0xFFCCCCCC,
            FontSize = 0,
            Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
        };

        private static readonly TextStyle LogTextStyle = new()
        {
            ForeColor = 0xFFFFFFFF,
            Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
        };

        private readonly LabelControl lblTime;
        private readonly LabelControl lblLevel;
        private readonly LabelControl lblText;

        private LogEntry? entry;
        private RowMode rowMode;
        private Regex? highlightRegex;

        private MatchCollection? matches;

        public LogEntryControl()
        {
            this.ChildrenList.Add(
                this.lblTime = new()
                {
                    SpannableTextOptions = new AbstractStyledText.Options { WordBreak = WordBreakType.BreakWord },
                });

            this.ChildrenList.Add(
                this.lblLevel = new()
                {
                    SpannableTextOptions = new AbstractStyledText.Options { WordBreak = WordBreakType.BreakWord },
                });

            this.ChildrenList.Add(this.lblText = new());

            this.Size = new(MatchParent, WrapContent);
            this.Padding = new(2);
        }

        public event PropertyChangeEventHandler<LogEntry?>? EntryChange;

        public event PropertyChangeEventHandler<Regex>? HighlightRegexChange;

        public event PropertyChangeEventHandler<AbstractStyledText.Options>?
            TextSpannableOptionsChange;

        private enum RowMode
        {
            /// <summary>[Time | Level | Text].</summary>
            OneLine,

            /// <summary>[Time]  [Level]<br />[Text.........].</summary>
            TwoLines,

            /// <summary>[Time]....<br />...[Level]<br />[Text....].</summary>
            ThreeLines,
        }

        public LogEntry? Entry
        {
            get => this.entry;
            set => this.HandlePropertyChange(nameof(this.Entry), ref this.entry, value, this.entry == value, this.OnEntryChange);
        }

        public Regex? HighlightRegex
        {
            get => this.highlightRegex;
            set => this.HandlePropertyChange(
                nameof(this.HighlightRegex),
                ref this.highlightRegex,
                value, this.highlightRegex == value, this.OnHighlightRegexChange);
        }

        public AbstractStyledText.Options? TextSpannableOptions
        {
            get => this.lblText.SpannableTextOptions as AbstractStyledText.Options;
            set
            {
                var storage = this.lblText.SpannableTextOptions as AbstractStyledText.Options;
                this.HandlePropertyChange(
                    nameof(this.TextSpannableOptions),
                    ref storage,
                    value,
                    ReferenceEquals(storage, value),
                    this.OnWordBreakChange);
                this.lblText.SpannableTextOptions = storage;
            }
        }

        protected override RectVector4 MeasureChildren(
            Vector2 suggestedSize,
            ReadOnlySpan<Spannable> children)
        {
            if (this.Renderer is null)
                return RectVector4.InvertedExtrema;
            
            // This value is not scaled.
            this.rowMode = this.Options.VisibleSize.X switch
            {
                >= 640 => RowMode.OneLine,
                >= 120 => RowMode.TwoLines,
                _ => RowMode.ThreeLines,
            };

            this.lblTime.Renderer = this.Renderer;
            this.lblLevel.Renderer = this.Renderer;
            this.lblText.Renderer = this.Renderer;
            this.lblTime.Options.RenderScale = this.EffectiveRenderScale;
            this.lblLevel.Options.RenderScale = this.EffectiveRenderScale;
            this.lblText.Options.RenderScale = this.EffectiveRenderScale;
            this.lblTime.Options.VisibleSize = this.Options.VisibleSize;
            this.lblLevel.Options.VisibleSize = this.Options.VisibleSize;
            this.lblText.Options.VisibleSize = this.Options.VisibleSize;

            var isKeepAll = this.TextSpannableOptions?.WordBreak == WordBreakType.KeepAll;
            switch (this.rowMode)
            {
                case RowMode.OneLine:
                    this.Size = new(isKeepAll ? WrapContent : MatchParent, WrapContent);
                    if (this.Renderer.TryGetFontData(this.EffectiveRenderScale, MetaLabelStyleFull, out var fontData))
                    {
                        this.lblTime.Size = new(
                            fontData.CalcTextSizeSimple("00:00:00.000  ").Right,
                            fontData.ScaledFontSize);
                        this.lblLevel.Size = new(
                            fontData.CalcTextSizeSimple("AAA  ").Right,
                            fontData.ScaledFontSize);
                    }
                    else
                    {
                        this.lblTime.Size = ImGui.CalcTextSize("00:00:00.000  ");
                        this.lblLevel.Size = ImGui.CalcTextSize("AAA  ");
                    }

                    this.lblTime.TextStyle = MetaLabelStyleFull;
                    this.lblLevel.TextStyle = MetaLabelStyleFull;
                    this.lblLevel.Alignment = new(0f);

                    this.lblTime.Options.PreferredSize = new(float.PositiveInfinity);
                    this.lblLevel.Options.PreferredSize = new(float.PositiveInfinity);

                    this.lblTime.RenderPassMeasure();
                    this.lblLevel.RenderPassMeasure();

                    this.lblText.TextStyle = LogTextStyle;
                    if (isKeepAll)
                    {
                        this.lblText.Size = new(WrapContent);
                        this.lblText.Options.PreferredSize = new(float.PositiveInfinity);
                    }
                    else
                    {
                        var w = this.lblText.Options.VisibleSize.X
                                - this.lblTime.Boundary.Right
                                - this.lblLevel.Boundary.Right;
                        this.lblText.Size = new(MatchParent, WrapContent);
                        this.lblText.Options.PreferredSize = new(w, float.MaxValue);
                    }

                    this.lblText.RenderPassMeasure();
                    break;

                case RowMode.TwoLines:
                case RowMode.ThreeLines:
                    this.Size = new(MatchParent, WrapContent);
                    this.lblTime.TextStyle = MetaLabelStyleTiny;
                    this.lblTime.Size = new(MatchParent, WrapContent);
                    this.lblLevel.TextStyle = MetaLabelStyleTiny;
                    this.lblLevel.Size = new(MatchParent, WrapContent);
                    this.lblLevel.Margin = BorderVector4.Zero;
                    this.lblLevel.Alignment = new(1, 0);
                    this.lblText.TextStyle = LogTextStyle;
                    this.lblText.Size = new(MatchParent, WrapContent);

                    this.lblTime.Options.PreferredSize = suggestedSize;
                    this.lblLevel.Options.PreferredSize = suggestedSize;
                    this.lblText.Options.PreferredSize = suggestedSize with { Y = float.PositiveInfinity };

                    this.lblTime.RenderPassMeasure();
                    this.lblLevel.RenderPassMeasure();
                    this.lblText.RenderPassMeasure();
                    break;
            }

            var timeSize = this.lblTime.MeasuredBoundaryBox;
            var levelSize = this.lblLevel.MeasuredBoundaryBox;
            var textSize = this.lblText.MeasuredBoundaryBox;

            switch (this.rowMode)
            {
                case RowMode.OneLine:
                default:
                    return new(
                        Vector2.Zero,
                        new(timeSize.Width + levelSize.Width + textSize.Width, textSize.Bottom));
                case RowMode.TwoLines:
                    return new(
                        Vector2.Zero,
                        suggestedSize with { Y = Math.Max(timeSize.Bottom, levelSize.Bottom) + textSize.Bottom });
                case RowMode.ThreeLines:
                    return new(
                        Vector2.Zero,
                        suggestedSize with { Y = timeSize.Bottom + levelSize.Bottom + textSize.Bottom });
            }
        }

        protected override void PlaceChildren(
            SpannableEventArgs args,
            ReadOnlySpan<Spannable> children)
        {
            var mcblt = new Vector3(this.MeasuredContentBox.LeftTop, 0);
            switch (this.rowMode)
            {
                case RowMode.OneLine:
                default:
                    this.lblTime.RenderPassPlace(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.RenderPassPlace(
                        Matrix4x4.CreateTranslation(mcblt + new Vector3(this.lblTime.MeasuredBoundaryBox.Right, 0, 0)),
                        this.FullTransformation);
                    this.lblText.RenderPassPlace(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                this.lblTime.MeasuredBoundaryBox.Right + this.lblLevel.MeasuredBoundaryBox.Right,
                                0,
                                0)),
                        this.FullTransformation);
                    break;

                case RowMode.TwoLines:
                    this.lblTime.RenderPassPlace(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.RenderPassPlace(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblText.RenderPassPlace(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(
                                    Math.Max(
                                        this.lblTime.MeasuredBoundaryBox.Bottom,
                                        this.lblLevel.MeasuredBoundaryBox.Bottom) * this.Options.RenderScale) /
                                this.Options.RenderScale,
                                0)),
                        this.FullTransformation);
                    break;

                case RowMode.ThreeLines:
                    this.lblTime.RenderPassPlace(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.RenderPassPlace(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(this.lblTime.MeasuredBoundaryBox.Bottom * this.Options.RenderScale) /
                                this.Options.RenderScale,
                                0)),
                        this.FullTransformation);
                    this.lblText.RenderPassPlace(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(
                                    (this.lblTime.MeasuredBoundaryBox.Bottom +
                                     this.lblLevel.MeasuredBoundaryBox.Bottom) *
                                    this.Options.RenderScale) / this.Options.RenderScale,
                                0)),
                        this.FullTransformation);
                    break;
            }
        }

        protected virtual void OnEntryChange(PropertyChangeEventArgs<LogEntry?> args)
        {
            this.EntryChange?.Invoke(args);
            if (args.State != PropertyChangeState.After)
                return;
            if (args.NewValue is null)
            {
                this.lblTime.Text = this.lblLevel.Text = this.lblText.Text = string.Empty;
                this.lblText.SpannableText = null;
                return;
            }

            this.lblTime.Text = args.NewValue.TimestampString;
            this.lblLevel.Text = GetTextForLogEventLevel(args.NewValue.Level);
            this.UpdateMatches();
        }

        protected virtual void OnHighlightRegexChange(PropertyChangeEventArgs<Regex> args)
        {
            this.HighlightRegexChange?.Invoke(args);
            this.UpdateMatches();
        }

        protected virtual void OnWordBreakChange(PropertyChangeEventArgs<AbstractStyledText.Options> args)
        {
            this.TextSpannableOptionsChange?.Invoke(args);
            this.lblText.SpannableTextOptions = args.NewValue;
        }

        private void UpdateMatches()
        {
            if (this.entry is null)
            {
                this.lblText.Text = null;
                this.lblText.SpannableText = null;
                this.matches = null;
                return;
            }

            this.matches = this.highlightRegex?.Matches(this.entry.Line);
            this.UpdateHighlights();
        }

        private void UpdateHighlights()
        {
            if (this.entry is null)
            {
                this.lblText.Text = null;
                this.lblText.SpannableText = null;
                this.matches = null;
                return;
            }

            if (this.matches is null)
            {
                this.lblText.Text = this.entry.Line;
                this.lblText.SpannableText = null;
                return;
            }

            Span<int> charOffsets = stackalloc int[(this.matches.Count * 2) + 2];
            var charOffsetsIndex = 1;
            for (var j = 0; j < this.matches.Count; j++)
            {
                var g = this.matches[j].Groups[0];
                charOffsets[charOffsetsIndex++] = g.Index;
                charOffsets[charOffsetsIndex++] = g.Index + g.Length;
            }

            var line = this.entry.Line.AsSpan();
            charOffsets[charOffsetsIndex++] = line.Length;

            var ssb = this.Renderer?.RentBuilder() ?? new();
            for (var i = 0; i < charOffsetsIndex - 1; i++)
            {
                var begin = charOffsets[i];
                var end = charOffsets[i + 1];
                if (i % 2 == 1)
                {
                    ssb.PushForeColor(ImGuiColors.HealerGreen)
                       .PushItalic(i % 2 == 1)
                       .Append(line[begin..end])
                       .PopItalic()
                       .PopForeColor();
                }
                else
                {
                    ssb.Append(line[begin..end]);
                }
            }

            this.lblText.SpannableText = ssb.Build();
            this.Renderer?.ReturnBuilder(ssb);
        }
    }
}
