using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>Base class for <see cref="SpannedString"/> and <see cref="SpannedStringBuilder"/>.</summary>
public abstract partial class SpannedStringBase
{
    /// <summary>A reference of data contained within a spannable.</summary>
    private protected readonly ref struct DataRef
    {
        /// <summary>The text data.</summary>
        public readonly ReadOnlySpan<byte> TextStream;

        /// <summary>The link data.</summary>
        public readonly ReadOnlySpan<byte> DataStream;

        /// <summary>The span entity data.</summary>
        public readonly ReadOnlySpan<SpannedRecord> Records;

        /// <summary>The used font sets.</summary>
        public readonly ReadOnlySpan<FontHandleVariantSet> FontSets;

        /// <summary>The textures used.</summary>
        public readonly ReadOnlySpan<IDalamudTextureWrap?> Textures;

        /// <summary>The callbacks used.</summary>
        public readonly ReadOnlySpan<ISpannable?> Spannables;

        /// <summary>Initializes a new instance of the <see cref="DataRef"/> struct.</summary>
        /// <param name="textStream">The text data.</param>
        /// <param name="dataStream">The link data.</param>
        /// <param name="records">The span records.</param>
        /// <param name="fontSets">The font sets.</param>
        /// <param name="textures">The textures.</param>
        /// <param name="spannables">The callbacks.</param>
        public DataRef(
            ReadOnlySpan<byte> textStream,
            ReadOnlySpan<byte> dataStream,
            ReadOnlySpan<SpannedRecord> records,
            ReadOnlySpan<FontHandleVariantSet> fontSets,
            ReadOnlySpan<IDalamudTextureWrap?> textures,
            ReadOnlySpan<ISpannable?> spannables)
        {
            this.TextStream = textStream;
            this.DataStream = dataStream;
            this.Records = records;
            this.FontSets = fontSets;
            this.Textures = textures;
            this.Spannables = spannables;
        }

        /// <summary>Initializes a new instance of the <see cref="DataRef"/> struct.</summary>
        /// <param name="textStream">The text data.</param>
        public DataRef(ReadOnlySpan<byte> textStream) => this.TextStream = textStream;

        /// <summary>Gets the offset past the last text and span record.</summary>
        public CompositeOffset EndOffset => new(this.TextStream.Length, this.Records.Length);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new(this);

        /// <summary>Attempts to get the codepoint at the given offset.</summary>
        /// <param name="offset">The offset.</param>
        /// <param name="numSkipCodepoints">Number of codepoints to skip.</param>
        /// <param name="codepoint">The retrieved codepoint.</param>
        /// <returns><c>true</c> if retrieved.</returns>
        public bool TryGetCodepointAt(int offset, int numSkipCodepoints, out int codepoint)
        {
            if (offset < 0 || offset >= this.TextStream.Length || numSkipCodepoints < 0)
            {
                codepoint = 0;
                return false;
            }

            var from = this.TextStream[offset..];
            var v = default(UtfValue);
            while (numSkipCodepoints-- >= 0)
            {
                if (!UtfValue.TryDecode8(ref from, out v, out _))
                {
                    codepoint = 0;
                    return false;
                }
            }

            codepoint = v;
            return true;
        }

        /// <summary>Attempts to get a non-null texture for the given span entity.</summary>
        /// <param name="index">The index.</param>
        /// <param name="texture">The retrieved texture.</param>
        /// <returns><c>true</c> if a corresponding texture is retrieved.</returns>
        public bool TryGetTextureAt(int index, [NotNullWhen(true)] out IDalamudTextureWrap? texture)
        {
            texture = index < 0 || index >= this.Textures.Length ? null : this.Textures[index];
            return texture is not null;
        }

        /// <summary>Attempts to get a spannable for the given span entity.</summary>
        /// <param name="index">The index.</param>
        /// <param name="spannable">The retrieved spannable.</param>
        /// <returns><c>true</c> if a corresponding spannable is retrieved.</returns>
        public bool TryGetSpannableAt(int index, [NotNullWhen(true)] out ISpannable? spannable)
        {
            spannable = index < 0 || index >= this.Spannables.Length ? default : this.Spannables[index];
            return spannable != default;
        }

        /// <summary>Attempts to get the link contained in the given span record.</summary>
        /// <param name="recordIndex">The record index.</param>
        /// <param name="link">The retrieved link, if any.</param>
        /// <returns><c>true</c> if retrieved.</returns>
        public bool TryGetLinkAt(int recordIndex, out ReadOnlySpan<byte> link)
        {
            link = default;
            if (recordIndex < 0 || recordIndex >= this.Records.Length)
                return false;

            ref readonly var record = ref this.Records[recordIndex];
            if (record.Type != SpannedRecordType.Link)
                return false;

            return SpannedRecordCodec.TryDecodeLink(
                this.DataStream.Slice(record.DataStart, record.DataLength),
                out link);
        }

        /// <summary>Represents a segment during enumeration.</summary>
        public ref struct Segment
        {
            /// <summary>The data.</summary>
            public DataRef Data;

            /// <summary>The offset.</summary>
            public CompositeOffset Offset;

            /// <summary>
            /// Initializes a new instance of the <see cref="Segment"/> struct.
            /// </summary>
            /// <param name="data">The data.</param>
            /// <param name="textOffset">The text offset.</param>
            /// <param name="recordIndex">The record index.</param>
            public Segment(DataRef data, int textOffset, int recordIndex)
            {
                this.Data = data;
                this.Offset.Text = textOffset;
                this.Offset.Record = recordIndex;
            }

            /// <summary>Gets a value indicating whether this segment is a text segment.</summary>
            public readonly bool IsText
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this.Offset.Record >= this.Data.Records.Length
                       || this.Data.Records[this.Offset.Record].TextStart > this.Offset.Text;
            }

            /// <summary>Attempts to get the next segment.</summary>
            /// <param name="next">The retrieved next segment.</param>
            /// <returns><c>true</c> if the next segment is retrieved.</returns>
            public readonly bool TryGetNext(out Segment next)
            {
                next = this;
                if (next.Offset.Text == -1)
                {
                    next.Offset.Text = 0;
                    next.Offset.Record = 0;
                }
                else
                {
                    if (next.Offset.Record < next.Data.Records.Length)
                    {
                        ref readonly var rec = ref next.Data.Records[next.Offset.Record];
                        if (rec.TextStart <= this.Offset.Text)
                            next.Offset.Record++;
                        else
                            next.Offset.Text = rec.TextStart;
                    }
                    else
                    {
                        next.Offset.Text = next.Data.TextStream.Length;
                    }
                }

                return next.Offset.Text < next.Data.TextStream.Length || next.Offset.Record < next.Data.Records.Length;
            }

            /// <summary>Attempts to get the previous segment.</summary>
            /// <param name="prev">The retrieved previous segment.</param>
            /// <returns><c>true</c> if the previous segment is retrieved.</returns>
            public readonly bool TryGetPrevious(out Segment prev)
            {
                prev = this;
                if (prev.Offset is { Text: 0, Record: 0 }
                    || prev.Offset.Text < 0
                    || prev.Offset.Text > prev.Data.TextStream.Length
                    || prev.Offset.Record < 0
                    || prev.Offset.Record > prev.Data.Records.Length)
                    return false;

                if (prev.Offset.Record > 0)
                {
                    ref readonly var rec = ref prev.Data.Records[prev.Offset.Record - 1];
                    if (rec.TextStart >= this.Offset.Text)
                        prev.Offset.Record--;
                    else
                        prev.Offset.Text = rec.TextStart;
                }
                else
                {
                    prev.Offset.Text = 0;
                }

                return prev.Offset.Text < prev.Data.TextStream.Length || prev.Offset.Record < prev.Data.Records.Length;
            }

            /// <summary>Attempts to get the next text segment.</summary>
            /// <param name="next">The next text segment.</param>
            /// <returns><c>true</c> if next text segment is retrieved.</returns>
            public readonly bool TryGetNextText(out Segment next)
            {
                next = this;
                while (next.TryGetNext(out next))
                {
                    if (next.IsText)
                        return true;
                }

                return false;
            }

            /// <summary>Attempts to get the current record.</summary>
            /// <param name="record">The record.</param>
            /// <param name="data">The record data.</param>
            /// <returns><c>true</c> if retrieved.</returns>
            public readonly bool TryGetRecord(out SpannedRecord record, out ReadOnlySpan<byte> data)
            {
                if (this.IsText || this.Offset.Record >= this.Data.Records.Length)
                {
                    record = default;
                    data = default;
                    return false;
                }

                record = this.Data.Records[this.Offset.Record];
                data = this.Data.DataStream.Slice(record.DataStart, record.DataLength);
                return true;
            }

            /// <summary>Attempts to get the current raw text.</summary>
            /// <param name="text">The text.</param>
            /// <returns><c>true</c> if retrieved.</returns>
            public readonly bool TryGetRawText(out ReadOnlySpan<byte> text)
            {
                if (!this.IsText)
                {
                    text = default;
                    return false;
                }

                text = this.Offset.Record < this.Data.Records.Length
                           ? this.Data.TextStream[this.Offset.Text..this.Data.Records[this.Offset.Record].TextStart]
                           : this.Data.TextStream[this.Offset.Text..];
                return true;
            }
        }

        /// <summary>Enumerator for <see cref="DataRef"/>.</summary>
        public ref struct Enumerator
        {
            /// <summary>Initializes a new instance of the <see cref="Enumerator"/> struct.</summary>
            /// <param name="data">The data.</param>
            public Enumerator(DataRef data) => this.Current = new(data, -1, -1);

            /// <inheritdoc cref="IEnumerator{T}.Current"/>
            public Segment Current { get; private set; }

            /// <inheritdoc cref="IEnumerator.MoveNext"/>
            public bool MoveNext()
            {
                if (!this.Current.TryGetNext(out var next))
                    return false;
                this.Current = next;
                return true;
            }
        }
    }
}
