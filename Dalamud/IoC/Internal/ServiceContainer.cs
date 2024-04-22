using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    /// <typeparam name="T">The type to register.</typeparam>
    public void RegisterSingleton<T>(Task<T> instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        this.instances[typeof(T)] = new(instance.ContinueWith(x => new WeakReference(x.Result)), typeof(T));
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
    /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
    /// <param name="scope">The scope to be used to create scoped services.</param>
    /// <returns>The created object.</returns>
    public async Task<object?> CreateAsync(Type objectType, object[] scopedObjects, IServiceScope? scope = null)
    {
        var scopeImpl = scope as ServiceScopeImpl;

        var ctor = this.FindApplicableCtor(objectType, scopedObjects);
        if (ctor == null)
        {
            Log.Error("Failed to create {TypeName}, an eligible ctor with satisfiable services could not be found", objectType.FullName!);
            return null;
        }

        // validate dependency versions (if they exist)
        var parameters = ctor.GetParameters().Select(p =>
        {
            var parameterType = p.ParameterType;
            var requiredVersion = p.GetCustomAttribute(typeof(RequiredVersionAttribute)) as RequiredVersionAttribute;
            return (parameterType, requiredVersion);
        }).ToList();

        var versionCheck = parameters.All(p => CheckInterfaceVersion(p.requiredVersion, p.parameterType));

        if (!versionCheck)
        {
            Log.Error("Failed to create {TypeName}, a RequestedVersion could not be satisfied", objectType.FullName!);
            return null;
        }

        var resolvedParams =
            await Task.WhenAll(
                parameters
                    .Select(async p =>
                    {
                        var service = await this.GetService(p.parameterType, scopeImpl, scopedObjects);

                        if (service == null)
                        {
                            Log.Error("Requested ctor service type {TypeName} was not available (null)", p.parameterType.FullName!);
                        }

                        return service;
                    }));

        var hasNull = resolvedParams.Any(p => p == null);
        if (hasNull)
        {
            Log.Error("Failed to create {TypeName}, a requested service type could not be satisfied", objectType.FullName!);
            return null;
        }

        var instance = RuntimeHelpers.GetUninitializedObject(objectType);

        if (!await this.InjectProperties(instance, scopedObjects, scope))
        {
            Log.Error("Failed to create {TypeName}, a requested property service type could not be satisfied", objectType.FullName!);
            return null;
        }

        ctor.Invoke(instance, resolvedParams);

        return instance;
    }

    /// <summary>
    /// Inject <see cref="PluginInterfaceAttribute"/> interfaces into public or static properties on the provided object.
    /// The properties have to be marked with the <see cref="PluginServiceAttribute"/>.
    /// The properties can be marked with the <see cref="RequiredVersionAttribute"/> to lock down versions.
    /// </summary>
    /// <param name="instance">The object instance.</param>
    /// <param name="publicScopes">Scoped objects to be injected.</param>
    /// <param name="scope">The scope to be used to create scoped services.</param>
    /// <returns>Whether or not the injection was successful.</returns>
    public async Task<bool> InjectProperties(object instance, object[] publicScopes, IServiceScope? scope = null)
    {
        var scopeImpl = scope as ServiceScopeImpl;
        var objectType = instance.GetType();

        var props = objectType.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
                                             BindingFlags.NonPublic).Where(x => x.GetCustomAttributes(typeof(PluginServiceAttribute)).Any()).Select(
            propertyInfo =>
            {
                var requiredVersion = propertyInfo.GetCustomAttribute(typeof(RequiredVersionAttribute)) as RequiredVersionAttribute;
                return (propertyInfo, requiredVersion);
            }).ToArray();

        var versionCheck = props.All(x => CheckInterfaceVersion(x.requiredVersion, x.propertyInfo.PropertyType));

        if (!versionCheck)
        {
            Log.Error("Failed to create {TypeName}, a RequestedVersion could not be satisfied", objectType.FullName!);
            return false;
        }

        foreach (var prop in props)
        {
            var service = await this.GetService(prop.propertyInfo.PropertyType, scopeImpl, publicScopes);
            if (service == null)
            {
                Log.Error("Requested service type {TypeName} was not available (null)", prop.propertyInfo.PropertyType.FullName!);
                return false;
            }

            prop.propertyInfo.SetValue(instance, service);
        }

        return true;
    }

    /// <summary>
    /// Get a service scope, enabling the creation of objects with scoped services.
    /// </summary>
    /// <returns>An implementation of a service scope.</returns>
    public IServiceScope GetScope() => new ServiceScopeImpl(this);

    /// <inheritdoc/>
    object? IServiceProvider.GetService(Type serviceType) => this.GetSingletonService(serviceType);

    private static bool CheckInterfaceVersion(RequiredVersionAttribute? requiredVersion, Type parameterType)
    {
        // if there's no required version, ignore it
        if (requiredVersion == null)
            return true;

        // if there's no requested version, ignore it
        var declVersion = parameterType.GetCustomAttribute<InterfaceVersionAttribute>();
        if (declVersion == null)
            return true;

        if (declVersion.Version == requiredVersion.Version)
            return true;

        Log.Error(
            "Requested version {ReqVersion} does not match the implemented version {ImplVersion} for param type {ParamType}",
            requiredVersion.Version,
            declVersion.Version,
            parameterType.FullName!);

        return false;
    }

    private async Task<object?> GetService(Type serviceType, ServiceScopeImpl? scope, object[] scopedObjects)
    {
        if (this.interfaceToTypeMap.TryGetValue(serviceType, out var implementingType))
            serviceType = implementingType;
        
        if (serviceType.GetCustomAttribute<ServiceManager.ScopedServiceAttribute>() != null)
        {
            if (scope == null)
            {
                Log.Error("Failed to create {TypeName}, is scoped but no scope provided", serviceType.FullName!);
                return null;
            }

            return await scope.CreatePrivateScopedObject(serviceType, scopedObjects);
        }

        var singletonService = await this.GetSingletonService(serviceType, false);
        if (singletonService != null)
        {
            return singletonService;
        }

        // resolve dependency from scoped objects
        var scoped = scopedObjects.FirstOrDefault(o => o.GetType().IsAssignableTo(serviceType));
        if (scoped == default)
        {
            return null;
        }

        return scoped;
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
        var types = scopedObjects
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
            if (this.ValidateCtor(ctor, types))
            {
                return ctor;
            }
        }

        return null;
    }

    private bool ValidateCtor(ConstructorInfo ctor, Type[] types)
    {
        bool IsTypeValid(Type type)
        {
            var contains = types.Any(x => x.IsAssignableTo(type));

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
                Log.Error("Failed to validate {TypeName}, unable to find any services that satisfy the type", parameter.ParameterType.FullName!);
                return false;
            }
        }

        return true;
    }
}
