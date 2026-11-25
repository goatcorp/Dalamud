using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>Describes where font data comes from and which face or variable instance to use.</summary>
/// <remarks><ul>
/// <li>Equal sources resolve to the same font. Files compare by case-insensitive full path, memory sources by content
/// hash, and manifest resources by assembly and resource name, along with face index and variable axes.</li>
/// <li>File and resource existence is checked lazily when the font is loaded.</li>
/// </ul></remarks>
/// <seealso cref="IUiBuilder.RegisterFont"/>
public readonly record struct FontSource
{
    private readonly EquatableArray<FontAxis> axes;
    private readonly WeakReference<Assembly>? assembly;

    private FontSource(
        Kinds kind, string key, string name, int fontNo, ImmutableArray<FontAxis> axes, string? path = null,
        byte[]? data = null, Assembly? assembly = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fontNo);
        this.Kind = kind;
        this.Key = key;
        this.Name = name;
        this.FontNo = fontNo;
        this.axes = new(FontAxis.Normalize(axes));
        this.Path = path;
        this.Data = data;
        this.assembly = assembly is null ? null : new(assembly);
    }

    /// <summary>The kinds of sources.</summary>
    internal enum Kinds
    {
        /// <summary>No source.</summary>
        None,

        /// <summary>A file on disk.</summary>
        File,

        /// <summary>Data in memory.</summary>
        Memory,

        /// <summary>A manifest resource of an assembly.</summary>
        ManifestResource,
    }

    /// <summary>Gets the index of the face in a font collection, such as a TTC file, or 0.</summary>
    public int FontNo { get; }

    /// <summary>Gets the axis values of an instance of a variable font, or empty for the default instance.</summary>
    /// <value>Sorted by tag, with one value per tag. Axes the font does not have are ignored.</value>
    public ImmutableArray<FontAxis> Axes => this.axes.Array;

    /// <summary>Gets the kind of the source.</summary>
    internal Kinds Kind { get; }

    /// <summary>Gets a value indicating whether this is the empty default value.</summary>
    internal bool IsDefault => this.Kind == Kinds.None;

    /// <summary>Gets the source identity key, excluding <see cref="FontNo"/> and <see cref="Axes"/>.</summary>
    internal string Key { get; }

    /// <summary>Gets the name of the file or resource, for display until the font's own name is read.</summary>
    internal string Name { get; }

    /// <summary>Gets the full path of a file.</summary>
    internal string? Path { get; }

    /// <summary>Gets the data of a memory source.</summary>
    internal byte[]? Data { get; }

    /// <summary>Gets the assembly of a resource, or null once it has been unloaded.</summary>
    internal Assembly? Assembly => this.assembly?.TryGetTarget(out var a) is true ? a : null;

    private string? AxesSuffix => this.Axes.IsEmpty ? null : $" {string.Join(' ', this.Axes)}";

    /// <summary>Gets a file source.</summary>
    /// <param name="path">Path of the file.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    public static implicit operator FontSource(string path) => FromFile(path);

    /// <summary>Creates a source from a font file (TTF, OTF, TTC).</summary>
    /// <param name="path">Path of the file. A relative path is taken from the current directory.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="axes">Axis values of an instance of a variable font, or default for the default instance.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="fontNo"/> is negative.</exception>
    /// <remarks>The file is read into memory lazily when loaded and may be re-read after cache eviction, so keep it on
    /// disk while registered. For temporary files, use <see cref="FromStream"/> or <see cref="FromMemory"/>.</remarks>
    public static FontSource FromFile(string path, int fontNo = 0, ImmutableArray<FontAxis> axes = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        string fullPath;
        try
        {
            fullPath = System.IO.Path.GetFullPath(path);
        }
        catch
        {
            fullPath = path;
        }

        return new(
            Kinds.File,
            $"file:{fullPath.ToLowerInvariant()}",
            System.IO.Path.GetFileNameWithoutExtension(fullPath),
            fontNo,
            axes,
            path: fullPath);
    }

    /// <summary>Creates a source from font data in a stream, read immediately from its current position to its end.</summary>
    /// <param name="stream">The stream. It is not closed or kept.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="axes">Axis values of an instance of a variable font, or default for the default instance.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="fontNo"/> is negative.</exception>
    /// <remarks>The data is copied and hashed, so keep the source rather than making it again.</remarks>
    public static FontSource FromStream(Stream stream, int fontNo = 0, ImmutableArray<FontAxis> axes = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return OfData(ms.ToArray(), fontNo, axes);
    }

    /// <summary>Creates a source from font data in memory.</summary>
    /// <param name="data">The data. It is copied now.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="axes">Axis values of an instance of a variable font, or default for the default instance.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="fontNo"/> is negative.</exception>
    /// <remarks><inheritdoc cref="FromStream" path="/remarks/node()"/></remarks>
    public static FontSource FromMemory(
        ReadOnlyMemory<byte> data, int fontNo = 0, ImmutableArray<FontAxis> axes = default) =>
        OfData(data.ToArray(), fontNo, axes);

    /// <summary>Creates a source from a font embedded in an assembly as a manifest resource.</summary>
    /// <param name="assembly">The assembly, held via a weak reference.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <param name="axes">Axis values of an instance of a variable font, or default for the default instance.</param>
    /// <returns>The source.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="assembly"/> or <paramref name="resourceName"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="fontNo"/> is negative.</exception>
    /// <remarks>The resource stream is opened lazily when the font is loaded.</remarks>
    public static FontSource FromManifestResource(
        Assembly assembly, string resourceName, int fontNo = 0, ImmutableArray<FontAxis> axes = default)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourceName);
        return new(
            Kinds.ManifestResource,
            $"resource:{assembly.GetName().Name}:{resourceName}",
            resourceName,
            fontNo,
            axes,
            assembly: assembly);
    }

    /// <inheritdoc/>
    public bool Equals(FontSource other) =>
        this.Key == other.Key && this.FontNo == other.FontNo && this.axes.Equals(other.axes);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(this.Key, this.FontNo, this.axes);

    /// <inheritdoc/>
    public override string ToString() => this.IsDefault ? "None" : $"{this.Key}#{this.FontNo}{this.AxesSuffix}";

    /// <summary>Gets a short name for debug display.</summary>
    /// <returns>The name of the file or resource, with the face index and axis values if any.</returns>
    internal string ToShortString() =>
        (this.FontNo == 0 ? this.Name : $"{this.Name}#{this.FontNo}") + this.AxesSuffix;

    /// <summary>Opens a read stream for this source.</summary>
    /// <returns>A new stream.</returns>
    /// <exception cref="FileNotFoundException">If the file or the resource does not exist.</exception>
    /// <exception cref="InvalidOperationException">If the assembly of a resource was unloaded, or the source is
    /// default.</exception>
    internal Stream Open() =>
        this.Kind switch
        {
            Kinds.File => File.OpenRead(this.Path!),
            Kinds.Memory => new MemoryStream(this.Data!, 0, this.Data!.Length, false, true),
            Kinds.ManifestResource => (this.Assembly ?? throw new InvalidOperationException(
                                           "The assembly of the font is not loaded."))
                                      .GetManifestResourceStream(this.Name)
                                      ?? throw new FileNotFoundException("The font resource does not exist.", this.Name),
            _ => throw new InvalidOperationException("There is no source."),
        };

    private static FontSource OfData(byte[] data, int fontNo, ImmutableArray<FontAxis> axes)
    {
        var hash = Convert.ToHexString(SHA256.HashData(data));
        return new(Kinds.Memory, $"memory:{data.Length}:{hash}", $"Font {hash[..8]}", fontNo, axes, data: data);
    }
}
