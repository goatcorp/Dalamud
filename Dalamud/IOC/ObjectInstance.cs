using System.Reflection;

namespace Dalamud.IOC
{
    internal class ObjectInstance
    {
        public DependencyVersionAttribute? Version { get; }

        public object Instance { get; }

        public ObjectInstance(object instance)
        {
            Instance = instance;

            var type = instance.GetType();
            if (type.GetCustomAttribute(typeof(DependencyVersionAttribute)) is DependencyVersionAttribute attr)
            {
                Version = attr;
            }
        }
    }
}
