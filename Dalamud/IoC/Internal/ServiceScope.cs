using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Utility;

namespace Dalamud.IoC.Internal;

/// <summary>
/// Container enabling the creation of scoped services.
/// </summary>
internal interface IServiceScope : IServiceProvider, IAsyncDisposable
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
    /// <param name="allowedVisibility">Defines which services are allowed to be directly resolved into this type.</param>
    /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
    /// <returns>The created object.</returns>
    Task<object> CreateAsync(Type objectType, ObjectInstanceVisibility allowedVisibility, params object[] scopedObjects);

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

    private readonly ReaderWriterLockSlim disposeLock = new(LockRecursionPolicy.SupportsRecursion);
    private bool disposed;

    /// <summary>Initializes a new instance of the <see cref="ServiceScopeImpl" /> class.</summary>
    /// <param name="container">The container this scope will use to create services.</param>
    public ServiceScopeImpl(ServiceContainer container) => this.container = container;

    /// <inheritdoc/>
    public object? GetService(Type serviceType)
    {
        return this.container.GetService(serviceType);
    }

    /// <inheritdoc/>
    public void RegisterPrivateScopes(params object[] scopes)
    {
        this.disposeLock.EnterReadLock();
        try
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            this.privateScopedObjects.AddRange(scopes);
        }
        finally
        {
            this.disposeLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task<object> CreateAsync(Type objectType, ObjectInstanceVisibility allowedVisibility, params object[] scopedObjects)
    {
        this.disposeLock.EnterReadLock();
        try
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            return this.container.CreateAsync(objectType, allowedVisibility, scopedObjects, this);
        }
        finally
        {
            this.disposeLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Task InjectPropertiesAsync(object instance, params object[] scopedObjects)
    {
        this.disposeLock.EnterReadLock();
        try
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            return this.container.InjectProperties(instance, scopedObjects, this);
        }
        finally
        {
            this.disposeLock.ExitReadLock();
        }
    }

    /// <summary>
    /// Create a service scoped to this scope, with private scoped objects.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="scopedObjects">Additional scoped objects.</param>
    /// <returns>The created object, or null.</returns>
    public Task<object> CreatePrivateScopedObject(Type objectType, params object[] scopedObjects)
    {
        this.disposeLock.EnterReadLock();
        try
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            return this.scopeCreatedObjects.GetOrAdd(
                objectType,
                static (objectType, p) => p.Scope.container.CreateAsync(
                    objectType,
                    ObjectInstanceVisibility.Internal, // We are allowed to resolve internal services here since this is a private scoped object.
                    p.Objects.Concat(p.Scope.privateScopedObjects).ToArray(),
                    p.Scope),
                (Scope: this, Objects: scopedObjects));
        }
        finally
        {
            this.disposeLock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        this.disposeLock.EnterWriteLock();
        this.disposed = true;
        this.disposeLock.ExitWriteLock();

        List<Exception>? exceptions = null;
        while (!this.scopeCreatedObjects.IsEmpty)
        {
            try
            {
                await Task.WhenAll(
                    this.scopeCreatedObjects.Keys.Select(
                        async type =>
                        {
                            if (!this.scopeCreatedObjects.Remove(type, out var serviceTask))
                                return;

                            switch (await serviceTask)
                            {
                                case IInternalDisposableService d:
                                    d.DisposeService();
                                    break;
                                case IAsyncDisposable d:
                                    await d.DisposeAsync();
                                    break;
                                case IDisposable d:
                                    d.Dispose();
                                    break;
                            }
                        }));
            }
            catch (AggregateException ae)
            {
                exceptions ??= [];
                exceptions.AddRange(ae.Flatten().InnerExceptions);
            }
        }

        // Unless Dalamud is unloading (plugin cannot be reloading at that point), ensure that there are no more
        // event callback call in progress when this function returns. Since above service dispose operations should
        // have unregistered the event listeners, on next framework tick, none can be running anymore.
        // This has an additional effect of ensuring that DtrBar entries are completely removed on return.
        // Note that this still does not handle Framework.RunOnTick with specified delays.
        await (Service<Framework>.GetNullable()?.DelayTicks(1) ?? Task.CompletedTask).SuppressException();

        if (exceptions is not null)
            throw new AggregateException(exceptions);
    }
}
