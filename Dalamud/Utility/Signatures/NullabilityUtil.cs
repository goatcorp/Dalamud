using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dalamud.Utility.Signatures;

/// <summary>
/// Class providing info about field, property or parameter nullability.
/// </summary>
internal static class NullabilityUtil
{
    /// <summary>
    /// Check if the provided property is nullable.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns>Whether or not the property is nullable.</returns>
    internal static bool IsNullable(PropertyInfo property) => IsNullableHelper(property.PropertyType, property.DeclaringType, property.CustomAttributes);

    /// <summary>
    /// Check if the provided field is nullable.
    /// </summary>
    /// <param name="field">The field to check.</param>
    /// <returns>Whether or not the field is nullable.</returns>
    internal static bool IsNullable(FieldInfo field) => IsNullableHelper(field.FieldType, field.DeclaringType, field.CustomAttributes);

    /// <summary>
    /// Check if the provided parameter is nullable.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>Whether or not the parameter is nullable.</returns>
    internal static bool IsNullable(ParameterInfo parameter) => IsNullableHelper(parameter.ParameterType, parameter.Member, parameter.CustomAttributes);

    private static bool IsNullableHelper(Type memberType, MemberInfo? declaringType, IEnumerable<CustomAttributeData> customAttributes)
    {
        if (memberType.IsValueType)
        {
            return Nullable.GetUnderlyingType(memberType) != null;
        }

        var nullable = customAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");
        if (nullable != null && nullable.ConstructorArguments.Count == 1)
        {
            var attributeArgument = nullable.ConstructorArguments[0];
            if (attributeArgument.ArgumentType == typeof(byte[]))
            {
                var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value!;
                if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                {
                    return (byte)args[0].Value! == 2;
                }
            }
            else if (attributeArgument.ArgumentType == typeof(byte))
            {
                return (byte)attributeArgument.Value! == 2;
            }
        }

        for (var type = declaringType; type != null; type = type.DeclaringType)
        {
            var context = type.CustomAttributes
                              .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");
            if (context != null &&
                context.ConstructorArguments.Count == 1 &&
                context.ConstructorArguments[0].ArgumentType == typeof(byte))
            {
                return (byte)context.ConstructorArguments[0].Value! == 2;
            }
        }

        // Couldn't find a suitable attribute
        return false;
    }
}
