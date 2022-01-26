using System;
using System.Reflection;

namespace Dalamud.Utility.Signatures.Wrappers
{
    internal sealed class FieldInfoWrapper : IFieldOrPropertyInfo
    {
        public FieldInfoWrapper(FieldInfo info)
        {
            this.Info = info;
        }

        public string Name => this.Info.Name;

        public Type ActualType => this.Info.FieldType;

        public bool IsNullable => NullabilityUtil.IsNullable(this.Info);

        private FieldInfo Info { get; }

        public void SetValue(object? self, object? value)
        {
            this.Info.SetValue(self, value);
        }

        public T? GetCustomAttribute<T>() where T : Attribute
        {
            return this.Info.GetCustomAttribute<T>();
        }
    }
}
