namespace Dalamud.IoC.Internal;

/// <summary>
/// Enum that declares the visibility of an object instance in the service container.
/// </summary>
internal enum ObjectInstanceVisibility
{
    /// <summary>
    /// The object instance is only visible to other internal services.
    /// </summary>
    Internal,

    /// <summary>
    /// The object instance is visible to all services and plugins.
    /// </summary>
    ExposedToPlugins,
}
