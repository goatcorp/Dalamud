using System;
using System.Reflection;

namespace Dalamud.Utility.Signatures.Wrappers
{
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

        public string Name => this.Info.Name;

        public Type ActualType => this.Info.PropertyType;

        public bool IsNullable => NullabilityUtil.IsNullable(this.Info);

        private PropertyInfo Info { get; }

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
