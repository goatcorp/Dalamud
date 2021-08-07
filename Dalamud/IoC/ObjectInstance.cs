using System;
using System.Reflection;

namespace Dalamud.IoC
{
    internal class ObjectInstance
    {
        public DependencyVersionAttribute? Version { get; }

        public WeakReference Instance { get; }

        public ObjectInstance(object instance)
        {
            Instance = new WeakReference(instance);

            var type = instance.GetType();
            if (type.GetCustomAttribute(typeof(DependencyVersionAttribute)) is DependencyVersionAttribute attr)
            {
                Version = attr;
            }
        }
    }
}
