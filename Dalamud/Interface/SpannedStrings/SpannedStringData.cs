using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.SpannedStrings.Internal;
using Dalamud.Interface.SpannedStrings.Styles;
using Dalamud.Utility.Text;

namespace Dalamud.Interface.SpannedStrings;

/// <summary>A reference of data contained within a spannable.</summary>
public readonly ref struct SpannedStringData
{
    /// <summary>The text data.</summary>
    private readonly ReadOnlySpan<byte> textStream;

    /// <summary>The link data.</summary>
    private readonly ReadOnlySpan<byte> dataStream;

    /// <summary>The span entity data.</summary>
    private readonly ReadOnlySpan<SpannedRecord> records;

    /// <summary>The used font sets.</summary>
    private readonly ReadOnlySpan<FontHandleVariantSet> fontSets;

    /// <summary>The textures used.</summary>
    private readonly ReadOnlySpan<IDalamudTextureWrap?> textures;

    /// <summary>The callbacks used.</summary>
    private readonly ReadOnlySpan<SpannedStringCallbackDelegate?> callbacks;

    // TODO: what's the best way to make this public, if we're to do that? 
    
    /// <summary>Initializes a new instance of the <see cref="SpannedStringData"/> struct.</summary>
    /// <param name="textStream">The text data.</param>
    /// <param name="dataStream">The link data.</param>
    /// <param name="records">The span records.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="textures">The textures.</param>
    /// <param name="callbacks">The callbacks.</param>
    internal SpannedStringData(
        ReadOnlySpan<byte> textStream,
        ReadOnlySpan<byte> dataStream,
        ReadOnlySpan<SpannedRecord> records,
        ReadOnlySpan<FontHandleVariantSet> fontSets,
        ReadOnlySpan<IDalamudTextureWrap?> textures,
        ReadOnlySpan<SpannedStringCallbackDelegate?> callbacks)
    {
        this.textStream = textStream;
        this.dataStream = dataStream;
        this.records = records;
        this.fontSets = fontSets;
        this.textures = textures;
        this.callbacks = callbacks;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Attempts to get the codepoint at the given offset.</summary>
    /// <param name="offset">The offset.</param>
    /// <param name="numSkipCodepoints">Number of codepoints to skip.</param>
    /// <param name="codepoint">The retrieved codepoint.</param>
    /// <returns><c>true</c> if retrieved.</returns>
    public bool TryGetCodepointAt(int offset, int numSkipCodepoints, out int codepoint)
    {
        if (offset < 0 || offset >= this.textStream.Length || numSkipCodepoints < 0)
        {
            codepoint = 0;
            return false;
        }

        var from = this.textStream[offset..];
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
        texture = index < 0 || index >= this.textures.Length ? null : this.textures[index];
        return texture is not null;
    }

    /// <summary>Attempts to get a font set for the given span entity.</summary>
    /// <param name="index">The index.</param>
    /// <param name="fontSet">The retrieved font set.</param>
    /// <returns><c>true</c> if a corresponding font set is retrieved.</returns>
    public bool TryGetFontSetAt(int index, out FontHandleVariantSet fontSet)
    {
        fontSet = index < 0 || index >= this.fontSets.Length ? default : this.fontSets[index];
        return fontSet != default;
    }

    /// <summary>Attempts to get a callback for the given span entity.</summary>
    /// <param name="index">The index.</param>
    /// <param name="callback">The retrieved callback.</param>
    /// <returns><c>true</c> if a corresponding callback is retrieved.</returns>
    public bool TryGetCallbackAt(int index, [NotNullWhen(true)] out SpannedStringCallbackDelegate? callback)
    {
        callback = index < 0 || index >= this.callbacks.Length ? default : this.callbacks[index];
        return callback != default;
    }

    /// <summary>Attempts to get the link contained in the given span record.</summary>
    /// <param name="recordIndex">The record index.</param>
    /// <param name="link">The retrieved link, if any.</param>
    /// <returns><c>true</c> if retrieved.</returns>
    public bool TryGetLinkAt(int recordIndex, out ReadOnlySpan<byte> link)
    {
        link = default;
        if (recordIndex < 0 || recordIndex >= this.records.Length)
            return false;

        ref readonly var record = ref this.records[recordIndex];
        if (record.Type != SpannedRecordType.Link)
            return false;

        return SpannedRecordCodec.TryDecodeLink(this.dataStream.Slice(record.DataStart, record.DataLength), out link);
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
            get => this.RecordIndex >= this.Data.records.Length
                   || this.Data.records[this.RecordIndex].TextStart > this.TextOffset;
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
                if (next.RecordIndex < next.Data.records.Length)
                {
                    ref readonly var rec = ref next.Data.records[next.RecordIndex];
                    if (rec.TextStart <= this.TextOffset)
                        next.RecordIndex++;
                    else
                        next.TextOffset = rec.TextStart;
                }
                else
                {
                    next.TextOffset = next.Data.textStream.Length;
                }
            }

            return next.TextOffset < next.Data.textStream.Length || next.RecordIndex < next.Data.records.Length;
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
        internal readonly bool TryGetRecord(out SpannedRecord record, out ReadOnlySpan<byte> data)
        {
            if (this.IsText || this.RecordIndex >= this.Data.records.Length)
            {
                record = default;
                data = default;
                return false;
            }

            record = this.Data.records[this.RecordIndex];
            data = this.Data.dataStream.Slice(record.DataStart, record.DataLength);
            return true;
        }

        /// <summary>Attempts to get the current raw text.</summary>
        /// <param name="text">The text.</param>
        /// <returns><c>true</c> if retrieved.</returns>
        internal readonly bool TryGetRawText(out ReadOnlySpan<byte> text)
        {
            if (!this.IsText)
            {
                text = default;
                return false;
            }

            text = this.RecordIndex < this.Data.records.Length
                       ? this.Data.textStream[this.TextOffset..this.Data.records[this.RecordIndex].TextStart]
                       : this.Data.textStream[this.TextOffset..];
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
