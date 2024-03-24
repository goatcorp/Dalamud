using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

using Dalamud.Interface.Spannables.Helpers;
using Dalamud.Interface.Spannables.Rendering;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Spannables.Text;

/// <summary>Base class for <see cref="TextSpannable"/> and <see cref="TextSpannableBuilder"/>.</summary>
public abstract partial class TextSpannableBase : ISpannable, ISpannableSerializable
{
    private static readonly BitArray WordBreakNormalBreakChars;

    static TextSpannableBase()
    {
        // Initialize which characters will make a valid word break point.

        WordBreakNormalBreakChars = new(char.MaxValue + 1);

        // https://en.wikipedia.org/wiki/Whitespace_character
        foreach (var c in
                 "\t\n\v\f\r\x20\u0085\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2008\u2009\u200A\u2028\u2029\u205F\u3000\u180E\u200B\u200C\u200D")
            WordBreakNormalBreakChars[c] = true;

        foreach (var range in new[]
                 {
                     UnicodeRanges.HangulJamo,
                     UnicodeRanges.HangulSyllables,
                     UnicodeRanges.HangulCompatibilityJamo,
                     UnicodeRanges.HangulJamoExtendedA,
                     UnicodeRanges.HangulJamoExtendedB,
                     UnicodeRanges.CjkCompatibility,
                     UnicodeRanges.CjkCompatibilityForms,
                     UnicodeRanges.CjkCompatibilityIdeographs,
                     UnicodeRanges.CjkRadicalsSupplement,
                     UnicodeRanges.CjkSymbolsandPunctuation,
                     UnicodeRanges.CjkStrokes,
                     UnicodeRanges.CjkUnifiedIdeographs,
                     UnicodeRanges.CjkUnifiedIdeographsExtensionA,
                     UnicodeRanges.Hiragana,
                     UnicodeRanges.Katakana,
                     UnicodeRanges.KatakanaPhoneticExtensions,
                 })
        {
            for (var i = 0; i < range.Length; i++)
                WordBreakNormalBreakChars[range.FirstCodePoint + i] = true;
        }
    }

    /// <inheritdoc/>
    public int StateGeneration { get; protected set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var s in this.GetAllChildSpannables())
            s?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public abstract IReadOnlyCollection<ISpannable?> GetAllChildSpannables();

    /// <inheritdoc/>
    public ISpannableRenderPass RentRenderPass(ISpannableRenderer renderer) => RenderPass.Rent(renderer);

    /// <inheritdoc/>
    public void ReturnRenderPass(ISpannableRenderPass? pass)
    {
        if (pass is RenderPass s)
            RenderPass.Return(s, this.GetData());
    }

    /// <inheritdoc/>
    public int SerializeState(Span<byte> buffer) =>
        SpannableSerializationHelper.Write(ref buffer, this.GetAllChildSpannables());

    /// <inheritdoc/>
    public bool TryDeserializeState(ReadOnlySpan<byte> buffer, out int consumed)
    {
        var origLen = buffer.Length;
        consumed = 0;
        if (!SpannableSerializationHelper.TryRead(ref buffer, this.GetAllChildSpannables()))
            return false;
        consumed += origLen - buffer.Length;
        return true;
    }

    /// <summary>Gets the data required for rendering.</summary>
    /// <returns>The data.</returns>
    private protected abstract DataRef GetData();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEffectivelyInfinity(float f) => f >= float.PositiveInfinity;

    private ref struct StateInfo
    {
        public float HorizontalOffsetWrtLine;
        public float VerticalOffsetWrtLine;

        private readonly RenderPass renderPass;

        private readonly Vector2 lineBBoxVertical;
        private readonly float lineWidth;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateInfo(RenderPass renderPass, scoped in MeasuredLine lineMeasurement)
        {
            this.renderPass = renderPass;
            this.lineBBoxVertical = lineMeasurement.BBoxVertical;
            this.lineWidth = lineMeasurement.Width;
        }

        public void Update(in TextStyleFontData fontInfo)
        {
            var lineAscentDescent = this.lineBBoxVertical;
            this.VerticalOffsetWrtLine = (fontInfo.BBoxVertical.Y - fontInfo.BBoxVertical.X) *
                                         this.renderPass.ActiveTextState.LastStyle.VerticalOffset;
            switch (this.renderPass.ActiveTextState.LastStyle.VerticalAlignment)
            {
                case < 0:
                    this.VerticalOffsetWrtLine -= lineAscentDescent.X + (fontInfo.Font.Ascent * fontInfo.Scale);
                    break;
                case >= 1f:
                    this.VerticalOffsetWrtLine += lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize;
                    break;
                default:
                    this.VerticalOffsetWrtLine +=
                        (lineAscentDescent.Y - lineAscentDescent.X - fontInfo.ScaledFontSize) *
                        this.renderPass.ActiveTextState.LastStyle.VerticalAlignment;
                    break;
            }

            this.VerticalOffsetWrtLine = MathF.Round(this.VerticalOffsetWrtLine);

            var alignWidth = this.renderPass.MaxSize.X;
            var alignLeft = 0f;
            if (IsEffectivelyInfinity(alignWidth))
            {
                if (!this.renderPass.Boundary.IsValid)
                {
                    this.HorizontalOffsetWrtLine = 0;
                    return;
                }

                alignWidth = this.renderPass.Boundary.Width;
                alignLeft = this.renderPass.Boundary.Left;
            }

            switch (this.renderPass.ActiveTextState.LastStyle.HorizontalAlignment)
            {
                case <= 0f:
                    this.HorizontalOffsetWrtLine = 0;
                    break;

                case >= 1f:
                    this.HorizontalOffsetWrtLine = alignLeft + (alignWidth - this.lineWidth);
                    break;

                default:
                    this.HorizontalOffsetWrtLine =
                        MathF.Round(
                            (alignLeft + (alignWidth - this.lineWidth)) *
                            this.renderPass.ActiveTextState.LastStyle.HorizontalAlignment);
                    break;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BoundaryToRecord
    {
        public RectVector4 Boundary;
        public int RecordIndex;

        public BoundaryToRecord(int recordIndex, RectVector4 boundary)
        {
            this.RecordIndex = recordIndex;
            this.Boundary = boundary;
        }
    }

    /// <summary>Item state for the spanned string itself.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct ImGuiItemStateStruct
    {
        [FieldOffset(0)]
        public int InteractedLinkRecordIndex;

        [FieldOffset(4)]
        public InteractionState State;

        public enum InteractionState : byte
        {
            Clear,
            Hover,
            Active,
        }
    }

    /// <summary>Item state for individual links.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    private struct ImGuiLinkStateStruct
    {
        [FieldOffset(0)]
        public uint Flags;

        public bool IsMouseButtonDownHandled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (this.Flags & 1) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~1u) | (value ? 1u : 0u);
        }

        public ImGuiMouseButton FirstMouseButton
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (ImGuiMouseButton)((this.Flags >> 1) & 3);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => this.Flags = (this.Flags & ~(3u << 1)) | ((uint)value << 1);
        }
    }
}
