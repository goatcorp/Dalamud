using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging.Internal;

namespace Dalamud.IoC.Internal;

/// <summary>
/// A simple singleton-only IOC container that provides (optional) version-based dependency resolution.
///
/// This is only used to resolve dependencies for plugins.
/// Dalamud services are constructed via Service{T}.ConstructObject at the moment.
/// </summary>
[ServiceManager.ProvidedService]
internal class ServiceContainer : IServiceProvider, IServiceType
{
    private static readonly ModuleLog Log = new("SERVICECONTAINER");

    private readonly Dictionary<Type, ObjectInstance> instances = new();
    private readonly Dictionary<Type, Type> interfaceToTypeMap = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceContainer"/> class.
    /// </summary>
    public ServiceContainer()
    {
        // Register the service container itself as a singleton.
        // For all other services, this is done through the static constructor of Service{T}.
        this.instances.Add(
            typeof(IServiceContainer),
            new(new Task<WeakReference>(() => new WeakReference(this), TaskCreationOptions.RunContinuationsAsynchronously), typeof(ServiceContainer), ObjectInstanceVisibility.Internal));
    }

    /// <summary>
    /// Gets a dictionary of all registered instances.
    /// </summary>
    public IReadOnlyDictionary<Type, ObjectInstance> Instances => this.instances;

    /// <summary>
    /// Gets a dictionary mapping interfaces to their implementations.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> InterfaceToTypeMap => this.interfaceToTypeMap;

    /// <summary>
    /// Register a singleton object of any type into the current IOC container.
    /// </summary>
    /// <param name="instance">The existing instance to register in the container.</param>
    /// <param name="visibility">The visibility of this singleton.</param>
    /// <typeparam name="T">The type to register.</typeparam>
    public void RegisterSingleton<T>(Task<T> instance, ObjectInstanceVisibility visibility)
    {
        ArgumentNullException.ThrowIfNull(instance);

        this.instances[typeof(T)] = new(instance.ContinueWith(x => new WeakReference(x.Result)), typeof(T), visibility);
    }

    /// <summary>
    /// Register the interfaces that can resolve this type.
    /// </summary>
    /// <param name="type">The type to register.</param>
    public void RegisterInterfaces(Type type)
    {
        var resolveViaTypes = type
                              .GetCustomAttributes()
                              .OfType<ResolveViaAttribute>()
                              .Select(x => x.GetType().GetGenericArguments().First());
        foreach (var resolvableType in resolveViaTypes)
        {
            Log.Verbose("=> {InterfaceName} provides for {TName}", resolvableType.FullName ?? "???", type.FullName ?? "???");

            Debug.Assert(!this.interfaceToTypeMap.ContainsKey(resolvableType), "A service already implements this interface, this is not allowed");
            Debug.Assert(type.IsAssignableTo(resolvableType), "Service does not inherit from indicated ResolveVia type");

            this.interfaceToTypeMap[resolvableType] = type;
        }
    }

    /// <summary>
    /// Create an object.
    /// </summary>
    /// <param name="objectType">The type of object to create.</param>
    /// <param name="allowedVisibility">Defines which services are allowed to be directly resolved into this type.</param>
    /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
    /// <param name="scope">The scope to be used to create scoped services.</param>
    /// <returns>The created object.</returns>
    public async Task<object> CreateAsync(Type objectType, ObjectInstanceVisibility allowedVisibility, object[] scopedObjects, IServiceScope? scope = null)
    {
        var errorStep = "constructor lookup";

        try
        {
            var scopeImpl = scope as ServiceScopeImpl;

            var ctor = this.FindApplicableCtor(objectType, scopedObjects)
                ?? throw new InvalidOperationException("An eligible ctor with satisfiable services could not be found");

            errorStep = "requested service resolution";
            var resolvedParams =
                await Task.WhenAll(
                    ctor.GetParameters()
                        .Select(p => p.ParameterType)
                        .Select(type => this.GetService(type, scopeImpl, scopedObjects)));

            var instance = RuntimeHelpers.GetUninitializedObject(objectType);

            errorStep = "property injection";
            await this.InjectProperties(instance, scopedObjects, scope);

            // Invoke ctor from a separate thread (LongRunning will spawn a new one)
            // so that it does not count towards thread pool active threads cap.
            // Plugin ctor can block to wait for Tasks, as we currently do not support asynchronous plugin init.
            errorStep = "ctor invocation";
            await Task.Factory.StartNew(
                () => ctor.Invoke(instance, resolvedParams),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).ConfigureAwait(false);

            return instance;
        }
        catch (Exception e)
        {
            throw new AggregateException($"Failed to create {objectType.FullName ?? objectType.Name} ({errorStep})", e);
        }
    }

    /// <summary>
    /// Inject <see cref="PluginInterfaceAttribute"/> interfaces into public or static properties on the provided object.
    /// The properties have to be marked with the <see cref="PluginServiceAttribute"/>.
    /// </summary>
    /// <param name="instance">The object instance.</param>
    /// <param name="publicScopes">Scoped objects to be injected.</param>
    /// <param name="scope">The scope to be used to create scoped services.</param>
    /// <returns>A <see cref="ValueTask"/> representing the operation.</returns>
    public async Task InjectProperties(object instance, object[] publicScopes, IServiceScope? scope = null)
    {
        var scopeImpl = scope as ServiceScopeImpl;
        var objectType = instance.GetType();

        var props =
            objectType
                .GetProperties(
                    BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(x => x.GetCustomAttributes(typeof(PluginServiceAttribute)).Any())
                .ToArray();

        foreach (var prop in props)
            prop.SetValue(instance, await this.GetService(prop.PropertyType, scopeImpl, publicScopes));
    }

    /// <summary>
    /// Get a service scope, enabling the creation of objects with scoped services.
    /// </summary>
    /// <returns>An implementation of a service scope.</returns>
    public IServiceScope GetScope() => new ServiceScopeImpl(this);

    /// <inheritdoc/>
    object? IServiceProvider.GetService(Type serviceType) => this.GetSingletonService(serviceType).Result;

    private async Task<object> GetService(Type serviceType, ServiceScopeImpl? scope, object[] scopedObjects)
    {
        if (this.interfaceToTypeMap.TryGetValue(serviceType, out var implementingType))
            serviceType = implementingType;

        if (serviceType.GetCustomAttribute<ServiceManager.ScopedServiceAttribute>() != null)
        {
            if (scope == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create {serviceType.FullName ?? serviceType.Name}, is scoped but no scope provided");
            }

            return await scope.CreatePrivateScopedObject(serviceType, scopedObjects);
        }

        var singletonService = await this.GetSingletonService(serviceType, false);
        if (singletonService != null)
            return singletonService;

        // resolve dependency from scoped objects
        return scopedObjects.FirstOrDefault(o => o.GetType().IsAssignableTo(serviceType))
               ?? throw new InvalidOperationException(
                   $"Requested type {serviceType.FullName ?? serviceType.Name} could not be found from {nameof(scopedObjects)}");
    }

    private async Task<object?> GetSingletonService(Type serviceType, bool tryGetInterface = true)
    {
        if (tryGetInterface && this.interfaceToTypeMap.TryGetValue(serviceType, out var implementingType))
            serviceType = implementingType;

        if (!this.instances.TryGetValue(serviceType, out var service))
            return null;

        var instance = await service.InstanceTask;
        return instance.Target;
    }

    private ConstructorInfo? FindApplicableCtor(Type type, object[] scopedObjects)
    {
        // get a list of all the available types: scoped and singleton
        var allValidServiceTypes = scopedObjects
                    .Select(o => o.GetType())
                    .Union(this.instances.Keys)
                    .ToArray();

        // Allow resolving non-public ctors for Dalamud types
        var ctorFlags = BindingFlags.Public | BindingFlags.Instance;
        if (type.Assembly == Assembly.GetExecutingAssembly())
            ctorFlags |= BindingFlags.NonPublic;

        var ctors = type.GetConstructors(ctorFlags);
        foreach (var ctor in ctors)
        {
            if (this.ValidateCtor(ctor, allValidServiceTypes))
            {
                return ctor;
            }
        }

        return null;
    }

    private bool ValidateCtor(ConstructorInfo ctor, Type[] validTypes)
    {
        bool IsTypeValid(Type type)
        {
            var contains = validTypes.Any(x => x.IsAssignableTo(type));

            // Scoped services are created on-demand
            return contains || type.GetCustomAttribute<ServiceManager.ScopedServiceAttribute>() != null;
        }

        var parameters = ctor.GetParameters();
        foreach (var parameter in parameters)
        {
            var valid = IsTypeValid(parameter.ParameterType);

            // If this service is provided by an interface
            if (!valid && this.interfaceToTypeMap.TryGetValue(parameter.ParameterType, out var implementationType))
                valid = IsTypeValid(implementationType);

            if (!valid)
            {
                Log.Error("Ctor from {DeclaringType}: Failed to validate {TypeName}, unable to find any services that satisfy the type",
                          ctor.DeclaringType?.FullName ?? ctor.DeclaringType?.Name ?? "null",
                          parameter.ParameterType.FullName!);
                return false;
            }
        }

        return true;
    }
}
