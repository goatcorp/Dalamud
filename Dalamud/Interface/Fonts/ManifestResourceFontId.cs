using System.Reflection;
using System.Runtime.CompilerServices;

using Dalamud.Interface.Fonts.Internal;

using Newtonsoft.Json;

namespace Dalamud.Interface.Fonts;

/// <summary>Represents a font embedded in an assembly as a manifest resource.</summary>
[JsonConverter(typeof(NotSerializableJsonConverter))]
public sealed class ManifestResourceFontId : SingleFontId
{
    private static readonly ConditionalWeakTable<Assembly, ConditionalWeakTable<string, ManifestResourceFontId>>
        Interned = new();

    private readonly WeakReference<Assembly> assembly;
    private readonly string assemblyName;
    private readonly int hashCode;

    private ManifestResourceFontId(Assembly assembly, string resourceName, int fontNo)
    {
        this.assembly = new(assembly);
        this.assemblyName = assembly.GetName().Name ?? string.Empty;
        this.ResourceName = resourceName;
        this.FontNo = fontNo;
        this.hashCode = HashCode.Combine(assembly, StringComparer.Ordinal.GetHashCode(resourceName), fontNo);
    }

    /// <summary>Gets the assembly the resource is in.</summary>
    /// <value>The assembly, or <see langword="null"/> once it has been unloaded.</value>
    public Assembly? Assembly => this.assembly.TryGetTarget(out var a) ? a : null;

    /// <summary>Gets the name of the resource.</summary>
    public string ResourceName { get; }

    /// <summary>Gets the index of the face in a font collection, or 0.</summary>
    public int FontNo { get; }

    /// <inheritdoc/>
    public override LocalizedName Name => field ??= new(this.ResourceName);

    /// <inheritdoc/>
    private protected override string ShortName =>
        this.FontNo == 0 ? this.ResourceName : $"{this.ResourceName}#{this.FontNo}";

    public static bool operator ==(ManifestResourceFontId? left, ManifestResourceFontId? right) =>
        Equals(left, right);

    public static bool operator !=(ManifestResourceFontId? left, ManifestResourceFontId? right) =>
        !Equals(left, right);

    /// <summary>Gets the id of a resource.</summary>
    /// <param name="assembly">The assembly the resource is in.</param>
    /// <param name="resourceName">The name of the resource.</param>
    /// <param name="fontNo">Index of the face in a collection.</param>
    /// <returns>The id. The same instance for the same assembly and name instance, for face 0.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="assembly"/> or <paramref name="resourceName"/> is
    /// <see langword="null"/>.</exception>
    /// <remarks>Cheap enough to call every frame, with a name kept in a field or a literal. Whether the resource
    /// exists is checked when the font is made.</remarks>
    public static ManifestResourceFontId Of(Assembly assembly, string resourceName, int fontNo = 0)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(resourceName);
        return fontNo != 0
                   ? new(assembly, resourceName, fontNo)
                   : Interned.GetOrAdd(assembly, static _ => new())!
                             .GetOrAdd(resourceName, static (name, asm) => new(asm, name, 0), assembly)!;
    }

    /// <inheritdoc/>
    /// <remarks>Ids of an unloaded assembly are equal only to themselves.</remarks>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is ManifestResourceFontId other
            && this.hashCode == other.hashCode
            && this.ResourceName == other.ResourceName
            && this.FontNo == other.FontNo
            && this.Assembly is { } a
            && a == other.Assembly);

    /// <inheritdoc/>
    public override int GetHashCode() => this.hashCode;

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(ManifestResourceFontId)}:{this.assemblyName}:{this.ResourceName}";
}
