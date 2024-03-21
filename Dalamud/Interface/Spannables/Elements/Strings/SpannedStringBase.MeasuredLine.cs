using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Interface.Spannables.Rendering;

namespace Dalamud.Interface.Spannables.Elements.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    /// <summary>Represents a measured line.</summary>
    private protected struct MeasuredLine
    {
        /// <summary>The offset in a <see cref="SpannedString"/> to begin to skip rendering.</summary>
        public CompositeOffset FirstOffset;

        /// <summary>The offset in a <see cref="SpannedString"/> pointing to the end of a line.</summary>
        public CompositeOffset Offset;

        /// <summary>The offset in a <see cref="SpannedString"/> to begin to skip rendering.</summary>
        public CompositeOffset OmitOffset;

        /// <summary>The horizontal cursor offset at the end of the line.</summary>
        public float X;

        /// <summary>The left(-) and right(+) bounds of the line.</summary>
        public Vector2 BBoxHorizontal;

        /// <summary>The ascent(-) and descent(+) of the line.</summary>
        public Vector2 BBoxVertical;

        /// <summary>The last thing processed.</summary>
        public LastThingStruct LastThing;

        /// <summary>Whether the line ends because it was too long and got wrapped/truncated.</summary>
        public bool IsWrapped;

        /// <summary>Whether the line ends because there is a line break character at the end.</summary>
        public bool HasNewLineAtEnd;

        /// <summary>Gets an empty value.</summary>
        public static MeasuredLine Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new()
            {
                Offset = CompositeOffset.Empty,
                BBoxHorizontal = new(float.MaxValue, float.MinValue),
                BBoxVertical = new(float.MaxValue, float.MinValue),
                LastThing = default,
            };
        }

        /// <summary>Gets the width.</summary>
        public readonly float Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.BBoxHorizontal.X < this.BBoxHorizontal.Y ? this.BBoxHorizontal.Y - this.BBoxHorizontal.X : 0;
        }

        /// <summary>Gets the height.</summary>
        public readonly float Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.BBoxVertical.X < this.BBoxVertical.Y ? this.BBoxVertical.Y - this.BBoxVertical.X : 0;
        }

        /// <summary>Gets a value indicating whether nothing has been measured yet.</summary>
        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Offset == CompositeOffset.Empty;
        }

        /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
        /// <param name="si1">The first value.</param>
        /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static MeasuredLine FirstNonEmpty(in MeasuredLine si1)
        {
            if (!si1.IsEmpty)
                return si1;
            return Empty;
        }

        /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
        /// <param name="si1">The first value.</param>
        /// <param name="si2">The second value.</param>
        /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static MeasuredLine FirstNonEmpty(in MeasuredLine si1, in MeasuredLine si2)
        {
            if (!si1.IsEmpty)
                return si1;
            if (!si2.IsEmpty)
                return si2;
            return Empty;
        }

        /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
        /// <param name="si1">The first value.</param>
        /// <param name="si2">The second value.</param>
        /// <param name="si3">The third value.</param>
        /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static MeasuredLine FirstNonEmpty(in MeasuredLine si1, in MeasuredLine si2, in MeasuredLine si3)
        {
            if (!si1.IsEmpty)
                return si1;
            if (!si2.IsEmpty)
                return si2;
            if (!si3.IsEmpty)
                return si3;
            return Empty;
        }

        /// <summary>Gets the first non-empty value, or <see cref="Empty"/>.</summary>
        /// <param name="si1">The first value.</param>
        /// <param name="si2">The second value.</param>
        /// <param name="si3">The third value.</param>
        /// <param name="si4">The fourth value.</param>
        /// <returns>The first non-empty value, or <see cref="Empty"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public static MeasuredLine FirstNonEmpty(
            in MeasuredLine si1, in MeasuredLine si2, in MeasuredLine si3, in MeasuredLine si4)
        {
            if (!si1.IsEmpty)
                return si1;
            if (!si2.IsEmpty)
                return si2;
            if (!si3.IsEmpty)
                return si3;
            if (!si4.IsEmpty)
                return si4;
            return Empty;
        }

        /// <summary>Unions the given horizontal boundary box to this instance.</summary>
        /// <param name="x0">The left boundary.</param>
        /// <param name="x1">The right boundary.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void UnionBBoxHorizontal(float x0, float x1)
        {
            this.BBoxHorizontal.X = MathF.Round(Math.Min(this.BBoxHorizontal.X, x0));
            this.BBoxHorizontal.Y = MathF.Round(Math.Max(this.BBoxHorizontal.Y, x1));
        }

        /// <summary>Unions the given vertical boundary box to this instance.</summary>
        /// <param name="ascent">The top boundary (=ascent for this implementation).</param>
        /// <param name="descent">The bottom boundary (=descent for this implementation).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public void UnionBBoxVertical(float ascent, float descent)
        {
            this.BBoxVertical.X = MathF.Round(Math.Min(this.BBoxVertical.X, ascent));
            this.BBoxVertical.Y = MathF.Round(Math.Max(this.BBoxVertical.Y, descent));
        }

        /// <summary>Tests if the line is contained within a right boundary.</summary>
        /// <param name="fontData">The font data.</param>
        /// <param name="rightBoundary">The right boundary.</param>
        /// <returns><c>true</c> if it will.</returns>
        public readonly bool ContainedInBounds(SpanStyleFontData fontData, float rightBoundary) =>
            MathF.Round(this.BBoxHorizontal.Y) <= rightBoundary;

        /// <summary>Tests if adding an object will still keep the line from reaching a right boundary.</summary>
        /// <param name="fontData">The font data.</param>
        /// <param name="objectWidth">The object width.</param>
        /// <param name="rightBoundary">The right boundary.</param>
        /// <returns><c>true</c> if it will.</returns>
        public readonly bool ContainedInBoundsWithObject(
            SpanStyleFontData fontData,
            float objectWidth,
            float rightBoundary) =>
            MathF.Round(Math.Max(this.BBoxHorizontal.Y, this.X + objectWidth + fontData.ScaledHorizontalOffset))
            <= rightBoundary;

        /// <summary>Returns a new instance of this struct after setting <see cref="IsWrapped"/>.</summary>
        /// <returns>The new instance.</returns>
        public readonly MeasuredLine WithWrapped() => this with { IsWrapped = true };

        /// <summary>Sets the offset.</summary>
        /// <param name="offset">The offset.</param>
        /// <param name="pad">The extra padding, if any.</param>
        public void SetOffset(CompositeOffset offset, float pad = 0f)
        {
            this.Offset = offset;
            if (pad != 0f)
            {
                this.LastThing.Clear();
                this.X += MathF.Round(pad);
                this.UnionBBoxHorizontal(this.X, this.X);
            }
        }

        /// <summary>Adds an object.</summary>
        /// <param name="fontData">The current font data.</param>
        /// <param name="recordIndex">The index of the record of this span, or -1 if none.</param>
        /// <param name="x0">The X0.</param>
        /// <param name="x1">The X1.</param>
        public void AddObject(SpanStyleFontData fontData, int recordIndex, float x0, float x1)
        {
            if (recordIndex == -1)
                this.LastThing.SetRecord(recordIndex);
            else
                this.LastThing.Clear();
            this.UnionBBoxHorizontal(
                this.X + x0 + fontData.ScaledHorizontalOffset,
                this.X + x1 + fontData.ScaledHorizontalOffset);
            this.X += MathF.Round(x1);
        }

        /// <summary>Adds a character.</summary>
        /// <param name="font">The font.</param>
        /// <param name="codepoint">The codepoint.</param>
        /// <param name="xy0">The scaled XY0.</param>
        /// <param name="xy1">The scaled XY1.</param>
        /// <param name="advance">The scaled advance width.</param>
        public void AddCharacter(
            in SpanStyleFontData font,
            int codepoint,
            Vector2 xy0,
            Vector2 xy1,
            float advance)
        {
            var xoff = this.X + font.ScaledHorizontalOffset;
            this.UnionBBoxHorizontal(
                MathF.Round(xoff + xy0.X),
                MathF.Round(xoff + xy1.X + font.GetScaledTopSkew(xy0) + font.BoldExtraWidth));
            this.UnionBBoxVertical(font.BBoxVertical.X, font.BBoxVertical.Y);
            this.X += MathF.Round(advance);
            this.LastThing.SetCodepoint(codepoint);
        }

        /// <summary>Adds a standard character.</summary>
        /// <param name="font">The font.</param>
        /// <param name="codepoint">The codepoint.</param>
        public void AddStandardCharacter(in SpanStyleFontData font, int codepoint)
        {
            ref readonly var glyph = ref font.GetEffectiveGlyph(codepoint);

            var adjust =
                this.LastThing.TryGetCodepoint(out var lastCodepoint)
                    ? font.GetScaledGap(lastCodepoint, glyph.Codepoint)
                    : 0;
            var boxOffset = new Vector2(adjust, 0);
            this.AddCharacter(
                font,
                codepoint,
                (glyph.XY0 * font.Scale) + boxOffset,
                (glyph.XY1 * font.Scale) + boxOffset,
                (glyph.AdvanceX * font.Scale) + adjust);
        }

        /// <summary>Adds a tab character, by aligning to the specified tab width.</summary>
        /// <param name="font">The font.</param>
        /// <param name="tabWidth">The width.</param>
        public void AddTabCharacter(in SpanStyleFontData font, float tabWidth)
        {
            this.X = MathF.Floor((this.X + tabWidth) / tabWidth) * tabWidth;
            this.UnionBBoxHorizontal(this.X, this.X);
            this.UnionBBoxVertical(font.BBoxVertical.X, font.BBoxVertical.Y);
            this.LastThing.SetCodepoint('\t');
        }

        /// <summary>Adds a soft hyphen character.</summary>
        /// <param name="font">The font.</param>
        public void AddSoftHyphenCharacter(in SpanStyleFontData font)
        {
            ref readonly var glyph = ref font.GetEffectiveGlyph(SoftHyphenReplacementChar);

            var adjust =
                this.LastThing.TryGetCodepoint(out var lastCodepoint)
                    ? font.GetScaledGap(lastCodepoint, glyph.Codepoint)
                    : 0;
            var boxOffset = new Vector2(adjust, 0);

            this.AddCharacter(
                font,
                glyph.Codepoint,
                (glyph.XY0 * font.Scale) + boxOffset,
                (glyph.XY1 * font.Scale) + boxOffset,
                0);
            this.LastThing.SetCodepoint(0xAD);
        }

        /// <summary>Stores information on what has been processed most recently.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 5)]
        public struct LastThingStruct
        {
            [FieldOffset(0)]
            private int value;

            [FieldOffset(4)]
            private byte type;

            /// <summary>Attempts to get the last codepoint.</summary>
            /// <param name="codepoint">The retrieved codepoint.</param>
            /// <returns><c>true</c> if it was a codepoint.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool TryGetCodepoint(out int codepoint)
            {
                if (this.type == 1)
                {
                    codepoint = this.value;
                    return true;
                }

                codepoint = 0;
                return false;
            }

            /// <summary>Tests if the struct contains the specified codepoint.</summary>
            /// <param name="codepoint">The codepoint to test.</param>
            /// <returns><c>true</c> if it is.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public readonly bool IsCodepoint(int codepoint) => this.value == codepoint && this.type == 1;

            /// <summary>Clears the thing.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() => this = default;

            /// <summary>Sets the last thing to a codepoint.</summary>
            /// <param name="codepoint">The codepoint.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetCodepoint(int codepoint)
            {
                this.value = codepoint;
                this.type = 1;
            }

            /// <summary>Sets the last thing to a record.</summary>
            /// <param name="recordIndex">The record index.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRecord(int recordIndex)
            {
                this.value = recordIndex;
                this.type = 2;
            }
        }
    }
}
