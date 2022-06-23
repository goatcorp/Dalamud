using System;

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
}
