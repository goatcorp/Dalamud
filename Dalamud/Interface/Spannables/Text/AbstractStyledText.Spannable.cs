using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.EventHandlers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="StyledText"/> and <see cref="StyledTextBuilder"/>.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "Stfu")]
public abstract partial class AbstractStyledText
{
    private static readonly ObjectPool<TextSpannable> MyMeasurementPool =
        new DefaultObjectPool<TextSpannable>(new DefaultPooledObjectPolicy<TextSpannable>());

    /// <summary>Describes states of links.</summary>
    public enum LinkState
    {
        /// <summary>The link is in normal state.</summary>
        Clear,

        /// <summary>The link is hovered.</summary>
        Hovered,

        /// <summary>The link is active.</summary>
        Active,

        /// <summary>The link has been clicked.</summary>
        ActiveNotHovered,
    }

    /// <inheritdoc cref="ISpannableTemplate.CreateSpannable"/>
    public TextSpannable CreateSpannable()
    {
        var t = MyMeasurementPool.Get();
        t.TryReset();
        t.BindTo(this);
        return t;
    }

    /// <inheritdoc/>
    Spannable ISpannableTemplate.CreateSpannable() => this.CreateSpannable();

    /// <inheritdoc/>
    public void RecycleSpannable(Spannable? spannable)
    {
        if (spannable is TextSpannable mm)
            MyMeasurementPool.Return(mm);
    }

    /// <summary>Options for <see cref="TextSpannable"/>.</summary>
    public sealed class Options : SpannableOptions
    {
        private bool displayControlCharacters;
        private WordBreakType wordBreak;
        private NewLineType acceptedNewLines;
        private float tabWidth;
        private float verticalAlignment;
        private int gfdIconMode;
        private TextStyle controlCharactersStyle;
        private TextStyle style;
        private ISpannableTemplate? wrapMarker;

        /// <summary>Initializes a new instance of the <see cref="Options"/> class.</summary>
        public Options() => this.TryReset();

        /// <summary>Gets or sets a value indicating whether to display representations of control characters, such as
        /// CR, LF, NBSP, and SHY.</summary>
        public bool DisplayControlCharacters
        {
            get => this.displayControlCharacters;
            set => this.UpdateProperty(
                nameof(this.DisplayControlCharacters),
                ref this.displayControlCharacters,
                value,
                this.displayControlCharacters == value);
        }

        /// <summary>Gets or sets how to handle word break mode.</summary>
        public WordBreakType WordBreak
        {
            get => this.wordBreak;
            set => this.UpdateProperty(nameof(this.WordBreak), ref this.wordBreak, value, this.wordBreak == value);
        }

        /// <summary>Gets or sets the type of new line sequences to handle.</summary>
        public NewLineType AcceptedNewLines
        {
            get => this.acceptedNewLines;
            set => this.UpdateProperty(
                nameof(this.AcceptedNewLines),
                ref this.acceptedNewLines,
                value,
                this.acceptedNewLines == value);
        }

        /// <summary>Gets or sets the tab size.</summary>
        /// <value><ul>
        /// <li><c>0</c> will treat tab characters as a whitespace character.</li>
        /// <li><b>Positive values</b> indicate the width in pixels.</li>
        /// <li><b>Negative values</b> indicate the width in the number of whitespace characters, multiplied by -1.</li>
        /// </ul></value>
        public float TabWidth
        {
            get => this.tabWidth;
            set => this.UpdateProperty(nameof(this.TabWidth), ref this.tabWidth, value, this.tabWidth - value == 0f);
        }

        /// <summary>Gets or sets the vertical alignment, with respect to the measured vertical boundary.</summary>
        /// <value><ul>
        /// <li><c>0.0</c> will align to top.</li>
        /// <li><c>0.5</c> will align to center.</li>
        /// <li><c>1.0</c> will align to right.</li>
        /// <li>Values outside the range of [0, 1] will be clamped.</li>
        /// </ul></value>
        /// <remarks>Does nothing if no (infinite) vertical boundary is set.</remarks>
        public float VerticalAlignment
        {
            get => this.verticalAlignment;
            set => this.UpdateProperty(
                nameof(this.VerticalAlignment),
                ref this.verticalAlignment,
                value,
                this.verticalAlignment - value == 0f);
        }

        /// <summary>Gets or sets the graphic font icon mode.</summary>
        /// <remarks>A value outside the suported range will use the one configured from the game configuration via
        /// game controller configuration.</remarks>
        public int GfdIconMode
        {
            get => this.gfdIconMode;
            set => this.UpdateProperty(
                nameof(this.GfdIconMode),
                ref this.gfdIconMode,
                value,
                this.gfdIconMode == value);
        }

        /// <summary>Gets or sets the text style for the control characters.</summary>
        /// <remarks>Does nothing if <see cref="DisplayControlCharacters"/> is <c>false</c>.</remarks>
        public TextStyle ControlCharactersStyle
        {
            get => this.controlCharactersStyle;
            set => this.UpdateProperty(
                nameof(this.ControlCharactersStyle),
                ref this.controlCharactersStyle,
                value,
                TextStyle.PropertyReferenceEquals(this.controlCharactersStyle, value));
        }

        /// <summary>Gets or sets the initial text style.</summary>
        /// <remarks>Text styles may be altered in middle of the text, but this property will not change.
        /// Use <see cref="TextSpannable.LastStyle"/> for that information.</remarks>
        public TextStyle Style
        {
            get => this.style;
            set => this.UpdateProperty(
                nameof(this.Style),
                ref this.style,
                value,
                TextStyle.PropertyReferenceEquals(this.style, value));
        }

        /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
        /// <value><c>null</c> indicates that wrap markers are disabled.</value>
        public ISpannableTemplate? WrapMarker
        {
            get => this.wrapMarker;
            set => this.UpdateProperty(
                nameof(this.WrapMarker),
                ref this.wrapMarker,
                value,
                ReferenceEquals(this.wrapMarker, value));
        }

        /// <inheritdoc/>
        public override void CopyFrom(SpannableOptions source)
        {
            if (source is Options mo)
                this.CopyFrom(mo);
            else
                base.CopyFrom(source);
        }

        /// <inheritdoc cref="SpannableOptions.CopyFrom"/>
        public void CopyFrom(Options source)
        {
            this.DisplayControlCharacters = source.DisplayControlCharacters;
            this.WordBreak = source.WordBreak;
            this.AcceptedNewLines = source.AcceptedNewLines;
            this.TabWidth = source.TabWidth;
            this.VerticalAlignment = source.VerticalAlignment;
            this.GfdIconMode = source.GfdIconMode;
            this.ControlCharactersStyle = source.ControlCharactersStyle;
            this.Style = source.Style;
            this.WrapMarker = source.WrapMarker;
            base.CopyFrom(source);
        }

        /// <inheritdoc/>
        public override bool TryReset()
        {
            this.DisplayControlCharacters = false;
            this.WordBreak = WordBreakType.Normal;
            this.AcceptedNewLines = NewLineType.All;
            this.TabWidth = -4;
            this.VerticalAlignment = 0f;
            this.GfdIconMode = -1;
            this.ControlCharactersStyle = default;
            this.Style = default;
            this.WrapMarker = null;
            return base.TryReset();
        }
    }

    /// <summary>Measurement for <see cref="StyledText"/> and <see cref="StyledTextBuilder"/>.</summary>
    [SuppressMessage("ReSharper", "ConvertToAutoProperty", Justification = "WIP")]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter", Justification = "WIP")]
    public sealed class TextSpannable : Spannable<Options>, IResettable
    {
        private readonly List<BoundaryToRecord> linkBoundaries = [];
        private readonly List<MeasuredLine> lines = [];
        private readonly List<ISpannableTemplate?> childrenTemplate = [];
        private readonly List<Spannable?> children = [];
        private readonly List<Vector2> childOffsets = [];

        private int childCount;
        private TextStyle lastStyle;
        private Vector2 lastOffset;

        private float shiftFromVerticalAlignment;

        private int interactedLinkIndex;
        private LinkState interactedLinkState;

        /// <summary>Initializes a new instance of the <see cref="TextSpannable"/> class.</summary>
        public TextSpannable()
        {
            this.TryReset();
        }

        /// <summary>Occurs when the mouse pointer enters a link in the control.</summary>
        public event SpannableMouseLinkEventHandler? LinkMouseEnter;

        /// <summary>Occurs when the mouse pointer leaves a link in the control.</summary>
        public event SpannableMouseLinkEventHandler? LinkMouseLeave;

        /// <summary>Occurs when a link in the control just got held down.</summary>
        public event SpannableMouseLinkEventHandler? LinkMouseDown;

        /// <summary>Occurs when a link in the control just got released.</summary>
        public event SpannableMouseLinkEventHandler? LinkMouseUp;

        /// <summary>Occurs when a link in the control is clicked by the mouse.</summary>
        public event SpannableMouseLinkEventHandler? LinkMouseClick;

        /// <summary>Gets the source instance of <see cref="AbstractStyledText"/>.</summary>
        public new AbstractStyledText? SourceTemplate
        {
            get => (AbstractStyledText?)base.SourceTemplate;
            private set => base.SourceTemplate = value;
        }

        /// <summary>Gets a reference to the last text style used.</summary>
        public ref TextStyle LastStyle => ref this.lastStyle;

        /// <summary>Gets a reference to the last cursor offset.</summary>
        public ref Vector2 LastOffset => ref this.lastOffset;

        /// <summary>Gets the number of lines.</summary>
        public int LineCount => this.lines.Count;

        /// <summary>Gets the span of measurements of inner spannables.</summary>
        public Span<Spannable?> Children =>
            CollectionsMarshal.AsSpan(this.children)[..this.childCount];

        /// <summary>Gets the span of offsets of inner spannables.</summary>
        public Span<Vector2> ChildOffsets =>
            CollectionsMarshal.AsSpan(this.childOffsets)[..this.childCount];

        /// <summary>Gets the measured lines so far.</summary>
        private Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.lines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        private Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.linkBoundaries);

        /// <inheritdoc/>
        public bool TryReset()
        {
            for (var i = 0; i < this.childCount; i++)
            {
                this.childrenTemplate[i]?.RecycleSpannable(this.children[i]);
                this.childrenTemplate[i] = null;
                this.children[i] = null;
            }

            this.ImGuiGlobalId = 0u;
            this.Options.TryReset();
            this.SourceTemplate = null;
            this.childCount = 0;
            this.interactedLinkIndex = -1;
            this.interactedLinkState = LinkState.Clear;
            this.LinkMouseEnter = null;
            this.LinkMouseLeave = null;
            this.LinkMouseDown = null;
            this.LinkMouseUp = null;
            this.LinkMouseClick = null;
            this.ClearMeasurement();
            this.RequestMeasure();
            return true;
        }

        /// <summary>Clears measured data.</summary>
        public void ClearMeasurement()
        {
            this.Boundary = RectVector4.InvertedExtrema;
            this.linkBoundaries.Clear();
            this.lines.Clear();
            this.lastStyle = this.Options.Style;
            this.lastOffset = Vector2.Zero;
        }

        /// <summary>Binds this spannable to the specified styled text.</summary>
        /// <param name="text">The text to attach.</param>
        public void BindTo(AbstractStyledText text)
        {
            this.SourceTemplate = text;

            var data = text.GetData();
            this.childCount = data.Children.Length;
            this.children.EnsureCapacity(this.childCount);
            this.childrenTemplate.EnsureCapacity(this.childCount);
            this.childOffsets.EnsureCapacity(this.childCount);
            while (this.children.Count < this.childCount)
            {
                this.children.Add(null);
                this.childrenTemplate.Add(null);
                this.childOffsets.Add(default);
            }

            for (var i = 0; i < this.childCount; i++)
            {
                if (data.Children[i]?.CreateSpannable() is not { } cs)
                    continue;

                cs.Renderer = this.Renderer;
                this.children[i] = cs;
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseDown(SpannableMouseEventArgs args)
        {
            base.OnMouseDown(args);
            if (args.SuppressHandling
                || args.Step == SpannableEventStep.BeforeChildren
                || this.SourceTemplate is null
                || this.interactedLinkIndex == -1
                || this.interactedLinkState is LinkState.Clear)
                return;

            if (!this.SourceTemplate.GetData().TryGetLinkAt(this.interactedLinkIndex, out var linkData))
                return;

            var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
            e.Initialize(this, SpannableEventStep.DirectTarget);
            e.InitializeMouseLinkEvent(linkData.ToArray(), args.Button);
            this.OnLinkMouseDown(e);
            if (e.SuppressHandling)
            {
                SpannableEventArgsPool.Return(e);
                return;
            }

            SpannableEventArgsPool.Return(e);
            this.interactedLinkState = LinkState.Active;
            this.CaptureMouse = true;
            args.SuppressHandling = true;
        }

        /// <inheritdoc/>
        protected override void OnMouseMove(SpannableMouseEventArgs args)
        {
            base.OnMouseMove(args);
            if (args.SuppressHandling
                || args.Step == SpannableEventStep.BeforeChildren
                || this.SourceTemplate is null)
                return;

            var data = this.SourceTemplate.GetData();
            var linkIndex = -1;

            if (this.IsMouseHovered)
            {
                foreach (ref var entry in this.LinkBoundaries)
                {
                    if (entry.Boundary.Contains(args.LocalLocation))
                    {
                        if (data.TryGetLinkAt(entry.RecordIndex, out _))
                            linkIndex = entry.RecordIndex;

                        break;
                    }
                }
            }

            if (!this.SourceTemplate.GetData().TryGetLinkAt(this.interactedLinkIndex, out var prevData))
                prevData = default;
            if (!this.SourceTemplate.GetData().TryGetLinkAt(linkIndex, out var currData))
                currData = default;

            var prev = (this.interactedLinkIndex, this.interactedLinkState);
            if (this.interactedLinkState is LinkState.Clear or LinkState.Hovered)
            {
                this.interactedLinkIndex = linkIndex;
                this.interactedLinkState = linkIndex == -1 ? LinkState.Clear : LinkState.Hovered;
            }
            else
            {
                this.interactedLinkState =
                    this.interactedLinkIndex == linkIndex ? LinkState.Active : LinkState.ActiveNotHovered;
            }

            if (prev != (this.interactedLinkIndex, this.interactedLinkState))
            {
                var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
                e.Initialize(this, SpannableEventStep.DirectTarget);
                switch (this.interactedLinkState)
                {
                    case LinkState.Active when currData != default:
                        e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                        this.OnLinkMouseEnter(e);
                        break;
                    case LinkState.ActiveNotHovered when currData != default:
                        e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                        this.OnLinkMouseLeave(e);
                        break;
                    case LinkState.Clear when prevData != default:
                        e.InitializeMouseLinkEvent(prevData.ToArray(), args.Button);
                        this.OnLinkMouseLeave(e);
                        break;
                    case LinkState.Hovered:
                        if (prevData != default)
                        {
                            e.InitializeMouseLinkEvent(prevData.ToArray(), args.Button);
                            this.OnLinkMouseLeave(e);
                            
                            e.Initialize(this, SpannableEventStep.DirectTarget);
                        }

                        if (currData != default)
                        {
                            e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                            this.OnLinkMouseEnter(e);
                        }

                        break;
                }
                
                SpannableEventArgsPool.Return(e);
            }
        }

        /// <inheritdoc/>
        protected override void OnMouseUp(SpannableMouseEventArgs args)
        {
            base.OnMouseUp(args);
            if (args.Step == SpannableEventStep.BeforeChildren)
                return;

            if (this.interactedLinkIndex != -1 && this.SourceTemplate is not null && !args.SuppressHandling)
            {
                if (!this.SourceTemplate.GetData().TryGetLinkAt(this.interactedLinkIndex, out var currData))
                    currData = default;
                
                var e = SpannableEventArgsPool.Rent<SpannableMouseLinkEventArgs>();
                e.Initialize(this, SpannableEventStep.DirectTarget);
                e.InitializeMouseLinkEvent(currData.ToArray(), args.Button);
                this.OnLinkMouseUp(e);

                if (!e.SuppressHandling && this.interactedLinkState is LinkState.Active)
                {
                    e.Initialize(this, SpannableEventStep.DirectTarget);
                    this.OnLinkMouseClick(e);
                }
                    
                SpannableEventArgsPool.Return(e);
                
                if (this.interactedLinkState is LinkState.ActiveNotHovered or LinkState.Clear)
                    this.interactedLinkIndex = -1;
                this.interactedLinkState =
                    this.interactedLinkState is LinkState.Active ? LinkState.Hovered : LinkState.Clear;
                args.SuppressHandling = true;
            }

            this.CaptureMouse = false;
        }

        /// <inheritdoc/>
        protected override void OnMeasure(SpannableEventArgs args)
        {
            base.OnMeasure(args);

            if (this.SourceTemplate is not { } tsb)
                return;

            this.ClearMeasurement();

            var boundary = RectVector4.InvertedExtrema;

            var data = tsb.GetData();
            var segment = new DataRef.Segment(data, 0, 0);
            var linkRecordIndex = -1;

            var charRenderer = new CharRenderer(this, data, default);
            var skipNextLine = false;
            while (true)
            {
                // Find the first line break point, only taking word wrapping into account.
                // this.FindFirstWordWrapByteOffset(args, segment, new(segment), out var line);
                var line = MeasuredLine.Empty;
                {
                    var testSegment = segment;
                    var wordBreaker = new WordBreaker(this, data);
                    var startOffset = new CompositeOffset(testSegment);
                    do
                    {
                        if (testSegment.TryGetRawText(out var rawText))
                        {
                            foreach (var c in rawText[(startOffset.Text - testSegment.Offset.Text)..]
                                         .EnumerateUtf(UtfEnumeratorFlags.Utf8))
                            {
                                var currentOffset = new CompositeOffset(
                                    startOffset.Text + c.ByteOffset,
                                    testSegment.Offset.Record);
                                var nextOffset = currentOffset.AddTextOffset(c.ByteLength);

                                var pad = 0f;
                                if (this.Options.DisplayControlCharacters &&
                                    c.Value.ShortName is { IsEmpty: false } name)
                                {
                                    var ssb = this.Renderer.RentBuilder();
                                    ssb.Clear().Append(name);

                                    var ccm = ssb.CreateSpannable();
                                    ccm.Renderer = this.Renderer;
                                    ccm.Options.RenderScale = this.Options.RenderScale;
                                    ccm.Options.ControlCharactersStyle = this.Options.ControlCharactersStyle;
                                    ccm.RenderPassMeasure();

                                    if (ccm.Boundary.IsValid)
                                    {
                                        pad = MathF.Ceiling(ccm.Boundary.Width * this.Options.RenderScale) /
                                              this.Options.RenderScale;
                                        wordBreaker.ResetLastChar();
                                    }

                                    ssb.RecycleSpannable(ccm);
                                    this.Renderer.ReturnBuilder(ssb);
                                }

                                switch (c.Value.IntValue)
                                {
                                    case '\r'
                                        when testSegment.Data.TryGetCodepointAt(
                                                 nextOffset.Text,
                                                 0,
                                                 out var nextCodepoint)
                                             && nextCodepoint == '\n'
                                             && (this.Options.AcceptedNewLines & NewLineType.CrLf) != 0:
                                        line = wordBreaker.Last;
                                        line.SetOffset(nextOffset.AddTextOffset(1), this.Options.RenderScale, pad);
                                        line.HasNewLineAtEnd = true;
                                        wordBreaker.UnionLineBBoxVertical(ref line);
                                        break;

                                    case '\r' when (this.Options.AcceptedNewLines & NewLineType.Cr) != 0:
                                    case '\n' when (this.Options.AcceptedNewLines & NewLineType.Lf) != 0:
                                        line = wordBreaker.Last;
                                        line.SetOffset(nextOffset, this.Options.RenderScale, pad);
                                        line.HasNewLineAtEnd = true;
                                        wordBreaker.UnionLineBBoxVertical(ref line);
                                        break;

                                    case '\r' or '\n':
                                        line = wordBreaker.AddCodepointAndMeasure(
                                            currentOffset,
                                            nextOffset,
                                            -1,
                                            pad: pad);
                                        break;

                                    default:
                                        line = wordBreaker.AddCodepointAndMeasure(
                                            currentOffset,
                                            nextOffset,
                                            c.EffectiveChar,
                                            pad: pad);
                                        break;
                                }

                                if (!line.IsEmpty)
                                    break;
                            }

                            startOffset = new(testSegment.Offset.Text + rawText.Length, testSegment.Offset.Record);
                        }
                        else if (testSegment.TryGetRecord(out var record, out var recordData))
                        {
                            line = wordBreaker.HandleSpan(record, recordData, new(testSegment), new(testSegment, 0, 1));
                        }
                    }
                    while (line.IsEmpty && testSegment.TryGetNext(out testSegment));

                    if (line.IsEmpty)
                    {
                        line = wordBreaker.Last;
                        line.SetOffset(
                            new(testSegment.Offset.Text, testSegment.Offset.Record),
                            this.Options.RenderScale,
                            0f);
                    }
                }

                line.FirstOffset = new(segment);
                var lineSegment = new DataRef.Segment(data, line.Offset.Text, line.Offset.Record);

                // If wrapped, then omit the ending whitespaces.
                if (line.IsWrapped)
                {
                    var searchTarget = data.TextStream[segment.Offset.Text..line.Offset.Text];
                    var nbTrim = 0;
                    var minOffset = line.Offset.Text;
                    var prevSegment = lineSegment;
                    while (prevSegment.TryGetPrevious(out prevSegment))
                    {
                        if (prevSegment.TryGetRawText(out var text))
                            minOffset -= text.Length;
                        else if (!prevSegment.TryGetRecord(out var prevRec, out _) || prevRec.Type.IsObject())
                            break;
                    }

                    while (!searchTarget.IsEmpty)
                    {
                        if (!UtfValue.TryDecode8(searchTarget[^1..], out var v, out var len)
                            && !UtfValue.TryDecode8(searchTarget[^2..], out v, out len)
                            && !UtfValue.TryDecode8(searchTarget[^3..], out v, out len)
                            && !UtfValue.TryDecode8(searchTarget[^4..], out v, out len))
                            break;
                        if (!v.TryGetRune(out var rune))
                            break;
                        if (!Rune.IsWhiteSpace(rune))
                            break;
                        if (line.Offset.Text - nbTrim - len < minOffset)
                            break;
                        nbTrim += len;
                        searchTarget = searchTarget[..^len];
                    }

                    line.OmitOffset = line.Offset.AddTextOffset(-nbTrim);
                }
                else
                {
                    line.OmitOffset = line.Offset;
                }

                if (!skipNextLine)
                {
                    this.lines.Add(line);
                    charRenderer.SetLine(line);

                    var accumulatedBoundary = RectVector4.InvertedExtrema;

                    for (var seg = segment; seg.Offset < line.Offset;)
                    {
                        if (seg.TryGetRawText(out var rawText))
                        {
                            foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                            {
                                var absOffset = new CompositeOffset(seg, c.ByteOffset);
                                if (absOffset.Text < seg.Offset.Text)
                                    continue;
                                if (absOffset.Text >= line.OmitOffset.Text)
                                    break;

                                if (this.Options.DisplayControlCharacters)
                                {
                                    var name = c.Value.ShortName;
                                    if (!name.IsEmpty)
                                    {
                                        var offset = charRenderer.StyleTranslation;
                                        this.lastOffset += offset;
                                        var old = charRenderer.UpdateSpanParams(this.Options.ControlCharactersStyle);
                                        charRenderer.LastRendered.Clear();
                                        foreach (var c2 in name)
                                            charRenderer.RenderOne(c2);
                                        charRenderer.LastRendered.Clear();
                                        _ = charRenderer.UpdateSpanParams(old);
                                        this.lastOffset -= offset;
                                    }
                                }

                                accumulatedBoundary = RectVector4.Union(
                                    accumulatedBoundary,
                                    charRenderer.RenderOne(c.EffectiveChar));
                                charRenderer.LastRendered.SetCodepoint(c.Value);
                            }
                        }
                        else if (seg.TryGetRecord(out var record, out var recordData))
                        {
                            switch (record.Type)
                            {
                                case SpannedRecordType.Link when record.IsRevert:
                                    this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                                    linkRecordIndex = -1;
                                    break;

                                case SpannedRecordType.Link
                                    when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                                    this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                                    linkRecordIndex = record.IsRevert || link.IsEmpty ? -1 : seg.Offset.Record;
                                    break;
                            }

                            accumulatedBoundary = RectVector4.Union(
                                accumulatedBoundary,
                                charRenderer.HandleSpan(record, recordData));
                        }

                        if (!seg.TryGetNext(out seg))
                            break;
                    }

                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        this.ProcessPostLine(line, ref charRenderer, default));

                    this.UpdateAndResetBoundary(ref boundary, ref accumulatedBoundary, linkRecordIndex);
                    this.ExtendBoundaryDownward(ref boundary, this.lastOffset.Y + charRenderer.MostRecentLineHeight);
                }
                else
                {
                    for (var seg = segment; seg.Offset < line.OmitOffset;)
                    {
                        if (seg.TryGetRecord(out var record, out var recordData))
                        {
                            charRenderer.HandleSpan(record, recordData);
                        }

                        if (!seg.TryGetNext(out seg))
                            break;
                    }

                    if (line.HasNewLineAtEnd ||
                        (line.IsWrapped && this.Options.WordBreak != WordBreakType.KeepAll))
                        this.AddLineBreak(line);
                }

                if (lineSegment.Offset == data.EndOffset)
                    break;
                segment = lineSegment;
                if (this.Options.WordBreak == WordBreakType.KeepAll)
                {
                    if (skipNextLine && !line.IsWrapped)
                        this.MeasuredLines[^1].HasNewLineAtEnd = true;
                    skipNextLine = line.IsWrapped;
                }
            }

            this.ExtendBoundaryDownward(ref boundary, this.lastOffset.Y);

            if (!boundary.IsValid)
                boundary = default;
            else
                boundary.Right += 1;

            // if (this.Options.Size.X < float.PositiveInfinity)
            //     this.boundary.Right = this.Options.Size.X;
            // if (this.Options.Size.Y < float.PositiveInfinity)
            //     this.boundary.Bottom = this.Options.Size.Y;

            if (this.Options.VerticalAlignment > 0f && this.Options.PreferredSize.Y < float.PositiveInfinity)
            {
                var offset =
                    MathF.Round(
                        (this.Options.PreferredSize.Y - boundary.Height) *
                        Math.Clamp(this.Options.VerticalAlignment, 0f, 1f) *
                        this.Options.RenderScale) /
                    this.Options.RenderScale;
                this.TranslateSubBoundaries(ref boundary, new(0, offset), data);
                this.shiftFromVerticalAlignment = offset;
            }
            else
            {
                this.shiftFromVerticalAlignment = 0;
            }

            this.Boundary = boundary;
        }

        /// <inheritdoc/>
        protected override void OnPlace(SpannableEventArgs args)
        {
            base.OnPlace(args);

            var o = this.ChildOffsets;
            var c = this.Children;
            for (var i = 0; i < this.childCount; i++)
                c[i]?.RenderPassPlace(Matrix4x4.CreateTranslation(new(o[i], 0)), this.FullTransformation);
        }

        /// <inheritdoc/>
        protected override unsafe void OnDraw(SpannableDrawEventArgs args)
        {
            base.OnDraw(args);
            if (this.SourceTemplate is not { } tsb)
                return;

            var data = tsb.GetData();

            this.lastOffset = new(0, this.shiftFromVerticalAlignment);
            this.lastStyle = this.Options.Style;

            var charRenderer = new CharRenderer(this, data, args.DrawListPtr);
            try
            {
                var segment = new DataRef.Segment(data, 0, 0);
                foreach (ref readonly var line in this.MeasuredLines)
                {
                    charRenderer.SetLine(line);

                    while (segment.Offset < line.Offset)
                    {
                        if (segment.TryGetRawText(out var rawText))
                        {
                            var lineHasMoreText = true;
                            foreach (var c in rawText.EnumerateUtf(UtfEnumeratorFlags.Utf8))
                            {
                                var absOffset = new CompositeOffset(segment, c.ByteOffset);
                                if (absOffset < line.FirstOffset)
                                    continue;
                                if (absOffset >= line.Offset)
                                {
                                    lineHasMoreText = false;
                                    break;
                                }

                                if (absOffset < line.OmitOffset)
                                {
                                    if (this.Options.DisplayControlCharacters)
                                    {
                                        var name = c.Value.ShortName;
                                        if (!name.IsEmpty)
                                        {
                                            var offset = charRenderer.StyleTranslation;
                                            this.lastOffset += offset;
                                            var old = charRenderer.UpdateSpanParams(
                                                this.Options.ControlCharactersStyle);
                                            charRenderer.LastRendered.Clear();
                                            foreach (var c2 in name)
                                                charRenderer.RenderOne(c2);
                                            charRenderer.LastRendered.Clear();
                                            _ = charRenderer.UpdateSpanParams(old);
                                            this.lastOffset -= offset;
                                        }
                                    }

                                    charRenderer.RenderOne(c.EffectiveChar);
                                }

                                charRenderer.LastRendered.SetCodepoint(c.Value);
                            }

                            if (!lineHasMoreText)
                                break;
                        }
                        else if (segment.TryGetRecord(out var record, out var recordData))
                        {
                            if (!record.Type.IsObject() || segment.Offset >= line.FirstOffset)
                            {
                                charRenderer.HandleSpan(record, recordData);
                            }
                        }

                        if (!segment.TryGetNext(out segment))
                            break;
                    }

                    // TODO: render to correct place?
                    this.ProcessPostLine(line, ref charRenderer, args.DrawListPtr);
                }

                foreach (var entry in this.LinkBoundaries)
                {
                    if (entry.RecordIndex != this.interactedLinkIndex)
                        continue;

                    var color = this.interactedLinkState switch
                    {
                        LinkState.Hovered => ImGui.GetColorU32(ImGuiCol.ButtonHovered),
                        LinkState.Active => ImGui.GetColorU32(ImGuiCol.ButtonActive),
                        LinkState.Clear => 0u,
                        _ => 0u,
                    };
                    if (color == 0u)
                        continue;

                    ImGuiNative.ImDrawList_AddRectFilled(
                        charRenderer.BackChannel,
                        entry.Boundary.LeftTop,
                        entry.Boundary.RightBottom,
                        color,
                        0f,
                        ImDrawFlags.None);
                }
            }
            finally
            {
                charRenderer.AppendAndReturnChannels(this.LocalTransformation);
            }
        }

        /// <summary>Raises the <see cref="LinkMouseEnter"/> event.</summary>
        /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
        private void OnLinkMouseEnter(SpannableMouseLinkEventArgs args) =>
            this.LinkMouseEnter?.Invoke(args);

        /// <summary>Raises the <see cref="LinkMouseLeave"/> event.</summary>
        /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
        private void OnLinkMouseLeave(SpannableMouseLinkEventArgs args) =>
            this.LinkMouseLeave?.Invoke(args);

        /// <summary>Raises the <see cref="LinkMouseDown"/> event.</summary>
        /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
        private void OnLinkMouseDown(SpannableMouseLinkEventArgs args) =>
            this.LinkMouseDown?.Invoke(args);

        /// <summary>Raises the <see cref="LinkMouseUp"/> event.</summary>
        /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
        private void OnLinkMouseUp(SpannableMouseLinkEventArgs args) =>
            this.LinkMouseUp?.Invoke(args);

        /// <summary>Raises the <see cref="LinkMouseClick"/> event.</summary>
        /// <param name="args">A <see cref="SpannableMouseLinkEventArgs"/> that contains the event data.</param>
        private void OnLinkMouseClick(SpannableMouseLinkEventArgs args) =>
            this.LinkMouseClick?.Invoke(args);

        /// <summary>Extends the bottom boundary by given amount.</summary>
        /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
        /// <param name="b">The amount to extend.</param>
        private void ExtendBoundaryDownward(ref RectVector4 boundary, float b)
        {
            if (boundary.IsValid)
                boundary.Bottom = Math.Max(boundary.Bottom, b);
        }

        /// <summary>Adds a line break.</summary>
        /// <param name="lineBefore">The line that came right before this line break.</param>
        private void AddLineBreak(in MeasuredLine lineBefore) =>
            this.lastOffset = new(
                0,
                MathF.Round((this.lastOffset.Y + lineBefore.Height) * this.Options.RenderScale) /
                this.Options.RenderScale);

        /// <summary>Add decorations and breaks line once a line ends.</summary>
        /// <param name="line">The line that came right before this.</param>
        /// <param name="charRenderer">The renderer.</param>
        /// <param name="drawListPtr">The draw list to draw to.</param>
        /// <returns>The additional region to add to boundary, from adding decorations.</returns>
        private unsafe RectVector4 ProcessPostLine(
            in MeasuredLine line,
            ref CharRenderer charRenderer,
            ImDrawListPtr drawListPtr)
        {
            var accumulatedBoundary = RectVector4.InvertedExtrema;
            if (line.IsWrapped)
            {
                if (line.LastThing.IsCodepoint(0x00AD) && this.Options.WordBreak != WordBreakType.KeepAll)
                {
                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        charRenderer.RenderOne(SoftHyphenReplacementChar));
                }

                if (this.Options.WrapMarker is { } wm)
                {
                    var wmm = wm.CreateSpannable();
                    wmm.Renderer = this.Renderer;
                    wmm.Options.RenderScale = this.Options.RenderScale;
                    wmm.RenderPassMeasure();

                    if (wmm.Boundary.IsValid)
                    {
                        if (drawListPtr.NativePtr is not null && this.Renderer is not null)
                        {
                            var wmLocalTransformation = Matrix4x4.CreateTranslation(
                                new(this.lastOffset + charRenderer.StyleTranslation, 0));
                            if (this.lastStyle.Italic)
                            {
                                wmLocalTransformation = Matrix4x4.Multiply(
                                    wmLocalTransformation,
                                    new Matrix4x4(
                                        Matrix3x2.CreateSkew(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0)));
                            }

                            wmm.RenderPassPlace(wmLocalTransformation, this.FullTransformation);

                            var tmpDrawList = this.Renderer.RentDrawList(drawListPtr);
                            try
                            {
                                wmm.RenderPassDraw(tmpDrawList);
                                tmpDrawList.CopyDrawListDataTo(drawListPtr, this.LocalTransformation, Vector4.One);
                            }
                            finally
                            {
                                this.Renderer.ReturnDrawList(tmpDrawList);
                            }
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            RectVector4.Translate(
                                wmm.Boundary,
                                this.lastOffset + charRenderer.StyleTranslation));
                        this.lastOffset.X += wmm.Boundary.Right;
                        charRenderer.LastRendered.Clear();
                    }

                    wm.RecycleSpannable(wmm);
                }
            }

            if (line.HasNewLineAtEnd || (line.IsWrapped && this.Options.WordBreak != WordBreakType.KeepAll))
                this.AddLineBreak(line);
            return accumulatedBoundary;
        }

        /// <summary>Updates <see cref="Spannable.Boundary"/> and <see cref="linkBoundaries"/>, and resets
        /// <paramref name="accumulator"/>.</summary>
        /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
        /// <param name="accumulator">Mutalbe reference to the temporary accumulator.</param>
        /// <param name="linkRecordIndex">The link record index.</param>
        private void UpdateAndResetBoundary(ref RectVector4 boundary, ref RectVector4 accumulator, int linkRecordIndex)
        {
            if (!accumulator.IsValid)
                return;

            if (linkRecordIndex != -1)
                this.linkBoundaries!.Add(new(linkRecordIndex, accumulator));

            boundary = RectVector4.Union(accumulator, boundary);
            accumulator = RectVector4.InvertedExtrema;
        }

        /// <summary>Translates the boundaries of inner boundary rectangles.</summary>
        /// <param name="boundary">Mutable reference to the boundary accumulator.</param>
        /// <param name="translation">The translation amount.</param>
        /// <param name="data">The data.</param>
        private void TranslateSubBoundaries(ref RectVector4 boundary, Vector2 translation, DataRef data)
        {
            foreach (ref var b in this.LinkBoundaries)
                b.Boundary = RectVector4.Translate(b.Boundary, translation);
            foreach (ref var v in this.ChildOffsets[..data.Children.Length])
                v += translation;
            boundary = RectVector4.Translate(boundary, translation);
        }
    }
}
