using System;

using JetBrains.Annotations;

namespace Dalamud
{
    /// <summary>
    /// Marker interface for indicating the class is a service.
    /// </summary>
    internal interface IServiceObject
    {
    }

    /// <summary>
    /// Marker interface for indicating the class will be provided by outside source.
    /// </summary>
    internal interface IProvidedServiceObject : IServiceObject
    {
    }

    /// <summary>
    /// Marker interface for indicating the class is a service and should be preloaded.
    /// </summary>
    internal interface IEarlyLoadableServiceObject : IServiceObject
    {
    }

    /// <summary>
    /// This attribute indicates whether an applicable service should be injected into the plugin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class ServiceAttribute : Attribute
    {
    }
}
