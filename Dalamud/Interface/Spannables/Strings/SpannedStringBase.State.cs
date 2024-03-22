using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.EventHandlerArgs;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Dalamud.Utility.Numerics;
using Dalamud.Utility.Text;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    private class State : ISpannableState
    {
        private static readonly ObjectPool<State> Pool =
            new DefaultObjectPool<State>(new DefaultPooledObjectPolicy<State>());

        private readonly List<BoundaryToRecord> linkBoundaries = new();
        private readonly List<MeasuredLine> measuredLines = new();
        private readonly List<ISpannableState?> spannableStates = new();
        private readonly List<Vector2> spannableOffsets = new();
        private Trss transformation;
        private Vector2 offset;
        private RectVector4 boundary;
        private TextState textState;

        /// <summary>Gets the offset.</summary>
        public ref Vector2 Offset => ref this.offset;

        /// <inheritdoc/>
        public ref TextState TextState => ref this.textState;

        /// <inheritdoc/>
        public ref readonly RectVector4 Boundary => ref this.boundary;

        public Vector2 ScreenOffset { get; private set; }

        public Vector2 TransformationOrigin { get; private set; }

        /// <inheritdoc/>
        public ref readonly Trss Transformation => ref this.transformation;

        /// <inheritdoc/>
        /// <remarks>This is not supposed to be called when not rented, so NRE on accessing this is fine.</remarks>
        public ISpannableRenderer Renderer { get; private set; } = null!;

        /// <summary>Gets the span of measured lines.</summary>
        public Span<MeasuredLine> MeasuredLines => CollectionsMarshal.AsSpan(this.measuredLines);

        /// <summary>Gets the span of mapping between link range to render coordinates.</summary>
        public Span<BoundaryToRecord> LinkBoundaries => CollectionsMarshal.AsSpan(this.linkBoundaries);

        public Span<ISpannableState?> SpannableStates => CollectionsMarshal.AsSpan(this.spannableStates);
        
        public Span<Vector2> SpannableOffsets => CollectionsMarshal.AsSpan(this.spannableOffsets);
        
        public SpannedStringBuilder? TempBuilder { get; set; }

        public uint ImGuiGlobalId { get; private set; }

        public float Scale { get; private set; }

        public Vector2 MaxSize { get; set; }

        public int InteractedLinkRecordIndex { get; set; }

        public bool IsInteractedLinkRecordActive { get; set; }

        public static State Rent(in SpannableRentStateArgs args, DataRef data)
        {
            var t = Pool.Get();
            t.Renderer = args.Renderer;
            t.textState = args.TextState;
            t.ImGuiGlobalId = args.ImGuiGlobalId;
            t.Scale = args.Scale;
            t.spannableStates.EnsureCapacity(data.Spannables.Length);
            t.spannableOffsets.EnsureCapacity(data.Spannables.Length);
            while (t.spannableStates.Count < data.Spannables.Length)
            {
                t.spannableStates.Add(null);
                t.spannableOffsets.Add(default);
            }

            return t;
        }

        public static void Return(State state, DataRef data)
        {
            state.Renderer = null!;
            state.linkBoundaries.Clear();
            state.measuredLines.Clear();
            state.InteractedLinkRecordIndex = -1;
            state.IsInteractedLinkRecordActive = false;

            for (var i = 0; i < data.Spannables.Length; i++)
                data.Spannables[i]?.ReturnState(state.spannableStates[i]);
            state.SpannableStates.Clear();
            Pool.Return(state);
        }

        /// <summary>Adds a line, from <see cref="SpannedStringBase.MeasureSpannable"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLine(in MeasuredLine line) => this.measuredLines.Add(line);

        /// <summary>Clears the boundary, to be used from <see cref="SpannedStringBase.MeasureSpannable"/> step.</summary>
        public void ClearBoundary() => this.boundary = RectVector4.InvertedExtrema;

        /// <summary>Updates <see cref="boundary"/>, <see cref="linkBoundaries"/>,
        /// and resets <paramref name="accumulator"/>, from <see cref="SpannedStringBase.MeasureSpannable"/> step.</summary>
        public void UpdateAndResetBoundary(ref RectVector4 accumulator, int linkRecordIndex)
        {
            if (!accumulator.IsValid)
                return;

            if (linkRecordIndex != -1)
                this.linkBoundaries.Add(new(linkRecordIndex, accumulator));

            this.boundary = RectVector4.Union(accumulator, this.boundary);
            accumulator = RectVector4.InvertedExtrema;
        }

        public void ExtendBoundaryBottom(float b) =>
            this.boundary.Bottom = Math.Max(this.boundary.Bottom, b);

        public void TranslateBoundaries(Vector2 translation, DataRef data)
        {
            foreach (ref var b in this.LinkBoundaries)
                b.Boundary = RectVector4.Translate(b.Boundary, translation);
            foreach (ref var v in this.SpannableOffsets[..data.Spannables.Length])
                v += translation;
            this.boundary = RectVector4.Translate(this.Boundary, translation);
        }

        /// <summary>Finds the first line break point, only taking word wrapping into account, from
        /// <see cref="SpannedStringBase.MeasureSpannable"/> step.</summary>
        public void FindFirstWordWrapByteOffset(
            SpannableMeasureArgs args,
            DataRef.Segment segment,
            CompositeOffset lineStartOffset,
            out MeasuredLine measuredLine)
        {
            measuredLine = MeasuredLine.Empty;

            var wordBreaker = new WordBreaker(args, segment.Data, this);
            var startOffset = lineStartOffset;
            do
            {
                if (segment.TryGetRawText(out var rawText))
                {
                    foreach (var c in rawText[(startOffset.Text - segment.Offset.Text)..]
                                 .EnumerateUtf(UtfEnumeratorFlags.Utf8))
                    {
                        var currentOffset = new CompositeOffset(startOffset.Text + c.ByteOffset, segment.Offset.Record);
                        var nextOffset = currentOffset.AddTextOffset(c.ByteLength);

                        var pad = 0f;
                        if (this.textState.UseControlCharacter && c.Value.ShortName is { IsEmpty: false } name)
                        {
                            var ssb = this.TempBuilder ??= new();
                            ssb.Clear().Append(name);

                            var state2 = Rent(
                                new(
                                    this.Renderer,
                                    0,
                                    this.Scale,
                                    this.textState with
                                    {
                                        LastStyle = this.textState.ControlCharactersStyle,
                                        InitialStyle = this.textState.ControlCharactersStyle,
                                    }),
                                ssb.GetData());
                            ssb.MeasureSpannable(new(state2, new(float.MaxValue)));

                            if (state2.Boundary.IsValid)
                            {
                                pad = MathF.Round(state2.Boundary.Width);
                                wordBreaker.ResetLastChar();
                            }

                            Return(state2, ssb.GetData());
                        }

                        switch (c.Value.IntValue)
                        {
                            case '\r'
                                when segment.Data.TryGetCodepointAt(nextOffset.Text, 0, out var nextCodepoint)
                                     && nextCodepoint == '\n'
                                     && (this.textState.AcceptedNewLines & NewLineType.CrLf) != 0:
                                measuredLine = wordBreaker.Last;
                                measuredLine.SetOffset(nextOffset.AddTextOffset(1), pad);
                                measuredLine.HasNewLineAtEnd = true;
                                wordBreaker.UnionLineBBoxVertical(ref measuredLine);
                                return;

                            case '\r' when (this.textState.AcceptedNewLines & NewLineType.Cr) != 0:
                            case '\n' when (this.textState.AcceptedNewLines & NewLineType.Lf) != 0:
                                measuredLine = wordBreaker.Last;
                                measuredLine.SetOffset(nextOffset, pad);
                                measuredLine.HasNewLineAtEnd = true;
                                wordBreaker.UnionLineBBoxVertical(ref measuredLine);
                                return;

                            case '\r' or '\n':
                                measuredLine = wordBreaker.AddCodepointAndMeasure(
                                    currentOffset,
                                    nextOffset,
                                    -1,
                                    pad: pad);
                                break;

                            default:
                                measuredLine = wordBreaker.AddCodepointAndMeasure(
                                    currentOffset,
                                    nextOffset,
                                    c.EffectiveChar,
                                    pad: pad);
                                break;
                        }

                        if (!measuredLine.IsEmpty)
                            return;
                    }

                    startOffset = new(segment.Offset.Text + rawText.Length, segment.Offset.Record);
                }
                else if (segment.TryGetRecord(out var record, out var recordData))
                {
                    measuredLine = wordBreaker.HandleSpan(record, recordData, new(segment), new(segment, 0, 1));
                    if (!measuredLine.IsEmpty)
                        return;
                }
            }
            while (segment.TryGetNext(out segment));

            measuredLine = wordBreaker.Last;
            measuredLine.SetOffset(new(segment.Offset.Text, segment.Offset.Record));
        }

        public void CommitMeasurement(scoped in SpannableCommitTransformationArgs args)
        {
            this.ScreenOffset = args.ScreenOffset;
            this.TransformationOrigin = args.TransformationOrigin;
            this.transformation = args.Transformation;
        }

        /// <summary>Forces a line break, from both <see cref="SpannedStringBase.MeasureSpannable"/> and
        /// <see cref="SpannedStringBase.DrawSpannable"/> step.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BreakLineImmediate(in MeasuredLine mostRecentLine) =>
            this.Offset = new(0, MathF.Round(this.Offset.Y + mostRecentLine.Height));

        /// <summary>Add decorations and break line once a line ends, from both <see cref="SpannedStringBase.MeasureSpannable"/>
        /// and <see cref="SpannedStringBase.DrawSpannable"/> step.</summary>
        public RectVector4 ProcessPostLine(in MeasuredLine line, ref CharRenderer charRenderer, SpannableDrawArgs args)
        {
            var accumulatedBoundary = RectVector4.InvertedExtrema;
            if (line.IsWrapped)
            {
                if (line.LastThing.IsCodepoint(0x00AD) && this.textState.WordBreak != WordBreakType.KeepAll)
                {
                    accumulatedBoundary = RectVector4.Union(
                        accumulatedBoundary,
                        charRenderer.RenderOne(SoftHyphenReplacementChar));
                }

                if (this.textState.WrapMarker is { } wrapMarker)
                {
                    var state2 = wrapMarker.RentState(
                        new(
                        this.Renderer,
                        0,
                        this.Scale,
                        this.textState with
                        {
                            InitialStyle = this.textState.LastStyle,
                            WordBreak = WordBreakType.KeepAll,
                            WrapMarker = null,
                        }));
                    wrapMarker.MeasureSpannable(new(state2, new(float.MaxValue)));
                    if (state2.Boundary.IsValid)
                    {
                        if (!args.IsEmpty)
                        {
                            var trss = Trss.Identity;
                            if (this.textState.LastStyle.Italic)
                                trss = Trss.CreateSkew(new(MathF.Atan(-1 / TextStyleFontData.FakeItalicDivisor), 0));

                            wrapMarker.CommitSpannableMeasurement(
                                new(
                                    state2,
                                    this.TransformToScreen(this.Offset + charRenderer.StyleTranslation),
                                    this.TransformationOrigin,
                                    Trss.Multiply(trss, Trss.WithoutTranslation(this.Transformation))));
                            args.NotifyChild(wrapMarker, state2);
                        }

                        accumulatedBoundary = RectVector4.Union(
                            accumulatedBoundary,
                            RectVector4.Translate(
                                state2.Boundary,
                                this.Offset + charRenderer.StyleTranslation));
                        this.Offset.X += state2.Boundary.Right;
                        charRenderer.LastRendered.Clear();
                    }

                    wrapMarker.ReturnState(state2);
                }
            }

            if (line.HasNewLineAtEnd || (line.IsWrapped && this.textState.WordBreak != WordBreakType.KeepAll))
                this.BreakLineImmediate(line);
            return accumulatedBoundary;
        }
    }
}
