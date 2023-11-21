using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

namespace Dalamud;

/// <summary>
/// Basic service locator.
/// </summary>
/// <remarks>
/// Only used internally within Dalamud, if plugins need access to things it should be _only_ via DI.
/// </remarks>
/// <typeparam name="T">The class you want to store in the service locator.</typeparam>
[SuppressMessage("ReSharper", "StaticMemberInGenericType", Justification = "Service container static type")]
internal static class Service<T> where T : IServiceType
{
    private static readonly ServiceManager.ServiceAttribute ServiceAttribute;
    private static TaskCompletionSource<T> instanceTcs = new();
    private static List<Type>? dependencyServices;

    static Service()
    {
        var type = typeof(T);
        ServiceAttribute =
            type.GetCustomAttribute<ServiceManager.ServiceAttribute>(true)
            ?? throw new InvalidOperationException(
                $"{nameof(T)} is missing {nameof(ServiceManager.ServiceAttribute)} annotations.");

        var exposeToPlugins = type.GetCustomAttribute<PluginInterfaceAttribute>() != null;
        if (exposeToPlugins)
            ServiceManager.Log.Debug("Service<{0}>: Static ctor called; will be exposed to plugins", type.Name);
        else
            ServiceManager.Log.Debug("Service<{0}>: Static ctor called", type.Name);

        if (exposeToPlugins)
            Service<ServiceContainer>.Get().RegisterSingleton(instanceTcs.Task);
    }

    /// <summary>
    /// Specifies how to handle the cases of failed services when calling <see cref="Service{T}.GetNullable"/>.
    /// </summary>
    public enum ExceptionPropagationMode
    {
        /// <summary>
        /// Propagate all exceptions.
        /// </summary>
        PropagateAll,

        /// <summary>
        /// Propagate all exceptions, except for <see cref="UnloadedException"/>.
        /// </summary>
        PropagateNonUnloaded,

        /// <summary>
        /// Treat all exceptions as null.
        /// </summary>
        None,
    }

    /// <summary>
    /// Sets the type in the service locator to the given object.
    /// </summary>
    /// <param name="obj">Object to set.</param>
    public static void Provide(T obj)
    {
        ServiceManager.Log.Debug("Service<{0}>: Provided", typeof(T).Name);
        instanceTcs.SetResult(obj);
    }

    /// <summary>
    /// Sets the service load state to failure.
    /// </summary>
    /// <param name="exception">The exception.</param>
    public static void ProvideException(Exception exception)
    {
        ServiceManager.Log.Error(exception, "Service<{0}>: Error", typeof(T).Name);
        instanceTcs.SetException(exception);
    }

    /// <summary>
    /// Pull the instance out of the service locator, waiting if necessary.
    /// </summary>
    /// <returns>The object.</returns>
    public static T Get()
    {
#if DEBUG
        if (ServiceAttribute.Kind != ServiceManager.ServiceKind.ProvidedService
            && ServiceManager.CurrentConstructorServiceType.Value is { } currentServiceType)
        {
            var deps = ServiceHelpers.GetDependencies(currentServiceType);
            if (!deps.Contains(typeof(T)))
            {
                throw new InvalidOperationException(
                    $"Calling {nameof(Service<IServiceType>)}<{typeof(T)}>.{nameof(Get)} which is not one of the" +
                    $" dependency services is forbidden from the service constructor of {currentServiceType}." +
                    $" This has a high chance of introducing hard-to-debug hangs.");
            }
        }
#endif

        if (!instanceTcs.Task.IsCompleted)
            instanceTcs.Task.Wait();
        return instanceTcs.Task.Result;
    }

    /// <summary>
    /// Pull the instance out of the service locator, waiting if necessary.
    /// </summary>
    /// <returns>The object.</returns>
    [UsedImplicitly]
    public static Task<T> GetAsync() => instanceTcs.Task;

    /// <summary>
    /// Attempt to pull the instance out of the service locator.
    /// </summary>
    /// <param name="propagateException">Specifies which exceptions to propagate.</param>
    /// <returns>The object if registered, null otherwise.</returns>
    public static T? GetNullable(ExceptionPropagationMode propagateException = ExceptionPropagationMode.PropagateNonUnloaded)
    {
        if (instanceTcs.Task.IsCompletedSuccessfully)
            return instanceTcs.Task.Result;
        if (instanceTcs.Task.IsFaulted && propagateException != ExceptionPropagationMode.None)
        {
            if (propagateException == ExceptionPropagationMode.PropagateNonUnloaded
                && instanceTcs.Task.Exception!.InnerExceptions.FirstOrDefault() is UnloadedException)
                return default;
            throw instanceTcs.Task.Exception!;
        }

        return default;
    }

    /// <summary>
    /// Gets an enumerable containing <see cref="Service{T}"/>s that are required for this Service to initialize
    /// without blocking.
    /// </summary>
    /// <returns>List of dependency services.</returns>
    [UsedImplicitly]
    public static List<Type> GetDependencyServices()
    {
        if (dependencyServices is not null)
            return dependencyServices;

        var res = new List<Type>();
        
        ServiceManager.Log.Verbose("Service<{0}>: Getting dependencies", typeof(T).Name);

        var ctor = GetServiceConstructor();
        if (ctor != null)
        {
            res.AddRange(ctor
               .GetParameters()
               .Select(x => x.ParameterType));
        }
        
        res.AddRange(typeof(T)
                         .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Where(x => x.GetCustomAttribute<ServiceManager.ServiceDependency>(true) != null)
                         .Select(x => x.FieldType));
        
        res.AddRange(typeof(T)
                     .GetCustomAttributes()
                     .OfType<InherentDependencyAttribute>()
                     .Select(x => x.GetType().GetGenericArguments().First()));

        // HACK: PluginManager needs to depend on ALL plugin exposed services
        if (typeof(T) == typeof(PluginManager))
        {
            foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!serviceType.IsAssignableTo(typeof(IServiceType)))
                    continue;
                
                if (serviceType == typeof(PluginManager))
                    continue;
                
                // Scoped plugin services lifetime is tied to their scopes. They go away when LocalPlugin goes away.
                // Nonetheless, their direct dependencies must be considered.
                if (serviceType.GetServiceKind() == ServiceManager.ServiceKind.ScopedService)
                {
                    var typeAsServiceT = ServiceHelpers.GetAsService(serviceType);
                    var dependencies = ServiceHelpers.GetDependencies(typeAsServiceT);
                    ServiceManager.Log.Verbose("Found dependencies of scoped plugin service {Type} ({Cnt})", serviceType.FullName!, dependencies!.Count);
                    
                    foreach (var scopedDep in dependencies)
                    {
                        if (scopedDep == typeof(PluginManager))
                            throw new Exception("Scoped plugin services cannot depend on PluginManager.");
                        
                        ServiceManager.Log.Verbose("PluginManager MUST depend on {Type} via {BaseType}", scopedDep.FullName!, serviceType.FullName!);
                        res.Add(scopedDep);
                    }

                    continue;
                }
                
                var pluginInterfaceAttribute = serviceType.GetCustomAttribute<PluginInterfaceAttribute>(true);
                if (pluginInterfaceAttribute == null)
                    continue;

                ServiceManager.Log.Verbose("PluginManager MUST depend on {Type}", serviceType.FullName!);
                res.Add(serviceType);
            }
        }
        
        foreach (var type in res)
        {
            ServiceManager.Log.Verbose("Service<{0}>: => Dependency: {1}", typeof(T).Name, type.Name);
        }

        var deps = res
                   .Distinct()
                   .ToList();
        if (typeof(T).GetCustomAttribute<ServiceManager.BlockingEarlyLoadedServiceAttribute>() is not null)
        {
            var offenders = deps.Where(
                                    x => x.GetCustomAttribute<ServiceManager.ServiceAttribute>(true)!.Kind
                                             is not ServiceManager.ServiceKind.BlockingEarlyLoadedService
                                             and not ServiceManager.ServiceKind.ProvidedService)
                                .ToArray();
            if (offenders.Any())
            {
                ServiceManager.Log.Error(
                    "{me} is a {bels}; it can only depend on {bels} and {ps}.\nOffending dependencies:\n{offenders}",
                    typeof(T),
                    nameof(ServiceManager.BlockingEarlyLoadedServiceAttribute),
                    nameof(ServiceManager.BlockingEarlyLoadedServiceAttribute),
                    nameof(ServiceManager.ProvidedServiceAttribute),
                    string.Join("\n", offenders.Select(x => $"\t* {x.Name}")));
            }
        }

        return dependencyServices = deps;
    }

    /// <summary>
    /// Starts the service loader. Only to be called from <see cref="ServiceManager"/>.
    /// </summary>
    /// <returns>The loader task.</returns>
    internal static Task<T> StartLoader()
    {
        if (instanceTcs.Task.IsCompleted)
            throw new InvalidOperationException($"{typeof(T).Name} is already loaded or disposed.");

        var attr = ServiceAttribute.GetType();
        if (attr.IsAssignableTo(typeof(ServiceManager.EarlyLoadedServiceAttribute)) != true)
            throw new InvalidOperationException($"{typeof(T).Name} is not an EarlyLoadedService");

        return Task.Run(Timings.AttachTimingHandle(async () =>
        {
            ServiceManager.Log.Debug("Service<{0}>: Begin construction", typeof(T).Name);
            try
            {
                var instance = await ConstructObject();
                instanceTcs.SetResult(instance);

                List<Task>? tasks = null;
                foreach (var method in typeof(T).GetMethods(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.GetCustomAttribute<ServiceManager.CallWhenServicesReady>(true) == null)
                        continue;

                    ServiceManager.Log.Debug("Service<{0}>: Calling {1}", typeof(T).Name, method.Name);
                    var args = await Task.WhenAll(method.GetParameters().Select(
                                                      x => ResolveServiceFromTypeAsync(x.ParameterType)));
                    try
                    {
                        if (method.Invoke(instance, args) is Task task)
                        {
                            tasks ??= new();
                            tasks.Add(task);
                        }
                    }
                    catch (Exception e)
                    {
                        tasks ??= new();
                        tasks.Add(Task.FromException(e));
                    }
                }

                if (tasks is not null)
                    await Task.WhenAll(tasks);

                ServiceManager.Log.Debug("Service<{0}>: Construction complete", typeof(T).Name);
                return instance;
            }
            catch (Exception e)
            {
                ServiceManager.Log.Error(e, "Service<{0}>: Construction failure", typeof(T).Name);
                instanceTcs.SetException(e);
                throw;
            }
        }));
    }

    [UsedImplicitly]
    private static void Unset()
    {
        if (!instanceTcs.Task.IsCompletedSuccessfully)
            return;

        var instance = instanceTcs.Task.Result;
        if (instance is IDisposable disposable)
        {
            ServiceManager.Log.Debug("Service<{0}>: Disposing", typeof(T).Name);
            try
            {
                disposable.Dispose();
                ServiceManager.Log.Debug("Service<{0}>: Disposed", typeof(T).Name);
            }
            catch (Exception e)
            {
                ServiceManager.Log.Warning(e, "Service<{0}>: Dispose failure", typeof(T).Name);
            }
        }
        else
        {
            ServiceManager.Log.Debug("Service<{0}>: Unset", typeof(T).Name);
        }

        instanceTcs = new TaskCompletionSource<T>();
        instanceTcs.SetException(new UnloadedException());
    }

    private static async Task<object?> ResolveServiceFromTypeAsync(Type type)
    {
        var task = (Task)typeof(Service<>)
                         .MakeGenericType(type)
                         .InvokeMember(
                             "GetAsync",
                             BindingFlags.InvokeMethod |
                             BindingFlags.Static |
                             BindingFlags.Public,
                             null,
                             null,
                             null)!;
        await task;
        return typeof(Task<>).MakeGenericType(type)
                             .GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)!
                             .GetValue(task);
    }

    private static ConstructorInfo? GetServiceConstructor()
    {
        const BindingFlags ctorBindingFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
        return typeof(T)
               .GetConstructors(ctorBindingFlags)
               .SingleOrDefault(x => x.GetCustomAttributes(typeof(ServiceManager.ServiceConstructor), true).Any());
    }

    private static async Task<T> ConstructObject()
    {
        var ctor = GetServiceConstructor();
        if (ctor == null)
            throw new Exception($"Service \"{typeof(T).FullName}\" had no applicable constructor");
        
        var args = await Task.WhenAll(
                       ctor.GetParameters().Select(x => ResolveServiceFromTypeAsync(x.ParameterType)));
        using (Timings.Start($"{typeof(T).Name} Construct"))
        {
#if DEBUG
            ServiceManager.CurrentConstructorServiceType.Value = typeof(Service<T>);
            try
            {
                return (T)ctor.Invoke(args)!;
            }
            finally
            {
                ServiceManager.CurrentConstructorServiceType.Value = null;
            }
#else
            return (T)ctor.Invoke(args)!;
#endif
        }
    }

    /// <summary>
    /// Exception thrown when service is attempted to be retrieved when it's unloaded.
    /// </summary>
    public class UnloadedException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnloadedException"/> class.
        /// </summary>
        public UnloadedException()
            : base("Service is unloaded.")
        {
        }
    }
}

/// <summary>
/// Helper functions for services.
/// </summary>
internal static class ServiceHelpers
{
    /// <summary>
    /// Get a list of dependencies for a service. Only accepts <see cref="Service{T}"/> types.
    /// These are returned as <see cref="Service{T}"/> types.
    /// </summary>
    /// <param name="serviceType">The dependencies for this service.</param>
    /// <returns>A list of dependencies.</returns>
    public static List<Type> GetDependencies(Type serviceType)
    {
#if DEBUG
        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(Service<>))
        {
            throw new ArgumentException(
                $"Expected an instance of {nameof(Service<IServiceType>)}<>",
                nameof(serviceType));
        }
#endif

        return (List<Type>)serviceType.InvokeMember(
                   nameof(Service<IServiceType>.GetDependencyServices),
                   BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                   null,
                   null,
                   null) ?? new List<Type>();
    }

    /// <summary>
    /// Get the <see cref="Service{T}"/> type for a given service type.
    /// This will throw if the service type is not a valid service.
    /// </summary>
    /// <param name="type">The type to obtain a <see cref="Service{T}"/> for.</param>
    /// <returns>The <see cref="Service{T}"/>.</returns>
    public static Type GetAsService(Type type)
    {
#if DEBUG
        if (!type.IsAssignableTo(typeof(IServiceType)))
            throw new ArgumentException($"Expected an instance of {nameof(IServiceType)}", nameof(type));
#endif

        return typeof(Service<>).MakeGenericType(type);
    }
}
