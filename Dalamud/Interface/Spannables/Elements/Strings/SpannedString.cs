using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Dalamud.Interface.Internal;
using Dalamud.Interface.Spannables.Elements.Strings.Internal;
using Dalamud.Interface.Spannables.Internal;
using Dalamud.Interface.Spannables.Styles;

namespace Dalamud.Interface.Spannables.Elements.Strings;

/// <summary>A UTF-8 character sequence with embedded styling information.</summary>
public sealed partial class SpannedString : SpannedStringBase, ISpanParsable<SpannedString>
{
    private static readonly (MethodInfo Info, SpannedParseInstructionAttribute Attr)[] SsbMethods =
        typeof(ISpannedStringBuilder)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
            .Select(
                x => (
                         Info: typeof(SpannedStringBuilder).GetMethod(
                             x.Name,
                             BindingFlags.Instance | BindingFlags.Public,
                             x.GetParameters().Select(y => y.ParameterType).ToArray()),
                         Attr: x.GetCustomAttribute<SpannedParseInstructionAttribute>()))
            .Where(x => x.Attr is not null)
            .OrderBy(x => x.Info.Name)
            .ThenByDescending(x => x.Info.GetParameters().Length)
            .ToArray();

    private readonly byte[] textStream;
    private readonly byte[] dataStream;
    private readonly SpannedRecord[] records;
    private readonly FontHandleVariantSet[] fontSets;
    private readonly IDalamudTextureWrap?[] textures;
    private readonly ISpannable?[] spannables;

    /// <summary>Initializes a new instance of the <see cref="SpannedString"/> class.</summary>
    /// <param name="data">The plain text.</param>
    public SpannedString(ReadOnlySpan<char> data)
    {
        this.textStream = new byte[Encoding.UTF8.GetByteCount(data)];
        Encoding.UTF8.GetBytes(data, this.textStream);
        this.dataStream = Array.Empty<byte>();
        this.records = Array.Empty<SpannedRecord>();
        this.fontSets = Array.Empty<FontHandleVariantSet>();
        this.textures = Array.Empty<IDalamudTextureWrap>();
        this.spannables = Array.Empty<ISpannable>();
    }

    /// <summary>Initializes a new instance of the <see cref="SpannedString"/> class.</summary>
    /// <param name="data">The plain UTF-8 text.</param>
    public SpannedString(ReadOnlySpan<byte> data)
    {
        this.textStream = data.ToArray();
        this.dataStream = Array.Empty<byte>();
        this.records = Array.Empty<SpannedRecord>();
        this.fontSets = Array.Empty<FontHandleVariantSet>();
        this.textures = Array.Empty<IDalamudTextureWrap>();
        this.spannables = Array.Empty<ISpannable>();
    }

    /// <summary>Initializes a new instance of the <see cref="SpannedString"/> class.</summary>
    /// <param name="data">The plain UTF-8 text.</param>
    public SpannedString(byte[] data)
    {
        this.textStream = data;
        this.dataStream = Array.Empty<byte>();
        this.records = Array.Empty<SpannedRecord>();
        this.fontSets = Array.Empty<FontHandleVariantSet>();
        this.textures = Array.Empty<IDalamudTextureWrap>();
        this.spannables = Array.Empty<ISpannable>();
    }

    /// <summary>Initializes a new instance of the <see cref="SpannedString"/> class.</summary>
    /// <param name="textStream">The text storage.</param>
    /// <param name="dataStream">The link strorage.</param>
    /// <param name="records">The spans.</param>
    /// <param name="fontSets">The font sets.</param>
    /// <param name="textures">The textures.</param>
    /// <param name="spannables">The callbacks.</param>
    internal SpannedString(
        byte[] textStream,
        byte[] dataStream,
        SpannedRecord[] records,
        FontHandleVariantSet[] fontSets,
        IDalamudTextureWrap?[] textures,
        ISpannable?[] spannables)
    {
        this.textStream = textStream;
        this.dataStream = dataStream;
        this.records = records;
        this.fontSets = fontSets;
        this.textures = textures;
        this.spannables = spannables;
    }

    /// <summary>Gets an empty instance of <see cref="SpannedString"/>.</summary>
    public static SpannedString Empty { get; } = new(Array.Empty<byte>());

    /// <summary>Gets the font handle sets.</summary>
    public IList<FontHandleVariantSet> FontHandleSets => this.fontSets;

    /// <summary>Gets the textures.</summary>
    public IList<IDalamudTextureWrap?> Textures => this.textures;

    /// <summary>Gets the callbacks.</summary>
    public IList<ISpannable?> Spannables => this.spannables;

    /// <inheritdoc/>
    public override string ToString() => this.ToString(null);

    /// <inheritdoc/>
    private protected override DataRef GetData() =>
        new(this.textStream, this.dataStream, this.records, this.fontSets, this.textures, this.spannables);
}
