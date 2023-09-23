using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

namespace Dalamud;

// TODO:
// - Unify dependency walking code(load/unload
// - Visualize/output .dot or imgui thing

/// <summary>
/// Class to initialize Service&lt;T&gt;s.
/// </summary>
internal static class ServiceManager
{
    /// <summary>
    /// Static log facility for Service{T}, to avoid duplicate instances for different types.
    /// </summary>
    public static readonly ModuleLog Log = new("SVC");

    private static readonly TaskCompletionSource BlockingServicesLoadedTaskCompletionSource = new();

    private static readonly List<Type> LoadedServices = new();

    private static ManualResetEvent unloadResetEvent = new(false);
    
    /// <summary>
    /// Kinds of services.
    /// </summary>
    [Flags]
    public enum ServiceKind
    {
        /// <summary>
        /// Not a service.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Regular service.
        /// </summary>
        ManualService = 1 << 0,
        
        /// <summary>
        /// Service that is loaded asynchronously while the game starts.
        /// </summary>
        EarlyLoadedService = 1 << 1,
        
        /// <summary>
        /// Service that is loaded before the game starts.
        /// </summary>
        BlockingEarlyLoadedService = 1 << 2,
        
        /// <summary>
        /// Service that is only instantiable via scopes.
        /// </summary>
        ScopedService = 1 << 3,
        
        /// <summary>
        /// Service that is loaded automatically when the game starts, synchronously or asynchronously.
        /// </summary>
        AutoLoadService = EarlyLoadedService | BlockingEarlyLoadedService,
    }

    /// <summary>
    /// Gets task that gets completed when all blocking early loading services are done loading.
    /// </summary>
    public static Task BlockingResolved { get; } = BlockingServicesLoadedTaskCompletionSource.Task;

    /// <summary>
    /// Initializes Provided Services and FFXIVClientStructs.
    /// </summary>
    /// <param name="dalamud">Instance of <see cref="Dalamud"/>.</param>
    /// <param name="startInfo">Instance of <see cref="DalamudStartInfo"/>.</param>
    /// <param name="configuration">Instance of <see cref="DalamudConfiguration"/>.</param>
    public static void InitializeProvidedServicesAndClientStructs(Dalamud dalamud, DalamudStartInfo startInfo, DalamudConfiguration configuration)
    {
        // Initialize the process information.
        var cacheDir = new DirectoryInfo(Path.Combine(startInfo.WorkingDirectory!, "cachedSigs"));
        if (!cacheDir.Exists)
            cacheDir.Create();

        lock (LoadedServices)
        {
            Service<Dalamud>.Provide(dalamud);
            LoadedServices.Add(typeof(Dalamud));

            Service<DalamudStartInfo>.Provide(startInfo);
            LoadedServices.Add(typeof(DalamudStartInfo));

            Service<DalamudConfiguration>.Provide(configuration);
            LoadedServices.Add(typeof(DalamudConfiguration));

            Service<ServiceContainer>.Provide(new ServiceContainer());
            LoadedServices.Add(typeof(ServiceContainer));

            Service<TargetSigScanner>.Provide(
                new TargetSigScanner(
                    true, new FileInfo(Path.Combine(cacheDir.FullName, $"{startInfo.GameVersion}.json"))));
            LoadedServices.Add(typeof(TargetSigScanner));
        }

        using (Timings.Start("CS Resolver Init"))
        {
            FFXIVClientStructs.Interop.Resolver.GetInstance.SetupSearchSpace(Service<TargetSigScanner>.Get().SearchBase, new FileInfo(Path.Combine(cacheDir.FullName, $"{startInfo.GameVersion}_cs.json")));
            FFXIVClientStructs.Interop.Resolver.GetInstance.Resolve();
        }
    }

    /// <summary>
    /// Kicks off construction of services that can handle early loading.
    /// </summary>
    /// <returns>Task for initializing all services.</returns>
    public static async Task InitializeEarlyLoadableServices()
    {
        using var serviceInitializeTimings = Timings.Start("Services Init");

        var earlyLoadingServices = new HashSet<Type>();
        var blockingEarlyLoadingServices = new HashSet<Type>();

        var dependencyServicesMap = new Dictionary<Type, List<Type>>();
        var getAsyncTaskMap = new Dictionary<Type, Task>();

        var serviceContainer = Service<ServiceContainer>.Get();

        foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
        {
            var serviceKind = serviceType.GetServiceKind();
            if (serviceKind is ServiceKind.None)
                continue;

            // Scoped service do not go through Service<T>, so we must let ServiceContainer know what their interfaces map to
            if (serviceKind is ServiceKind.ScopedService)
            {
                serviceContainer.RegisterInterfaces(serviceType);
                continue;
            }
            
            Debug.Assert(
                !serviceKind.HasFlag(ServiceKind.ManualService) && !serviceKind.HasFlag(ServiceKind.ScopedService),
                "Regular and scoped services should never be loaded early");

            var genericWrappedServiceType = typeof(Service<>).MakeGenericType(serviceType);
            
            var getTask = (Task)genericWrappedServiceType
                                .InvokeMember(
                                    "GetAsync",
                                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                    null,
                                    null,
                                    null);

            if (serviceKind.HasFlag(ServiceKind.BlockingEarlyLoadedService))
            {
                getAsyncTaskMap[serviceType] = getTask;
                blockingEarlyLoadingServices.Add(serviceType);
            }
            else
            {
                earlyLoadingServices.Add(serviceType);
            }

            dependencyServicesMap[serviceType] =
                (List<Type>)typeof(Service<>)
                            .MakeGenericType(serviceType)
                            .InvokeMember(
                                "GetDependencyServices",
                                BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                null,
                                null,
                                null);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(blockingEarlyLoadingServices.Select(x => getAsyncTaskMap[x]));
                BlockingServicesLoadedTaskCompletionSource.SetResult();
                Timings.Event("BlockingServices Initialized");
            }
            catch (Exception e)
            {
                BlockingServicesLoadedTaskCompletionSource.SetException(e);
            }
        }).ConfigureAwait(false);

        var tasks = new List<Task>();
        try
        {
            var servicesToLoad = new HashSet<Type>();
            servicesToLoad.UnionWith(earlyLoadingServices);
            servicesToLoad.UnionWith(blockingEarlyLoadingServices);

            while (servicesToLoad.Any())
            {
                foreach (var serviceType in servicesToLoad)
                {
                    var hasDeps = true;
                    foreach (var dependency in dependencyServicesMap[serviceType])
                    {
                        var depServiceKind = dependency.GetServiceKind();
                        var depResolveTask = getAsyncTaskMap.GetValueOrDefault(dependency);

                        if (depResolveTask == null && (depServiceKind.HasFlag(ServiceKind.EarlyLoadedService) || depServiceKind.HasFlag(ServiceKind.BlockingEarlyLoadedService)))
                        {
                            Log.Error("{Type}: {Dependency} has no resolver task, is it early loaded or blocking early loaded?", serviceType.FullName!, dependency.FullName!);
                            Debug.Assert(false, $"No resolver for dependent service {dependency.FullName}");
                        }
                        else if (depResolveTask is { IsCompleted: false })
                        {
                            hasDeps = false;
                        }
                    }
                    
                    if (!hasDeps)
                        continue;

                    tasks.Add((Task)typeof(Service<>)
                                    .MakeGenericType(serviceType)
                                    .InvokeMember(
                                        "StartLoader",
                                        BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic,
                                        null,
                                        null,
                                        null));
                    servicesToLoad.Remove(serviceType);

                    tasks.Add(tasks.Last().ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                            return;
                        lock (LoadedServices)
                        {
                            LoadedServices.Add(serviceType);
                        }
                    }));
                }

                if (!tasks.Any())
                    throw new InvalidOperationException("Unresolvable dependency cycle detected");

                if (servicesToLoad.Any())
                {
                    await Task.WhenAny(tasks);
                    var faultedTasks = tasks.Where(x => x.IsFaulted).Select(x => (Exception)x.Exception!).ToArray();
                    if (faultedTasks.Any())
                        throw new AggregateException(faultedTasks);
                }
                else
                {
                    await Task.WhenAll(tasks);
                }

                tasks.RemoveAll(x => x.IsCompleted);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed resolving services");
            try
            {
                BlockingServicesLoadedTaskCompletionSource.SetException(e);
            }
            catch (Exception)
            {
                // don't care, as this means task result/exception has already been set
            }

            while (tasks.Any())
            {
                await Task.WhenAny(tasks);
                tasks.RemoveAll(x => x.IsCompleted);
            }

            UnloadAllServices();

            throw;
        }
    }

    /// <summary>
    /// Unloads all services, in the reverse order of load.
    /// </summary>
    public static void UnloadAllServices()
    {
        var framework = Service<Framework>.GetNullable(Service<Framework>.ExceptionPropagationMode.None);
        if (framework is { IsInFrameworkUpdateThread: false, IsFrameworkUnloading: false })
        {
            framework.RunOnFrameworkThread(UnloadAllServices).Wait();
            return;
        }

        unloadResetEvent.Reset();

        var dependencyServicesMap = new Dictionary<Type, List<Type>>();
        var allToUnload = new HashSet<Type>();
        var unloadOrder = new List<Type>();
        
        Log.Information("==== COLLECTING SERVICES TO UNLOAD ====");
        
        foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!serviceType.IsAssignableTo(typeof(IServiceType)))
                continue;
            
            // Scoped services shall never be unloaded here.
            // Their lifetime must be managed by the IServiceScope that owns them. If it leaks, it's their fault.
            if (serviceType.GetServiceKind() == ServiceKind.ScopedService)
                continue;

            Log.Verbose("Calling GetDependencyServices for '{ServiceName}'", serviceType.FullName!);

            dependencyServicesMap[serviceType] =
                ((List<Type>)typeof(Service<>)
                            .MakeGenericType(serviceType)
                            .InvokeMember(
                                "GetDependencyServices",
                                BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                null,
                                null,
                                null))!
                .Select(x => x.GetGenericArguments()[0]).ToList();

            allToUnload.Add(serviceType);
        }

        void UnloadService(Type serviceType)
        {
            if (unloadOrder.Contains(serviceType))
                return;

            var deps = dependencyServicesMap[serviceType];
            foreach (var dep in deps)
            {
                UnloadService(dep);
            }

            unloadOrder.Add(serviceType);
            Log.Information("Queue for unload {Type}", serviceType.FullName!);
        }
        
        foreach (var serviceType in allToUnload)
        {
            UnloadService(serviceType);
        }
        
        Log.Information("==== UNLOADING ALL SERVICES ====");

        unloadOrder.Reverse();
        foreach (var type in unloadOrder)
        {
            Log.Verbose("Unload {Type}", type.FullName!);

            typeof(Service<>)
                    .MakeGenericType(type)
                    .InvokeMember(
                        "Unset",
                        BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic,
                        null,
                        null,
                        null);
        }
        
        lock (LoadedServices)
        {
            LoadedServices.Clear();
        }

        unloadResetEvent.Set();
    }

    /// <summary>
    /// Wait until all services have been unloaded.
    /// </summary>
    public static void WaitForServiceUnload()
    {
        unloadResetEvent.WaitOne();
    }

    /// <summary>
    /// Get the service type of this type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The type of service this type is.</returns>
    public static ServiceKind GetServiceKind(this Type type)
    {
        var attr = type.GetCustomAttribute<Service>(true)?.GetType();
        if (attr == null)
            return ServiceKind.None;
        
        Debug.Assert(
            type.IsAssignableTo(typeof(IServiceType)),
            "Service did not inherit from IServiceType");

        if (attr.IsAssignableTo(typeof(BlockingEarlyLoadedService)))
            return ServiceKind.BlockingEarlyLoadedService;
        
        if (attr.IsAssignableTo(typeof(EarlyLoadedService)))
            return ServiceKind.EarlyLoadedService;
        
        if (attr.IsAssignableTo(typeof(ScopedService)))
            return ServiceKind.ScopedService;

        return ServiceKind.ManualService;
    }

    /// <summary>
    /// Indicates that this constructor will be called for early initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor)]
    [MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class ServiceConstructor : Attribute
    {
    }

    /// <summary>
    /// Indicates that the field is a service that should be loaded before constructing the class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ServiceDependency : Attribute
    {
    }

    /// <summary>
    /// Indicates that the class is a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Service : Attribute
    {
    }

    /// <summary>
    /// Indicates that the class is a service, and will be instantiated automatically on startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EarlyLoadedService : Service
    {
    }

    /// <summary>
    /// Indicates that the class is a service, and will be instantiated automatically on startup,
    /// blocking game main thread until it completes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BlockingEarlyLoadedService : EarlyLoadedService
    {
    }

    /// <summary>
    /// Indicates that the class is a service that will be created specifically for a
    /// service scope, and that it cannot be created outside of a scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedService : Service
    {
    }

    /// <summary>
    /// Indicates that the method should be called when the services given in the constructor are ready.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class CallWhenServicesReady : Attribute
    {
    }
}
