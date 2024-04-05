using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.Spannables;
using Dalamud.Interface.Spannables.Controls;
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
        private float outerWidth;
        private WordBreakType wordBreak;
        private ISpannableTemplate? wrapMarker;

        private MatchCollection? matches;

        public LogEntryControl()
        {
            this.Children.Add(this.lblTime = new());
            this.Children.Add(this.lblLevel = new());
            this.Children.Add(this.lblText = new());

            this.Size = new(MatchParent, WrapContent);
            this.Padding = new(2);
            this.ClipChildren = false;
        }

        public event PropertyChangeEventHandler<LogEntry?>? EntryChange;

        public event PropertyChangeEventHandler<Regex>? HighlightRegexChange;

        public event PropertyChangeEventHandler<float>? OuterWidthChange;

        public event PropertyChangeEventHandler<WordBreakType>? WordBreakChange;

        public event PropertyChangeEventHandler<ISpannableTemplate>? WrapMarkerChange;

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
            set => this.HandlePropertyChange(
                nameof(this.Entry),
                ref this.entry,
                value,
                this.entry == value,
                this.OnEntryChange);
        }

        public Regex? HighlightRegex
        {
            get => this.highlightRegex;
            set => this.HandlePropertyChange(
                nameof(this.HighlightRegex),
                ref this.highlightRegex,
                value,
                this.highlightRegex == value,
                this.OnHighlightRegexChange);
        }

        public float OuterWidth
        {
            get => this.outerWidth;
            set => this.HandlePropertyChange(
                nameof(this.OuterWidth),
                ref this.outerWidth,
                value,
                this.outerWidth - value == 0f,
                this.OnOuterWidthChange);
        }

        public WordBreakType WordBreak
        {
            get => this.wordBreak;
            set => this.HandlePropertyChange(
                nameof(this.WordBreak),
                ref this.wordBreak,
                value,
                this.wordBreak == value,
                this.OnWordBreakChange);
        }

        public ISpannableTemplate? WrapMarker
        {
            get => this.wrapMarker;
            set => this.HandlePropertyChange(
                nameof(this.WrapMarker),
                ref this.wrapMarker,
                value,
                ReferenceEquals(this.wrapMarker, value),
                this.OnWrapMarkerChange);
        }

        private float EffectiveOuterWidth =>
            this.Parent is ControlSpannable control
                ? (this.outerWidth / this.EffectiveRenderScale)
                  - control.Padding.Width - control.Margin.Width
                  - this.Margin.Width - this.Padding.Width
                : (this.outerWidth / this.EffectiveRenderScale) - this.Margin.Width - this.Padding.Width;

        protected override RectVector4 MeasureChildren(Vector2 suggestedSize)
        {
            this.rowMode = this.EffectiveOuterWidth switch
            {
                >= 640 => RowMode.OneLine,
                >= 120 => RowMode.TwoLines,
                _ => RowMode.ThreeLines,
            };

            var isKeepAll = this.WordBreak == WordBreakType.KeepAll;
            this.Size = new(isKeepAll ? WrapContent : MatchParent, WrapContent);

            switch (this.rowMode)
            {
                case RowMode.OneLine:
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

                    this.lblTime.RenderPassMeasure(new(float.PositiveInfinity));
                    this.lblLevel.RenderPassMeasure(new(float.PositiveInfinity));

                    this.lblText.TextStyle = LogTextStyle;
                    if (isKeepAll)
                    {
                        this.lblText.Size = new(WrapContent);
                        this.lblText.RenderPassMeasure(new(float.PositiveInfinity));
                    }
                    else
                    {
                        var w = this.EffectiveOuterWidth
                                - this.lblTime.MeasuredBoundaryBox.Right
                                - this.lblLevel.MeasuredBoundaryBox.Right;
                        this.lblText.Size = new(w, WrapContent);
                        this.lblText.RenderPassMeasure(new(w, float.PositiveInfinity));
                    }

                    break;

                case RowMode.TwoLines:
                case RowMode.ThreeLines:
                    this.lblTime.TextStyle = MetaLabelStyleTiny;
                    this.lblTime.Size = new(this.EffectiveOuterWidth, WrapContent);
                    this.lblLevel.TextStyle = MetaLabelStyleTiny;
                    this.lblLevel.Size = new(this.EffectiveOuterWidth, WrapContent);
                    this.lblLevel.Margin = BorderVector4.Zero;
                    this.lblLevel.Alignment = new(1, 0);

                    this.lblTime.RenderPassMeasure(suggestedSize);
                    this.lblLevel.RenderPassMeasure(suggestedSize);

                    this.lblText.TextStyle = LogTextStyle;
                    if (isKeepAll)
                    {
                        this.lblText.Size = new(WrapContent);
                        this.lblText.RenderPassMeasure(new(float.PositiveInfinity));
                    }
                    else
                    {
                        this.lblText.Size = new(MatchParent, WrapContent);
                        this.lblText.RenderPassMeasure(suggestedSize with { Y = float.PositiveInfinity });
                    }

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
                        new(
                            Math.Max(timeSize.Width, textSize.Width),
                            Math.Max(timeSize.Bottom, levelSize.Bottom) + textSize.Bottom));
                case RowMode.ThreeLines:
                    return new(
                        Vector2.Zero,
                        new(
                            Math.Max(timeSize.Width, textSize.Width),
                            timeSize.Bottom + levelSize.Bottom + textSize.Bottom));
            }
        }

        protected override void PlaceChildren(SpannableEventArgs args)
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
                                        this.lblLevel.MeasuredBoundaryBox.Bottom) * this.EffectiveRenderScale) /
                                this.EffectiveRenderScale,
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
                                MathF.Round(this.lblTime.MeasuredBoundaryBox.Bottom * this.EffectiveRenderScale) /
                                this.EffectiveRenderScale,
                                0)),
                        this.FullTransformation);
                    this.lblText.RenderPassPlace(
                        Matrix4x4.CreateTranslation(
                            mcblt + new Vector3(
                                0,
                                MathF.Round(
                                    (this.lblTime.MeasuredBoundaryBox.Bottom +
                                     this.lblLevel.MeasuredBoundaryBox.Bottom) *
                                    this.EffectiveRenderScale) / this.EffectiveRenderScale,
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
                this.lblTime.SpannableText = this.lblLevel.SpannableText = this.lblText.SpannableText = null;
                return;
            }

            var s = new StyledText(args.NewValue.TimestampString).CreateSpannable();
            s.WordBreak = WordBreakType.BreakWord;
            this.lblTime.SpannableText = s;

            s = new StyledText(GetTextForLogEventLevel(args.NewValue.Level)).CreateSpannable();
            s.WordBreak = WordBreakType.BreakWord;
            this.lblLevel.SpannableText = s;
            this.UpdateMatches();
        }

        protected virtual void OnHighlightRegexChange(PropertyChangeEventArgs<Regex> args)
        {
            this.HighlightRegexChange?.Invoke(args);
            this.UpdateMatches();
        }

        protected virtual void OnOuterWidthChange(PropertyChangeEventArgs<float> args) =>
            this.OuterWidthChange?.Invoke(args);

        protected virtual void OnWordBreakChange(PropertyChangeEventArgs<WordBreakType> args)
        {
            this.WordBreakChange?.Invoke(args);
            if (this.lblText.SpannableText is StyledTextSpannable ts)
                ts.WordBreak = args.NewValue;
        }

        protected virtual void OnWrapMarkerChange(PropertyChangeEventArgs<ISpannableTemplate> args)
        {
            this.WrapMarkerChange?.Invoke(args);
            if (this.lblText.SpannableText is StyledTextSpannable ts)
                ts.WrapMarker = args.NewValue;
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

            var ssb = this.Renderer.RentBuilder();
            if (this.matches is null)
            {
                ssb.Append(this.entry.Line);
            }
            else
            {
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
            }

            var s = ssb.Build().CreateSpannable();
            s.WordBreak = this.wordBreak;
            s.WrapMarker = this.wrapMarker;
            s.Style = s.Style with { EdgeWidth = 1f };
            s.DisplayControlCharacters = true;
            s.ControlCharactersStyle = new()
            {
                Font = new(DalamudAssetFontAndFamilyId.From(DalamudAsset.InconsolataRegular)),
                BackColor = 0xFF333333,
                EdgeWidth = 1,
                ForeColor = 0xFFFFFFFF,
                FontSize = -0.6f,
                VerticalAlignment = 0.5f,
            };
            this.lblText.SpannableText = s;
            this.Renderer.ReturnBuilder(ssb);
        }
    }
}
