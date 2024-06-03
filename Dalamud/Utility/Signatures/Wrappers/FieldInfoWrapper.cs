using System.Reflection;

namespace Dalamud.Utility.Signatures.Wrappers;

/// <summary>
/// Class providing information about a field.
/// </summary>
internal sealed class FieldInfoWrapper : IFieldOrPropertyInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldInfoWrapper"/> class.
    /// </summary>
    /// <param name="info">FieldInfo to populate from.</param>
    public FieldInfoWrapper(FieldInfo info)
    {
        this.Info = info;
    }

    /// <inheritdoc/>
    public string Name => this.Info.Name;

    /// <inheritdoc/>
    public Type ActualType => this.Info.FieldType;

    /// <inheritdoc/>
    public bool IsNullable => NullabilityUtil.IsNullable(this.Info);

    private FieldInfo Info { get; }

    /// <inheritdoc/>
    public void SetValue(object? self, object? value)
    {
        this.Info.SetValue(self, value);
    }

    /// <inheritdoc/>
    public T? GetCustomAttribute<T>() where T : Attribute
    {
        return this.Info.GetCustomAttribute<T>();
    }
}
