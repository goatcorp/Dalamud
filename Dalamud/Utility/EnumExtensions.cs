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
    public static TAttribute? GetAttribute<TAttribute>(this Enum value)
        where TAttribute : Attribute
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name.IsNullOrEmpty())
            return null;

        return type.GetField(name)?
                   .GetCustomAttributes(false)
                   .OfType<TAttribute>()
                   .SingleOrDefault();
    }

    /// <summary>
    /// Gets an indicator if enum has been flagged as obsolete (deprecated).
    /// </summary>
    /// <param name="value">The enum value that has an attached attribute.</param>
    /// <returns>Indicator if enum has been flagged as obsolete.</returns>
    public static bool IsObsolete(this Enum value)
    {
        return GetAttribute<ObsoleteAttribute>(value) != null;
    }
}
