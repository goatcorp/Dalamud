using System;

namespace Dalamud.IoC
{
    /// <summary>
    /// Whether the decorated class should be exposed to plugins via IOC.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginInterfaceAttribute : Attribute
    {
    }
}
