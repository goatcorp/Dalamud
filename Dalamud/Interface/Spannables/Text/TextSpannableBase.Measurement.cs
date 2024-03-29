using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using ImGuiNET;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="TextSpannable"/> and <see cref="TextSpannableBuilder"/>.</summary>
[SuppressMessage(
    "StyleCop.CSharp.SpacingRules",
    "SA1010:Opening square brackets should be spaced correctly",
    Justification = "Stfu")]
public abstract partial class TextSpannableBase
{
    private static readonly ObjectPool<Measurement> MyMeasurementPool =
        new DefaultObjectPool<Measurement>(new DefaultPooledObjectPolicy<Measurement>());

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
        Clicked,
    }

    /// <summary>Private helper interface for mutating <see cref="Measurement"/>.</summary>
    private interface IInternalMeasurement : ISpannableMeasurement
    {
        /// <summary>Gets an ithisutable reference to <see cref="Measurement.Options"/>.</summary>
        new Options Options { get; }

        /// <summary>Gets a mutable reference to <see cref="ISpannableMeasurement.Spannable"/>.</summary>
        new ref ISpannable? Spannable { get; }

        /// <summary>Gets a mutable reference to <see cref="ISpannableMeasurement.Renderer"/>.</summary>
        new ref ISpannableRenderer? Renderer { get; }

        /// <summary>Gets a mutable reference to <see cref="ISpannableMeasurement.IsMeasurementValid"/>.</summary>
        [SuppressMessage(
            "StyleCop.CSharp.DocumentationRules",
            "SA1623:Property summary documentation should match accessors",
            Justification = "Stfu")]
        new ref bool IsMeasurementValid { get; }

        /// <summary>Gets a mutable reference to <see cref="ISpannableMeasurement.Boundary"/>.</summary>
        new ref RectVector4 Boundary { get; }

        /// <summary>Gets a mutable reference to <see cref="Measurement.LastStyle"/>.</summary>
        ref TextStyle LastStyle { get; }

        /// <summary>Gets a mutable reference to <see cref="Measurement.lastOffset"/>.</summary>
        ref Vector2 LastOffset { get; }

        /// <summary>Gets or sets the effective vertical shift from <see cref="VerticalAlignment"/>.</summary>
        float ShiftFromVerticalAlignment { get; set; }

        /// <summary>Gets the span of measurements of inner spannables.</summary>
        Span<ISpannableMeasurement?> ChildMeasurements { get; }

        /// <summary>Gets the span of offsets of inner spannables.</summary>
        Span<Vector2> ChildOffsets { get; }

        /// <summary>Gets a mutable reference for local transformation of this spannable.</summary>
        ref Matrix4x4 LocalTransformation { get; }

        /// <summary>Gets a mutable reference for full transformation of this spannable.</summary>
        new ref Matrix4x4 FullTransformation { get; }

        /// <summary>Clears measured data.</summary>
        void ClearMeasurement();

        /// <summary>Sets up this measurement.</summary>
        /// <param name="renderer">The renderer to attach.</param>
        /// <param name="text">The text to attach.</param>
        void Setup(ISpannableRenderer renderer, TextSpannableBase text);
    }

    /// <inheritdoc/>
    ISpannableMeasurement ISpannable.RentMeasurement(ISpannableRenderer renderer) => this.RentMeasurement(renderer);

    /// <inheritdoc cref="ISpannable.RentMeasurement"/>
    public Measurement RentMeasurement(ISpannableRenderer renderer)
    {
        var t = MyMeasurementPool.Get();
        t.TryReset();
        ((IInternalMeasurement)t).Setup(renderer, this);
        return t;
    }

    /// <inheritdoc/>
    public void ReturnMeasurement(ISpannableMeasurement? measurement)
    {
        if (measurement is Measurement mm)
            MyMeasurementPool.Return(mm);
    }

    /// <summary>Options for <see cref="Measurement"/>.</summary>
    public sealed class Options : SpannableMeasurementOptions, IEquatable<Options>
    {
        private bool displayControlCharacters;
        private WordBreakType wordBreak;
        private NewLineType acceptedNewLines;
        private float tabWidth;
        private float verticalAlignment;
        private int gfdIconMode;
        private TextStyle controlCharactersStyle;
        private TextStyle style;
        private ISpannable? wrapMarker;

        /// <summary>Initializes a new instance of the <see cref="Options"/> class.</summary>
        public Options() => this.TryReset();

        /// <summary>Gets or sets a value indicating whether to display representations of control characters, such as
        /// CR, LF, NBSP, and SHY.</summary>
        public bool DisplayControlCharacters
        {
            get => this.displayControlCharacters;
            set => this.UpdateProperty(nameof(this.DisplayControlCharacters), ref this.displayControlCharacters, value);
        }

        /// <summary>Gets or sets how to handle word break mode.</summary>
        public WordBreakType WordBreak
        {
            get => this.wordBreak;
            set => this.UpdateProperty(nameof(this.WordBreak), ref this.wordBreak, value);
        }

        /// <summary>Gets or sets the type of new line sequences to handle.</summary>
        public NewLineType AcceptedNewLines
        {
            get => this.acceptedNewLines;
            set => this.UpdateProperty(nameof(this.AcceptedNewLines), ref this.acceptedNewLines, value);
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
            set => this.UpdateProperty(nameof(this.TabWidth), ref this.tabWidth, value);
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
            set => this.UpdateProperty(nameof(this.VerticalAlignment), ref this.verticalAlignment, value);
        }

        /// <summary>Gets or sets the graphic font icon mode.</summary>
        /// <remarks>A value outside the suported range will use the one configured from the game configuration via
        /// game controller configuration.</remarks>
        public int GfdIconMode
        {
            get => this.gfdIconMode;
            set => this.UpdateProperty(nameof(this.GfdIconMode), ref this.gfdIconMode, value);
        }

        /// <summary>Gets or sets the text style for the control characters.</summary>
        /// <remarks>Does nothing if <see cref="DisplayControlCharacters"/> is <c>false</c>.</remarks>
        public TextStyle ControlCharactersStyle
        {
            get => this.controlCharactersStyle;
            set => this.UpdateProperty(nameof(this.ControlCharactersStyle), ref this.controlCharactersStyle, value);
        }

        /// <summary>Gets or sets the initial text style.</summary>
        /// <remarks>Text styles may be altered in middle of the text, but this property will not change.
        /// Use <see cref="Measurement.LastStyle"/> for that information.</remarks>
        public TextStyle Style
        {
            get => this.style;
            set => this.UpdateProperty(nameof(this.Style), ref this.style, value);
        }

        /// <summary>Gets or sets the ellipsis or line break indicator string to display.</summary>
        /// <value><c>null</c> indicates that wrap markers are disabled.</value>
        public ISpannable? WrapMarker
        {
            get => this.wrapMarker;
            set => this.UpdateProperty(nameof(this.WrapMarker), ref this.wrapMarker, value);
        }

        public static bool operator ==(Options? left, Options? right) => Equals(left, right);

        public static bool operator !=(Options? left, Options? right) => !Equals(left, right);

        /// <inheritdoc/>
        public override void CopyFrom(ISpannableMeasurementOptions source)
        {
            if (source is Options mo)
                this.CopyFrom(mo);
            else
                base.CopyFrom(source);
        }

        /// <inheritdoc cref="ISpannableMeasurementOptions.CopyFrom"/>
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
        
        /// <inheritdoc/>
        public bool Equals(Options? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return
                this.displayControlCharacters == other.displayControlCharacters
                && this.wordBreak == other.wordBreak
                && this.acceptedNewLines == other.acceptedNewLines
                && this.tabWidth.Equals(other.tabWidth)
                && this.verticalAlignment.Equals(other.verticalAlignment)
                && this.gfdIconMode == other.gfdIconMode
                && this.controlCharactersStyle.Equals(other.controlCharactersStyle)
                && this.style.Equals(other.style)
                && Equals(this.wrapMarker, other.wrapMarker);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) || (obj is Options other && this.Equals(other));
        
        /// <inheritdoc/>
        // just shutting up warnings on not having HashCode impl'd when Equals is
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }

    /// <summary>Measurement for <see cref="TextSpannable"/> and <see cref="TextSpannableBuilder"/>.</summary>
    [SuppressMessage("ReSharper", "ConvertToAutoProperty", Justification = "WIP")]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter", Justification = "WIP")]
    public sealed class Measurement : IInternalMeasurement
    {
        private readonly List<BoundaryToRecord> linkBoundaries = [];
        private readonly List<MeasuredLine> lines = [];
        private readonly List<LinkInteractionData> linkInteractions = [];
        private readonly List<ISpannableMeasurement?> childrenMeasurements = [];
        private readonly List<Vector2> childOffsets = [];
        private readonly Options options;

        private float scale;

        private ISpannable? spannable;
        private ISpannableRenderer? renderer;
        private int childCount;
        private bool isMeasurementValid;
        private RectVector4 boundary;
        private TextStyle lastStyle;
        private Vector2 lastOffset;

        private Matrix4x4 localTransformation;
        private Matrix4x4 fullTransformation;
        private float shiftFromVerticalAlignment;

        private int interactedLinkIndex;
        private LinkState interactedLinkState;

        /// <summary>Initializes a new instance of the <see cref="Measurement"/> class.</summary>
        public Measurement()
        {
            this.options = new();
            this.TryReset();
        }

        /// <inheritdoc/>
        public ISpannable? Spannable => this.spannable;

        /// <inheritdoc/>
        ref ISpannable? IInternalMeasurement.Spannable => ref this.spannable;

        /// <inheritdoc/>
        public ISpannableRenderer? Renderer => this.renderer;

        /// <inheritdoc/>
        ref ISpannableRenderer? IInternalMeasurement.Renderer => ref this.renderer;

        /// <inheritdoc/>
        public bool IsMeasurementValid => this.isMeasurementValid;

        /// <inheritdoc/>
        ref bool IInternalMeasurement.IsMeasurementValid => ref this.isMeasurementValid;

        /// <inheritdoc/>
        public RectVector4 Boundary => this.boundary;

        /// <inheritdoc/>
        ref RectVector4 IInternalMeasurement.Boundary => ref this.boundary;

        /// <inheritdoc cref="ISpannableMeasurement.Options"/>
        public Options Options => this.options;

        /// <inheritdoc/>
        public uint ImGuiGlobalId { get; set; }

        /// <inheritdoc/>
        ISpannableMeasurementOptions ISpannableMeasurement.Options => this.options;

        /// <inheritdoc/>
        public float RenderScale
        {
            get => this.scale;
            set => this.UpdateProperty(ref this.scale, value);
        }

        /// <summary>Gets a read-only reference to the last text style used.</summary>
        public ref readonly TextStyle LastStyle => ref this.lastStyle;

        /// <inheritdoc/>
        ref TextStyle IInternalMeasurement.LastStyle => ref this.lastStyle;

        /// <inheritdoc/>
        ref Vector2 IInternalMeasurement.LastOffset => ref this.lastOffset;

        /// <summary>Gets the number of lines.</summary>
        public int LineCount => this.lines.Count;

        /// <inheritdoc/>
        float IInternalMeasurement.ShiftFromVerticalAlignment
        {
            get => this.shiftFromVerticalAlignment;
            set => this.shiftFromVerticalAlignment = value;
        }

        /// <inheritdoc/>
        Span<ISpannableMeasurement?> IInternalMeasurement.ChildMeasurements =>
            CollectionsMarshal.AsSpan(this.childrenMeasurements)[..this.childCount];

        /// <inheritdoc/>
        Span<Vector2> IInternalMeasurement.ChildOffsets =>
            CollectionsMarshal.AsSpan(this.childOffsets)[..this.childCount];

        /// <inheritdoc/>
        ref Matrix4x4 IInternalMeasurement.LocalTransformation => ref this.localTransformation;

        /// <inheritdoc/>
        public ref readonly Matrix4x4 FullTransformation => ref this.fullTransformation;

        /// <inheritdoc/>
        ref Matrix4x4 IInternalMeasurement.FullTransformation => ref this.fullTransformation;

        /// <summary>Gets the measured lines so far.</summary>
        private Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.lines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        private Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.linkBoundaries);

        /// <summary>Gets the span of link interaction states.</summary>
        private Span<LinkInteractionData> LinkInteractions => CollectionsMarshal.AsSpan(this.linkInteractions);

        /// <inheritdoc/>
        public bool TryReset()
        {
            for (var i = 0; i < this.childCount; i++)
            {
                this.childrenMeasurements[i]?.Spannable?.ReturnMeasurement(this.childrenMeasurements[i]);
                this.childrenMeasurements[i] = null;
            }

            this.scale = 1f;
            this.ImGuiGlobalId = 0u;
            this.localTransformation = this.fullTransformation = Matrix4x4.Identity;
            this.options.TryReset();
            this.spannable = null;
            this.childCount = 0;
            this.interactedLinkIndex = -1;
            this.interactedLinkState = LinkState.Clear;
            this.linkInteractions.Clear();
            this.ClearMeasurement();
            return true;
        }

        /// <inheritdoc/>
        public void ClearMeasurement()
        {
            this.boundary = RectVector4.InvertedExtrema;
            this.isMeasurementValid = false;
            this.linkBoundaries.Clear();
            this.lines.Clear();
            this.lastStyle = this.options.Style;
            this.lastOffset = Vector2.Zero;
        }

        /// <inheritdoc/>
        void IInternalMeasurement.Setup(ISpannableRenderer r, TextSpannableBase text)
        {
            this.renderer = r;
            this.spannable = text;

            var data = text.GetData();
            var children = data.Spannables;
            this.childCount = children.Length;
            this.childrenMeasurements.EnsureCapacity(this.childCount);
            this.childOffsets.EnsureCapacity(this.childCount);
            while (this.childrenMeasurements.Count < this.childCount)
            {
                this.childrenMeasurements.Add(null);
                this.childOffsets.Add(default);
            }

            for (var i = 0; i < children.Length; i++)
                this.childrenMeasurements[i] = children[i]?.RentMeasurement(r);

            this.linkInteractions.EnsureCapacity(data.Records.Length);
            for (var i = 0; i < data.Records.Length; i++)
                this.linkInteractions.Add(default);
        }

        /// <summary>Gets the interacted link.</summary>
        /// <param name="linkData">The link data.</param>
        /// <returns>The link state.</returns>
        public LinkState GetInteractedLink(out ReadOnlySpan<byte> linkData)
        {
            if (this.spannable is not TextSpannableBase tsb)
            {
                linkData = default;
                return LinkState.Clear;
            }

            return
                tsb.GetData().TryGetLinkAt(this.interactedLinkIndex, out linkData)
                    ? this.interactedLinkState
                    : LinkState.Clear;
        }

        /// <inheritdoc/>
        public bool HandleInteraction()
        {
            if (this.spannable is not TextSpannableBase tsb || !this.boundary.IsValid)
                return false;

            var data = tsb.GetData();

            foreach (var m in ((IInternalMeasurement)this).ChildMeasurements)
                m?.HandleInteraction();

            this.interactedLinkIndex = -1;
            if (this.interactedLinkState == LinkState.Clicked)
                this.interactedLinkState = LinkState.Clear;
            // TODO: setup links

            var mouseRel = this.PointToClient(ImGui.GetMousePos());
            foreach (ref var entry in this.LinkBoundaries)
            {
                if (entry.Boundary.Contains(mouseRel)
                    && SpannableImGuiItem.IsItemHoverable(this, mouseRel, entry.Boundary, entry.RecordIndex))
                {
                    if (data.TryGetLinkAt(entry.RecordIndex, out _))
                        this.interactedLinkIndex = entry.RecordIndex;
                    break;
                }
            }

            var prevLinkRecordIndex = -1;
            foreach (ref var linkBoundary in this.LinkBoundaries)
            {
                if (prevLinkRecordIndex == linkBoundary.RecordIndex)
                    continue;
                prevLinkRecordIndex = linkBoundary.RecordIndex;

                ref var linkState = ref this.LinkInteractions[linkBoundary.RecordIndex];

                if (linkState.IsMouseButtonDownHandled)
                {
                    switch (linkState.FirstMouseButton)
                    {
                        case var _ when linkBoundary.RecordIndex != this.interactedLinkIndex:
                            this.interactedLinkIndex = -1;
                            break;
                        case ImGuiMouseButton.Left when !ImGui.IsMouseDown(ImGuiMouseButton.Left):
                        case ImGuiMouseButton.Right when !ImGui.IsMouseDown(ImGuiMouseButton.Right):
                        case ImGuiMouseButton.Middle when !ImGui.IsMouseDown(ImGuiMouseButton.Middle):
                            this.interactedLinkState = LinkState.Clicked;
                            linkState.IsMouseButtonDownHandled = false;
                            break;
                    }

                    if (!ImGui.GetIO().MouseDown[0] && !ImGui.GetIO().MouseDown[1] && !ImGui.GetIO().MouseDown[2])
                    {
                        linkState.IsMouseButtonDownHandled = false;
                        SpannableImGuiItem.ClearActive();
                    }
                }

                if (this.interactedLinkIndex == linkBoundary.RecordIndex)
                {
                    SpannableImGuiItem.SetHovered(this, linkBoundary.RecordIndex);
                    if (!linkState.IsMouseButtonDownHandled)
                    {
                        if (ImGui.IsMouseDown(linkState.FirstMouseButton = ImGuiMouseButton.Left))
                            linkState.IsMouseButtonDownHandled = true;
                        else if (ImGui.IsMouseDown(linkState.FirstMouseButton = ImGuiMouseButton.Right))
                            linkState.IsMouseButtonDownHandled = true;
                        else if (ImGui.IsMouseDown(linkState.FirstMouseButton = ImGuiMouseButton.Middle))
                            linkState.IsMouseButtonDownHandled = true;
                    }

                    if (this.interactedLinkState != LinkState.Clicked)
                    {
                        this.interactedLinkState =
                            linkState.IsMouseButtonDownHandled
                                ? LinkState.Active
                                : LinkState.Hovered;
                    }
                }

                if (linkState.IsMouseButtonDownHandled)
                {
                    SpannableImGuiItem.SetHovered(this, linkBoundary.RecordIndex);
                    SpannableImGuiItem.SetActive(this, linkBoundary.RecordIndex);
                }
            }

            if (this.interactedLinkIndex == -1)
                this.interactedLinkState = LinkState.Clear;
            return true;
        }

        /// <inheritdoc/>
        public bool Measure()
        {
            if (this.spannable is not TextSpannableBase tsb || this.IsMeasurementValid)
                return false;

            this.ClearMeasurement();

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

                                    var ccm = ssb.RentMeasurement(this.Renderer);
                                    ccm.RenderScale = this.RenderScale;
                                    ccm.Options.ControlCharactersStyle = this.Options.ControlCharactersStyle;
                                    ccm.Measure();

                                    if (ccm.Boundary.IsValid)
                                    {
                                        pad = MathF.Ceiling(ccm.Boundary.Width * this.RenderScale) / this.RenderScale;
                                        wordBreaker.ResetLastChar();
                                    }

                                    ssb.ReturnMeasurement(ccm);
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
                                        line.SetOffset(nextOffset.AddTextOffset(1), this.RenderScale, pad);
                                        line.HasNewLineAtEnd = true;
                                        wordBreaker.UnionLineBBoxVertical(ref line);
                                        break;

                                    case '\r' when (this.Options.AcceptedNewLines & NewLineType.Cr) != 0:
                                    case '\n' when (this.Options.AcceptedNewLines & NewLineType.Lf) != 0:
                                        line = wordBreaker.Last;
                                        line.SetOffset(nextOffset, this.RenderScale, pad);
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
                        line.SetOffset(new(testSegment.Offset.Text, testSegment.Offset.Record), this.RenderScale, 0f);
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
                                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                                    linkRecordIndex = -1;
                                    break;

                                case SpannedRecordType.Link
                                    when SpannedRecordCodec.TryDecodeLink(recordData, out var link):
                                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
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

                    this.UpdateAndResetBoundary(ref accumulatedBoundary, linkRecordIndex);
                    this.ExtendBoundaryDownward(this.lastOffset.Y + charRenderer.MostRecentLineHeight);
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

            this.ExtendBoundaryDownward(this.lastOffset.Y);

            if (!this.boundary.IsValid)
                this.boundary = default;
            else
                this.boundary.Right += 1;

            // if (this.Options.Size.X < float.PositiveInfinity)
            //     this.boundary.Right = this.Options.Size.X;
            // if (this.Options.Size.Y < float.PositiveInfinity)
            //     this.boundary.Bottom = this.Options.Size.Y;

            if (this.Options.VerticalAlignment > 0f && this.Options.Size.Y < float.PositiveInfinity)
            {
                var offset =
                    MathF.Round(
                        (this.Options.Size.Y - this.boundary.Height) *
                        Math.Clamp(this.Options.VerticalAlignment, 0f, 1f) *
                        this.RenderScale) /
                    this.RenderScale;
                this.TranslateSubBoundaries(new(0, offset), data);
                this.shiftFromVerticalAlignment = offset;
            }
            else
            {
                this.shiftFromVerticalAlignment = 0;
            }

            return true;
        }

        /// <inheritdoc/>
        public void UpdateTransformation(scoped in Matrix4x4 local, scoped in Matrix4x4 ancestral)
        {
            var mm = (IInternalMeasurement)this;
            this.localTransformation = local;
            this.fullTransformation = Matrix4x4.Multiply(local, ancestral);
            foreach (var m in mm.ChildMeasurements)
                m?.UpdateTransformation(Matrix4x4.Identity, this.FullTransformation);
        }

        /// <inheritdoc/>
        public unsafe void Draw(ImDrawListPtr drawListPtr)
        {
            if (this.spannable is not TextSpannableBase tsb)
                return;

            var data = tsb.GetData();

            this.lastOffset = new(0, this.shiftFromVerticalAlignment);
            this.lastStyle = this.Options.Style;

            var charRenderer = new CharRenderer(this, data, drawListPtr);
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

                    this.ProcessPostLine(line, ref charRenderer, drawListPtr);
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
                charRenderer.AppendAndReturnChannels(this.localTransformation);
            }
        }

        /// <inheritdoc/>
        public ISpannableMeasurement? FindChildMeasurementAt(Vector2 screenOffset)
        {
            for (var i = 0; i < this.childCount; i++)
            {
                if (this.childrenMeasurements[i] is not { } m)
                    continue;
                if (m.Boundary.Contains(m.PointToClient(screenOffset)))
                    return m;
            }

            return null;
        }

        /// <inheritdoc/>
        public void ReturnMeasurementToSpannable() => this.Spannable?.ReturnMeasurement(this);

        /// <summary>Extends the bottom boundary by given amount.</summary>
        /// <param name="b">The amount to extend.</param>
        public void ExtendBoundaryDownward(float b)
        {
            if (this.boundary.IsValid)
                this.boundary.Bottom = Math.Max(this.boundary.Bottom, b);
        }

        /// <summary>Adds a line break.</summary>
        /// <param name="lineBefore">The line that came right before this line break.</param>
        private void AddLineBreak(in MeasuredLine lineBefore) =>
            this.lastOffset = new(
                0,
                MathF.Round((this.lastOffset.Y + lineBefore.Height) * this.RenderScale) / this.RenderScale);

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
                if (line.LastThing.IsCodepoint(0x00AD) && this.options.WordBreak != WordBreakType.KeepAll)
                {
                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        charRenderer.RenderOne(SoftHyphenReplacementChar));
                }

                if (this.options.WrapMarker is { } wm)
                {
                    var wmm = wm.RentMeasurement(this.Renderer!);
                    wmm.RenderScale = this.scale;
                    wmm.Measure();

                    if (wmm.Boundary.IsValid)
                    {
                        if (drawListPtr.NativePtr is not null)
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

                            wmm.UpdateTransformation(wmLocalTransformation, this.fullTransformation);

                            var tmpDrawList = this.Renderer.RentDrawList(drawListPtr);
                            try
                            {
                                wmm.Draw(tmpDrawList);
                                tmpDrawList.CopyDrawListDataTo(drawListPtr, this.localTransformation, Vector4.One);
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

                    wm.ReturnMeasurement(wmm);
                }
            }

            if (line.HasNewLineAtEnd || (line.IsWrapped && this.options.WordBreak != WordBreakType.KeepAll))
                this.AddLineBreak(line);
            return accumulatedBoundary;
        }

        /// <summary>Updates <see cref="boundary"/> and <see cref="linkBoundaries"/>, and resets
        /// <paramref name="accumulator"/>.</summary>
        /// <param name="accumulator">The accumulator.</param>
        /// <param name="linkRecordIndex">The link record index.</param>
        private void UpdateAndResetBoundary(ref RectVector4 accumulator, int linkRecordIndex)
        {
            if (!accumulator.IsValid)
                return;

            if (linkRecordIndex != -1)
                this.linkBoundaries!.Add(new(linkRecordIndex, accumulator));

            this.boundary = RectVector4.Union(accumulator, this.boundary);
            accumulator = RectVector4.InvertedExtrema;
        }

        /// <summary>Translates the boundaries of inner boundary rectangles.</summary>
        /// <param name="translation">The translation amount.</param>
        /// <param name="data">The data.</param>
        private void TranslateSubBoundaries(Vector2 translation, DataRef data)
        {
            var mm = (IInternalMeasurement)this;
            foreach (ref var b in this.LinkBoundaries)
                b.Boundary = RectVector4.Translate(b.Boundary, translation);
            foreach (ref var v in mm.ChildOffsets[..data.Spannables.Length])
                v += translation;
            this.boundary = RectVector4.Translate(this.boundary, translation);
        }

        private void UpdateProperty<T>(ref T storage, in T newValue)
        {
            if (Equals(storage, newValue))
                return;
            this.isMeasurementValid = false;
            storage = newValue;
        }
    }
}
