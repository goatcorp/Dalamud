using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Strings.Internal;
using Dalamud.Interface.Spannables.Styles;
using Dalamud.Utility;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Strings;

/// <summary>A custom text renderer implementation.</summary>
public sealed partial class SpannedStringBuilder
    : SpannedStringBase, ISpannedStringBuilder, IResettable
{
    private readonly MemoryStream textStream = new();
    private readonly MemoryStream dataStream = new();
    private readonly List<SpannedRecord> records = new();
    private readonly List<FontHandleVariantSet> fontSets = new();
    private readonly List<IDalamudTextureWrap?> textures = new();
    private readonly List<ISpannable?> spannables;

    private Stack<int>? stackLink;
    private Stack<int>? stackFontSize;
    private Stack<int>? stackFont;
    private Stack<int>? stackLineHeight;
    private Stack<int>? stackHorizontalOffset;
    private Stack<int>? stackHorizontalAlignment;
    private Stack<int>? stackVerticalOffset;
    private Stack<int>? stackVerticalAlignment;
    private Stack<BoolOrToggle>? stackItalicMode;
    private Stack<BoolOrToggle>? stackBoldMode;
    private Stack<int>? stackTextDecoration;
    private Stack<int>? stackTextDecorationStyle;
    private Stack<int>? stackBackColor;
    private Stack<int>? stackShadowColor;
    private Stack<int>? stackEdgeColor;
    private Stack<int>? stackTextDecorationColor;
    private Stack<int>? stackForeCoor;
    private Stack<int>? stackBorderWidth;
    private Stack<int>? stackShadowOffset;
    private Stack<int>? stackTextDecorationThickness;

    /// <summary>Initializes a new instance of the <see cref="SpannedStringBuilder"/> class.</summary>
    public SpannedStringBuilder()
        : base(new List<ISpannable?>())
    {
        this.spannables = (List<ISpannable>)this.Children;
        this.records.Add(default);
    }

    /// <inheritdoc/>
    public SpannedString Build() =>
        new(
            this.textStream.ToArray(),
            this.dataStream.ToArray(),
            this.records.ToArray(),
            this.fontSets.ToArray(),
            this.textures.ToArray(),
            this.spannables.ToArray());

    /// <inheritdoc/>
    public SpannedStringBuilder Clear()
    {
        this.textStream.Clear();
        this.dataStream.Clear();
        this.records.Clear();
        this.fontSets.Clear();
        this.textures.Clear();
        this.spannables.Clear();

        this.stackLink?.Clear();
        this.stackFontSize?.Clear();
        this.stackFont?.Clear();
        this.stackLineHeight?.Clear();
        this.stackHorizontalOffset?.Clear();
        this.stackHorizontalAlignment?.Clear();
        this.stackVerticalOffset?.Clear();
        this.stackVerticalAlignment?.Clear();
        this.stackItalicMode?.Clear();
        this.stackBoldMode?.Clear();
        this.stackTextDecoration?.Clear();
        this.stackTextDecorationStyle?.Clear();
        this.stackBackColor?.Clear();
        this.stackShadowColor?.Clear();
        this.stackEdgeColor?.Clear();
        this.stackTextDecorationColor?.Clear();
        this.stackForeCoor?.Clear();
        this.stackBorderWidth?.Clear();
        this.stackShadowOffset?.Clear();
        this.stackTextDecorationThickness?.Clear();
        return this;
    }

    /// <inheritdoc/>
    bool IResettable.TryReset()
    {
        this.Clear();
        return true;
    }

    /// <inheritdoc/>
    private protected override DataRef GetData() =>
        new(
            this.textStream.GetDataSpan(),
            this.dataStream.GetDataSpan(),
            CollectionsMarshal.AsSpan(this.records),
            CollectionsMarshal.AsSpan(this.fontSets),
            CollectionsMarshal.AsSpan(this.textures),
            CollectionsMarshal.AsSpan(this.spannables));

    /// <summary>Reserves an area of bytes at the end of <see cref="textStream"/>.</summary>
    /// <param name="numBytes">The number of bytes.</param>
    /// <returns>The working area.</returns>
    private Span<byte> ReserveBytes(int numBytes)
    {
        var off = unchecked((int)this.textStream.Length);
        this.textStream.SetLength(off + numBytes);
        this.textStream.Position = off + numBytes;
        return this.textStream.GetBuffer().AsSpan(off, numBytes);
    }

    /// <summary>Adds a record, and reserves an area of bytes at the end of <see cref="dataStream"/>.</summary>
    /// <param name="type">The type of the record.</param>
    /// <param name="dataLength">The number of bytes for the data to reseserve.</param>
    /// <param name="reservedData">The reserved data.</param>
    /// <returns>The index of the added record.</returns>
    private int AddRecordAndReserveData(
        SpannedRecordType type,
        int dataLength,
        out Span<byte> reservedData)
    {
        var textStart = unchecked((int)this.textStream.Length);
        var dataStart = dataLength == 0 ? 0 : unchecked((int)this.dataStream.Length);
        this.records.Add(new(textStart, dataStart, dataLength, type));
        if (dataLength > 0)
        {
            this.dataStream.SetLength(this.dataStream.Position = dataStart + dataLength);
            reservedData = this.dataStream.GetBuffer().AsSpan(dataStart, dataLength);
        }
        else
        {
            reservedData = default;
        }

        return this.records.Count - 1;
    }

    /// <summary>Adds a copy of the record, only altering the offsets.</summary>
    private void AddRecordCopy(int recordIndex)
    {
        ref var rec = ref CollectionsMarshal.AsSpan(this.records)[recordIndex];
        var textStart = unchecked((int)this.textStream.Length);
        this.records.Add(new(textStart, rec.DataStart, rec.DataLength, rec.Type));
    }

    /// <summary>Adds a record that instructs to revert to the initial state.</summary>
    /// <param name="type">The type of the record.</param>
    private void AddRecordRevert(SpannedRecordType type)
    {
        var textStart = unchecked((int)this.textStream.Length);
        this.records.Add(new(textStart, 0, 0, type, true));
    }
}
