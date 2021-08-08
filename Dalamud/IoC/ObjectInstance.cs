using System;
using System.Reflection;

namespace Dalamud.IoC
{
    internal class ObjectInstance
    {
        public InterfaceVersionAttribute? Version { get; }

        public WeakReference Instance { get; }

        public ObjectInstance(object instance)
        {
            this.Instance = new WeakReference(instance);

            var type = instance.GetType();
            if (type.GetCustomAttribute(typeof(InterfaceVersionAttribute)) is InterfaceVersionAttribute attr)
            {
                this.Version = attr;
            }
        }
    }
}
