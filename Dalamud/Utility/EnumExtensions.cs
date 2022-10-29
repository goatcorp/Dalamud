using System;
using System.Linq;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Gets an attribute on an enum.
    /// </summary>
    /// <typeparam name="TAttribute">The type of attribute to get.</typeparam>
    /// <param name="value">The enum value that has an attached attribute.</param>
    /// <returns>The attached attribute, if any.</returns>
    public static TAttribute GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        return type.GetField(name) // I prefer to get attributes this way
                   .GetCustomAttributes(false)
                   .OfType<TAttribute>()
                   .SingleOrDefault();
    }
}
