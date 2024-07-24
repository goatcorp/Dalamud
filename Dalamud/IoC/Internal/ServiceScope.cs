using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Serilog;

namespace Dalamud.IoC.Internal;

/// <summary>
/// Container enabling the creation of scoped services.
/// </summary>
internal interface IServiceScope : IDisposable
{
    /// <summary>
    /// Register objects that may be injected to scoped services,
    /// but not directly to created objects.
    /// </summary>
    /// <param name="scopes">The scopes to add.</param>
    void RegisterPrivateScopes(params object[] scopes);

    /// <summary>
    /// Create an object.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
    /// <returns>The created object.</returns>
    Task<object> CreateAsync(Type objectType, params object[] scopedObjects);

    /// <summary>
    /// Inject <see cref="PluginInterfaceAttribute" /> interfaces into public or static properties on the provided object.
    /// The properties have to be marked with the <see cref="PluginServiceAttribute" />.
    /// </summary>
    /// <param name="instance">The object instance.</param>
    /// <param name="scopedObjects">Scoped objects to be injected.</param>
    /// <returns>A <see cref="ValueTask"/> representing the status of the operation.</returns>
    Task InjectPropertiesAsync(object instance, params object[] scopedObjects);
}

/// <summary>
/// Implementation of a service scope.
/// </summary>
internal class ServiceScopeImpl : IServiceScope
{
    private readonly ServiceContainer container;

    private readonly List<object> privateScopedObjects = [];
    private readonly ConcurrentDictionary<Type, Task<object>> scopeCreatedObjects = new();

    /// <summary>Initializes a new instance of the <see cref="ServiceScopeImpl" /> class.</summary>
    /// <param name="container">The container this scope will use to create services.</param>
    public ServiceScopeImpl(ServiceContainer container) => this.container = container;

    /// <inheritdoc/>
    public void RegisterPrivateScopes(params object[] scopes) =>
        this.privateScopedObjects.AddRange(scopes);

    /// <inheritdoc />
    public Task<object> CreateAsync(Type objectType, params object[] scopedObjects) =>
        this.container.CreateAsync(objectType, scopedObjects, this);

    /// <inheritdoc />
    public Task InjectPropertiesAsync(object instance, params object[] scopedObjects) =>
        this.container.InjectProperties(instance, scopedObjects, this);

    /// <summary>
    /// Create a service scoped to this scope, with private scoped objects.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="scopedObjects">Additional scoped objects.</param>
    /// <returns>The created object, or null.</returns>
    public Task<object> CreatePrivateScopedObject(Type objectType, params object[] scopedObjects) =>
        this.scopeCreatedObjects.GetOrAdd(
            objectType,
            static (objectType, p) => p.Scope.container.CreateAsync(
                objectType,
                p.Objects.Concat(p.Scope.privateScopedObjects).ToArray()),
            (Scope: this, Objects: scopedObjects));

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var objectTask in this.scopeCreatedObjects)
        {
            objectTask.Value.ContinueWith(
                static r =>
                {
                    if (!r.IsCompletedSuccessfully)
                    {
                        if (r.Exception is { } e)
                            Log.Error(e, "{what}: Failed to load.", nameof(ServiceScopeImpl));
                        return;
                    }

                    switch (r.Result)
                    {
                        case IInternalDisposableService d:
                            d.DisposeService();
                            break;
                        case IDisposable d:
                            d.Dispose();
                            break;
                    }
                });
        }
    }
}
