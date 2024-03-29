using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Controls;
using Dalamud.Interface.Spannables.Controls.Containers;
using Dalamud.Interface.Spannables.Controls.EventHandlers;
using Dalamud.Interface.Spannables.Controls.Labels;
using Dalamud.Interface.Spannables.Patterns;
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
        private bool selectedForCopy;

        private MatchCollection? matches;

        public LogEntryControl()
        {
            this.ChildrenList.Add(
                this.lblTime = new()
                {
                    SpannableTextOptions = new TextSpannableBase.Options { WordBreak = WordBreakType.BreakWord },
                });

            this.ChildrenList.Add(
                this.lblLevel = new()
                {
                    SpannableTextOptions = new TextSpannableBase.Options { WordBreak = WordBreakType.BreakWord },
                });

            this.ChildrenList.Add(this.lblText = new());

            this.Size = new(MatchParent, WrapContent);
            this.Padding = new(2);

            this.NormalBackground = new ShapePattern { Type = ShapePattern.Shape.RectFilled };
        }

        public event PropertyChangeEventHandler<ControlSpannable, LogEntry?>? EntryChange;

        public event PropertyChangeEventHandler<ControlSpannable, Regex>? HighlightRegexChange;

        public event PropertyChangeEventHandler<ControlSpannable, bool>? SelectedForCopyChange;

        public event PropertyChangeEventHandler<ControlSpannable, TextSpannableBase.Options>?
            TextSpannableMeasurementOptionsChange;

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
            set => this.HandlePropertyChange(nameof(this.Entry), ref this.entry, value, this.OnEntryChange);
        }

        public Regex? HighlightRegex
        {
            get => this.highlightRegex;
            set => this.HandlePropertyChange(
                nameof(this.HighlightRegex),
                ref this.highlightRegex,
                value,
                this.OnHighlightRegexChange);
        }

        public bool SelectedForCopy
        {
            get => this.selectedForCopy;
            set => this.HandlePropertyChange(
                nameof(this.SelectedForCopy),
                ref this.selectedForCopy,
                value,
                this.OnSelectedForCopyChange);
        }

        public TextSpannableBase.Options? TextSpannableMeasurementOptions
        {
            get => this.lblText.SpannableTextOptions as TextSpannableBase.Options;
            set
            {
                var storage = this.lblText.SpannableTextOptions as TextSpannableBase.Options;
                this.HandlePropertyChange(
                    nameof(this.TextSpannableMeasurementOptions),
                    ref storage,
                    value,
                    this.OnWordBreakChange);
                this.lblText.SpannableTextOptions = storage;
            }
        }

        private ShapePattern Background => (ShapePattern)this.NormalBackground!;

        protected override RectVector4 MeasureChildren(
            Vector2 suggestedSize,
            ReadOnlySpan<ISpannableMeasurement> childMeasurements)
        {
            // This value is not scaled.
            this.rowMode = this.MeasurementOptions.VisibleSize.X switch
            {
                >= 640 => RowMode.OneLine,
                >= 120 => RowMode.TwoLines,
                _ => RowMode.ThreeLines,
            };

            this.lblTime.MeasurementOptions.VisibleSize = this.MeasurementOptions.VisibleSize;
            this.lblLevel.MeasurementOptions.VisibleSize = this.MeasurementOptions.VisibleSize;
            this.lblText.MeasurementOptions.VisibleSize = this.MeasurementOptions.VisibleSize;

            var isKeepAll = this.TextSpannableMeasurementOptions?.WordBreak == WordBreakType.KeepAll;
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

                    this.lblTime.MeasurementOptions.Size = new(float.PositiveInfinity);
                    this.lblLevel.MeasurementOptions.Size = new(float.PositiveInfinity);

                    this.lblTime.ExplicitMeasure();
                    this.lblLevel.ExplicitMeasure();
                    
                    this.lblText.TextStyle = LogTextStyle;
                    if (isKeepAll)
                    {
                        this.lblText.Size = new(WrapContent);
                        this.lblText.MeasurementOptions.Size = new(float.PositiveInfinity);
                    }
                    else
                    {
                        var w = this.lblText.MeasurementOptions.VisibleSize.X
                                - this.lblTime.Boundary.Right
                                - this.lblLevel.Boundary.Right;
                        this.lblText.Size = new(MatchParent, WrapContent);
                        this.lblText.MeasurementOptions.Size = new(w, float.MaxValue);
                    }

                    this.lblText.ExplicitMeasure();
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

                    this.lblTime.MeasurementOptions.Size = suggestedSize;
                    this.lblLevel.MeasurementOptions.Size = suggestedSize;
                    this.lblText.MeasurementOptions.Size = suggestedSize with { Y = float.PositiveInfinity };

                    this.lblTime.ExplicitMeasure();
                    this.lblLevel.ExplicitMeasure();
                    this.lblText.ExplicitMeasure();
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

        protected override void UpdateTransformationChildren(
            SpannableControlEventArgs args,
            ReadOnlySpan<ISpannableMeasurement> childMeasurements)
        {
            var mcblt = new Vector3(this.MeasuredContentBox.LeftTop, 0);
            switch (this.rowMode)
            {
                case RowMode.OneLine:
                default:
                    this.lblTime.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(mcblt + new Vector3(this.lblTime.MeasuredBoundaryBox.Right, 0, 0)),
                        this.FullTransformation);
                    this.lblText.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                this.lblTime.MeasuredBoundaryBox.Right + this.lblLevel.MeasuredBoundaryBox.Right,
                                0,
                                0)),
                        this.FullTransformation);
                    break;

                case RowMode.TwoLines:
                    this.lblTime.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblText.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(
                                    Math.Max(
                                        this.lblTime.MeasuredBoundaryBox.Bottom,
                                        this.lblLevel.MeasuredBoundaryBox.Bottom) * this.RenderScale) /
                                this.RenderScale,
                                0)),
                        this.FullTransformation);
                    break;

                case RowMode.ThreeLines:
                    this.lblTime.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(mcblt),
                        this.FullTransformation);
                    this.lblLevel.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(this.lblTime.MeasuredBoundaryBox.Bottom * this.RenderScale) /
                                this.RenderScale,
                                0)),
                        this.FullTransformation);
                    this.lblText.ExplicitUpdateTransformation(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(
                                    (this.lblTime.MeasuredBoundaryBox.Bottom +
                                     this.lblLevel.MeasuredBoundaryBox.Bottom) *
                                    this.RenderScale) / this.RenderScale,
                                0)),
                        this.FullTransformation);
                    break;
            }
        }

        protected virtual void OnEntryChange(PropertyChangeEventArgs<ControlSpannable, LogEntry?> args)
        {
            this.EntryChange?.Invoke(args);
            if (args.State != PropertyChangeState.After)
                return;
            if (args.NewValue is null)
            {
                this.lblTime.Text = this.lblLevel.Text = this.lblText.Text = string.Empty;
                this.lblText.SpannableText = null;
                this.Background.Color = 0;
                return;
            }

            this.lblTime.Text = args.NewValue.TimestampString;
            this.lblLevel.Text = GetTextForLogEventLevel(args.NewValue.Level);
            this.UpdateBackground();
            this.UpdateMatches();
        }

        protected virtual void OnHighlightRegexChange(PropertyChangeEventArgs<ControlSpannable, Regex> args)
        {
            this.HighlightRegexChange?.Invoke(args);
            this.UpdateMatches();
        }

        protected virtual void OnSelectedForCopyChange(PropertyChangeEventArgs<ControlSpannable, bool> args)
        {
            this.SelectedForCopyChange?.Invoke(args);
            this.UpdateBackground();
        }

        protected virtual void OnWordBreakChange(
            PropertyChangeEventArgs<ControlSpannable, TextSpannableBase.Options> args)
        {
            this.TextSpannableMeasurementOptionsChange?.Invoke(args);
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

        private void UpdateBackground()
        {
            if (this.entry is null)
                this.Background.Color = 0;
            else if (this.selectedForCopy)
                this.Background.Color = ImGuiColors.ParsedGrey;
            else if (GetColorForLogEventLevel(this.entry.Level) is var color && color != 0)
                this.Background.Color = color;
            else
                this.Background.Color = 0;
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

            var ssb = this.Renderer.RentBuilder();
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
            this.Renderer.ReturnBuilder(ssb);
        }
    }
}
