using System;
using System.Reflection;

namespace Dalamud.IoC.Internal;

/// <summary>
/// An object instance registered in the <see cref="ServiceContainer"/>.
/// </summary>
internal class ObjectInstance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectInstance"/> class.
    /// </summary>
    /// <param name="instance">The underlying instance.</param>
    public ObjectInstance(object instance)
    {
        this.Instance = new WeakReference(instance);
        this.Version = instance.GetType().GetCustomAttribute<InterfaceVersionAttribute>();
    }

    /// <summary>
    /// Gets the current version of the instance, if it exists.
    /// </summary>
    public InterfaceVersionAttribute? Version { get; }

    /// <summary>
    /// Gets a reference to the underlying instance.
    /// </summary>
    public WeakReference Instance { get; }
}
