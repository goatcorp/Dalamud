using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    public void RegisterPrivateScopes(params object[] scopes);

    /// <summary>
    /// Create an object.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
    /// <returns>The created object.</returns>
    public Task<object?> CreateAsync(Type objectType, params object[] scopedObjects);

    /// <summary>
    /// Inject <see cref="PluginInterfaceAttribute" /> interfaces into public or static properties on the provided object.
    /// The properties have to be marked with the <see cref="PluginServiceAttribute" />.
    /// The properties can be marked with the <see cref="RequiredVersionAttribute" /> to lock down versions.
    /// </summary>
    /// <param name="instance">The object instance.</param>
    /// <param name="scopedObjects">Scoped objects to be injected.</param>
    /// <returns>Whether or not the injection was successful.</returns>
    public Task<bool> InjectPropertiesAsync(object instance, params object[] scopedObjects);
}

/// <summary>
/// Implementation of a service scope.
/// </summary>
internal class ServiceScopeImpl : IServiceScope
{
    private readonly ServiceContainer container;

    private readonly List<object> privateScopedObjects = new();
    private readonly List<object> scopeCreatedObjects = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScopeImpl" /> class.
    /// </summary>
    /// <param name="container">The container this scope will use to create services.</param>
    public ServiceScopeImpl(ServiceContainer container)
    {
        this.container = container;
    }

    /// <inheritdoc/>
    public void RegisterPrivateScopes(params object[] scopes)
    {
        this.privateScopedObjects.AddRange(scopes);
    }

    /// <inheritdoc />
    public Task<object?> CreateAsync(Type objectType, params object[] scopedObjects)
    {
        return this.container.CreateAsync(objectType, scopedObjects, this);
    }

    /// <inheritdoc />
    public Task<bool> InjectPropertiesAsync(object instance, params object[] scopedObjects)
    {
        return this.container.InjectProperties(instance, scopedObjects, this);
    }

    /// <summary>
    /// Create a service scoped to this scope, with private scoped objects.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="scopedObjects">Additional scoped objects.</param>
    /// <returns>The created object, or null.</returns>
    public async Task<object?> CreatePrivateScopedObject(Type objectType, params object[] scopedObjects)
    {
        var instance = this.scopeCreatedObjects.FirstOrDefault(x => x.GetType() == objectType);
        if (instance != null)
            return instance;

        instance =
            await this.container.CreateAsync(objectType, scopedObjects.Concat(this.privateScopedObjects).ToArray());
        if (instance != null)
            this.scopeCreatedObjects.Add(instance);

        return instance;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var createdObject in this.scopeCreatedObjects)
        {
            switch (createdObject)
            {
                case IInternalDisposableService d:
                    d.DisposeService();
                    break;
                case IDisposable d:
                    d.Dispose();
                    break;
            }
        }
    }
}
