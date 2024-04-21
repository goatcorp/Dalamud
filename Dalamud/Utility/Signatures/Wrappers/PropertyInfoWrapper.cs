using System.Reflection;

namespace Dalamud.Utility.Signatures.Wrappers;

/// <summary>
/// Class providing information about a property.
/// </summary>
internal sealed class PropertyInfoWrapper : IFieldOrPropertyInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyInfoWrapper"/> class.
    /// </summary>
    /// <param name="info">PropertyInfo.</param>
    public PropertyInfoWrapper(PropertyInfo info)
    {
        this.Info = info;
    }

    /// <inheritdoc/>
    public string Name => this.Info.Name;

    /// <inheritdoc/>
    public Type ActualType => this.Info.PropertyType;

    /// <inheritdoc/>
    public bool IsNullable => NullabilityUtil.IsNullable(this.Info);

    private PropertyInfo Info { get; }

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
