using System.Reflection;
using System.Threading.Tasks;

namespace Dalamud.IoC.Internal;

/// <summary>
/// An object instance registered in the <see cref="ServiceContainer"/>.
/// </summary>
internal class ObjectInstance
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectInstance"/> class.
    /// </summary>
    /// <param name="instanceTask">Weak reference to the underlying instance.</param>
    /// <param name="type">Type of the underlying instance.</param>
    public ObjectInstance(Task<WeakReference> instanceTask, Type type)
    {
        this.InstanceTask = instanceTask;
        this.Version = type.GetCustomAttribute<InterfaceVersionAttribute>();
    }

    /// <summary>
    /// Gets the current version of the instance, if it exists.
    /// </summary>
    public InterfaceVersionAttribute? Version { get; }

    /// <summary>
    /// Gets a reference to the underlying instance.
    /// </summary>
    /// <returns>The underlying instance.</returns>
    public Task<WeakReference> InstanceTask { get; }
}
