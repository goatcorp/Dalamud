using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Styles;

namespace Dalamud.Interface.Spannables.Text.Internal;

/// <summary>Memory references to the underlying data.</summary>
internal readonly struct TsDataMemory
{
    /// <summary>The text data.</summary>
    public readonly ReadOnlyMemory<byte> TextStream;

    /// <summary>The link data.</summary>
    public readonly ReadOnlyMemory<byte> DataStream;

    /// <summary>The Memory entity data.</summary>
    public readonly ReadOnlyMemory<SpannedRecord> Records;

    /// <summary>The used font sets.</summary>
    public readonly ReadOnlyMemory<FontHandleVariantSet> FontSets;

    /// <summary>The textures used.</summary>
    public readonly ReadOnlyMemory<IDalamudTextureWrap?> Textures;

    /// <summary>The callbacks used.</summary>
    public readonly ReadOnlyMemory<ISpannableTemplate?> Children;

    /// <summary>Initializes a new instance of the <see cref="TsDataMemory"/> struct.</summary>
    /// <param name="textStream">The text data.</param>
    /// <param name="dataStream">The link data.</param>
    /// <param name="records">The Memory records.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="textures">The textures.</param>
    /// <param name="children">The callbacks.</param>
    public TsDataMemory(
        ReadOnlyMemory<byte> textStream,
        ReadOnlyMemory<byte> dataStream,
        ReadOnlyMemory<SpannedRecord> records,
        ReadOnlyMemory<FontHandleVariantSet> fontSets,
        ReadOnlyMemory<IDalamudTextureWrap?> textures,
        ReadOnlyMemory<ISpannableTemplate?> children)
    {
        this.TextStream = textStream;
        this.DataStream = dataStream;
        this.Records = records;
        this.FontSets = fontSets;
        this.Textures = textures;
        this.Children = children;
    }

    /// <summary>Gets <see cref="TsDataSpan"/> from this <see cref="TsDataMemory"/>.</summary>
    /// <returns>A <see cref="TsDataSpan"/> corresponding to this <see cref="TsDataMemory"/>.</returns>
    public TsDataSpan AsSpan() => new(
        this.TextStream.Span,
        this.DataStream.Span,
        this.Records.Span,
        this.FontSets.Span,
        this.Textures.Span,
        this.Children.Span);
}
