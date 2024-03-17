using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Styles;

namespace Dalamud.Interface.SpannedStrings.Internal;

/// <summary>A reference of data contained within a spannable.</summary>
internal ref struct SpannedStringData
{
    /// <summary>The text data.</summary>
    public ReadOnlySpan<byte> TextStream;

    /// <summary>The link data.</summary>
    public ReadOnlySpan<byte> DataStream;

    /// <summary>The span entity data.</summary>
    public ReadOnlySpan<SpannedRecord> Records;

    /// <summary>The used font sets.</summary>
    public ReadOnlySpan<FontHandleVariantSet> FontSets;

    /// <summary>The textures used.</summary>
    public ReadOnlySpan<IDalamudTextureWrap?> Textures;

    /// <summary>The callbacks used.</summary>
    public ReadOnlySpan<SpannedStringCallbackDelegate?> Callbacks;

    /// <summary>Initializes a new instance of the <see cref="SpannedStringData"/> struct.</summary>
    /// <param name="textStream">The text data.</param>
    /// <param name="dataStream">The link data.</param>
    /// <param name="records">The span records.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="textures">The textures.</param>
    /// <param name="callbacks">The callbacks.</param>
    public SpannedStringData(
        ReadOnlySpan<byte> textStream,
        ReadOnlySpan<byte> dataStream,
        ReadOnlySpan<SpannedRecord> records,
        ReadOnlySpan<FontHandleVariantSet> fontSets,
        ReadOnlySpan<IDalamudTextureWrap?> textures,
        ReadOnlySpan<SpannedStringCallbackDelegate?> callbacks)
    {
        this.TextStream = textStream;
        this.DataStream = dataStream;
        this.Records = records;
        this.FontSets = fontSets;
        this.Textures = textures;
        this.Callbacks = callbacks;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Attempts to get a non-null texture for the given span entity.</summary>
    /// <param name="index">The index.</param>
    /// <param name="texture">The retrieved texture.</param>
    /// <returns><c>true</c> if a corresponding texture is retrieved.</returns>
    public readonly bool TryGetTextureAt(int index, [NotNullWhen(true)] out IDalamudTextureWrap? texture)
    {
        texture = index < 0 || index >= this.Textures.Length ? null : this.Textures[index];
        return texture is not null;
    }

    /// <summary>Attempts to get a font set for the given span entity.</summary>
    /// <param name="index">The index.</param>
    /// <param name="fontSet">The retrieved font set.</param>
    /// <returns><c>true</c> if a corresponding font set is retrieved.</returns>
    public readonly bool TryGetFontSetAt(int index, out FontHandleVariantSet fontSet)
    {
        fontSet = index < 0 || index >= this.FontSets.Length ? default : this.FontSets[index];
        return fontSet != default;
    }

    /// <summary>Attempts to get a callback for the given span entity.</summary>
    /// <param name="index">The index.</param>
    /// <param name="callback">The retrieved callback.</param>
    /// <returns><c>true</c> if a corresponding callback is retrieved.</returns>
    public readonly bool TryGetCallbackAt(int index, [NotNullWhen(true)] out SpannedStringCallbackDelegate? callback)
    {
        callback = index < 0 || index >= this.Callbacks.Length ? default : this.Callbacks[index];
        return callback != default;
    }

    /// <summary>Represents a segment during enumeration.</summary>
    public ref struct Segment
    {
        /// <summary>The data.</summary>
        public SpannedStringData Data;

        /// <summary>The starting text offset.</summary>
        public int TextOffset;

        /// <summary>The starting record index.</summary>
        public int RecordIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Segment"/> struct.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="textOffset">The text offset.</param>
        /// <param name="recordIndex">The record index.</param>
        public Segment(SpannedStringData data, int textOffset, int recordIndex)
        {
            this.Data = data;
            this.TextOffset = textOffset;
            this.RecordIndex = recordIndex;
        }

        /// <summary>Gets a value indicating whether this segment is a text segment.</summary>
        public readonly bool IsText
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.RecordIndex >= this.Data.Records.Length
                   || this.Data.Records[this.RecordIndex].TextStart > this.TextOffset;
        }

        /// <summary>Attempts to get the next segment.</summary>
        /// <param name="next">The retrieved next segment.</param>
        /// <returns><c>true</c> if the next segment is retrieved.</returns>
        public readonly bool TryGetNext(out Segment next)
        {
            next = this;
            if (next.TextOffset == -1)
            {
                next.TextOffset = 0;
                next.RecordIndex = 0;
            }
            else
            {
                if (next.RecordIndex < next.Data.Records.Length)
                {
                    ref readonly var rec = ref next.Data.Records[next.RecordIndex];
                    if (rec.TextStart <= this.TextOffset)
                        next.RecordIndex++;
                    else
                        next.TextOffset = rec.TextStart;
                }
                else
                {
                    next.TextOffset = next.Data.TextStream.Length;
                }
            }

            return next.TextOffset < next.Data.TextStream.Length || next.RecordIndex < next.Data.Records.Length;
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
            if (this.IsText || this.RecordIndex >= this.Data.Records.Length)
            {
                record = default;
                data = default;
                return false;
            }

            record = this.Data.Records[this.RecordIndex];
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

            text = this.RecordIndex < this.Data.Records.Length
                       ? this.Data.TextStream[this.TextOffset..this.Data.Records[this.RecordIndex].TextStart]
                       : this.Data.TextStream[this.TextOffset..];
            return true;
        }
    }

    /// <summary>Enumerator for <see cref="SpannedStringData"/>.</summary>
    public ref struct Enumerator
    {
        /// <summary>Initializes a new instance of the <see cref="Enumerator"/> struct.</summary>
        /// <param name="data">The data.</param>
        public Enumerator(SpannedStringData data) => this.Current = new(data, -1, -1);

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
