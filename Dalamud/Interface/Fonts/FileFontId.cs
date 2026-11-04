using System.Runtime.CompilerServices;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>Represents a font from a file on the filesystem.</summary>
public sealed class FileFontId : SingleFontId
{
    private static readonly ConditionalWeakTable<string, FileFontId> Interned = new();

    private readonly string fullPath;
    private readonly int hashCode;

    /// <summary>Initializes a new instance of the <see cref="FileFontId"/> class.</summary>
    /// <param name="path">Path of the font file.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    [JsonConstructor]
    internal FileFontId(string? path, int fontNo = 0)
    {
        this.Path = path ?? string.Empty;
        this.FontNo = fontNo;
        try
        {
            this.fullPath = System.IO.Path.GetFullPath(this.Path);
        }
        catch
        {
            // Not a valid path. It is compared as it is, and fails to load.
            this.fullPath = this.Path;
        }

        this.hashCode = HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(this.fullPath), fontNo);
    }

    /// <summary>Gets the path of the font file.</summary>
    [JsonProperty]
    public string Path { get; }

    /// <summary>Gets the index of the face in a font collection file (such as TTC), or 0.</summary>
    [JsonProperty]
    public int FontNo { get; }

    /// <inheritdoc/>
    [JsonIgnore]
    [field: JsonIgnore]
    public override LocalizedName Name => field ??= new(System.IO.Path.GetFileNameWithoutExtension(this.Path));

    /// <inheritdoc/>
    private protected override string ShortName =>
        this.FontNo == 0
            ? System.IO.Path.GetFileName(this.Path)
            : $"{System.IO.Path.GetFileName(this.Path)}#{this.FontNo}";

    public static bool operator ==(FileFontId? left, FileFontId? right) => Equals(left, right);

    public static bool operator !=(FileFontId? left, FileFontId? right) => !Equals(left, right);

    /// <summary>Gets the id of a font file.</summary>
    /// <param name="path">Path of the font file. A relative path is taken from the current directory of when its id
    /// was first made.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <returns>The id. The same instance for the same path instance, for face 0.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <remarks>Cheap enough to call every frame with a path kept in a field or a literal. Whether the file exists is
    /// checked when the font is made.</remarks>
    public static FileFontId Of(string path, int fontNo = 0)
    {
        ArgumentNullException.ThrowIfNull(path);
        return fontNo != 0 ? new(path, fontNo) : Interned.GetOrAdd(path, static p => new(p));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is FileFontId other
            && this.hashCode == other.hashCode
            && this.FontNo == other.FontNo
            && string.Equals(this.fullPath, other.fullPath, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public override int GetHashCode() => this.hashCode;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(FileFontId)}:{this.Path}#{this.FontNo}";
}
