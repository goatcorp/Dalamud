namespace Dalamud.Utility.Signatures.Wrappers;

/// <summary>
/// Interface providing information about a field or a property.
/// </summary>
internal interface IFieldOrPropertyInfo
{
    /// <summary>
    /// Gets the name of the field or property.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the actual type of the field or property.
    /// </summary>
    Type ActualType { get; }

    /// <summary>
    /// Gets a value indicating whether or not the field or property is nullable.
    /// </summary>
    bool IsNullable { get; }

    /// <summary>
    /// Set this field or property's value.
    /// </summary>
    /// <param name="self">The object instance.</param>
    /// <param name="value">The value to set.</param>
    void SetValue(object? self, object? value);

    /// <summary>
    /// Get a custom attribute.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <returns>The attribute.</returns>
    T? GetCustomAttribute<T>() where T : Attribute;
}
