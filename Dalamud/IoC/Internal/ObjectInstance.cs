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
    /// <param name="visibility">The visibility of this instance.</param>
    public ObjectInstance(Task<WeakReference> instanceTask, Type type, ObjectInstanceVisibility visibility)
    {
        this.InstanceTask = instanceTask;
        this.Visibility = visibility;
    }

    /// <summary>
    /// Gets a reference to the underlying instance.
    /// </summary>
    /// <returns>The underlying instance.</returns>
    public Task<WeakReference> InstanceTask { get; }

    /// <summary>
    /// Gets or sets the visibility of the object instance.
    /// </summary>
    public ObjectInstanceVisibility Visibility { get; set; }
}
