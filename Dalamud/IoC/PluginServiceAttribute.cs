using System;

namespace Dalamud.IoC
{
    /// <summary>
    /// This attribute indicates whether an applicable service should be injected into the plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginServiceAttribute : Attribute
    {
    }
}
