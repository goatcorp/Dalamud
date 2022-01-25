using System;

namespace Dalamud.Utility.Signatures.Wrappers
{
    internal interface IFieldOrPropertyInfo
    {
        string Name { get; }

        Type ActualType { get; }

        bool IsNullable { get; }

        void SetValue(object? self, object? value);

        T? GetCustomAttribute<T>() where T : Attribute;
    }
}
