using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Storage;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

namespace Dalamud;

// TODO:
// - Unify dependency walking code(load/unload)
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

#if DEBUG
    /// <summary>
    /// Marks which service constructor the current thread's in. For use from <see cref="Service{T}"/> only.
    /// </summary>
    internal static readonly ThreadLocal<Type?> CurrentConstructorServiceType = new();

    [SuppressMessage("ReSharper", "CollectionNeverQueried.Local", Justification = "Debugging purposes")]
    private static readonly List<Type> LoadedServices = new();
#endif

    private static readonly TaskCompletionSource BlockingServicesLoadedTaskCompletionSource = new();

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
        /// Service that is loaded manually.
        /// </summary>
        ProvidedService = 1 << 0,
        
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
    /// <param name="fs">Instance of <see cref="ReliableFileStorage"/>.</param>
    /// <param name="configuration">Instance of <see cref="DalamudConfiguration"/>.</param>
    /// <param name="scanner">Instance of <see cref="TargetSigScanner"/>.</param>
    public static void InitializeProvidedServices(Dalamud dalamud, ReliableFileStorage fs, DalamudConfiguration configuration, TargetSigScanner scanner)
    {
#if DEBUG
        lock (LoadedServices)
        {
            ProvideService(dalamud);
            ProvideService(fs);
            ProvideService(configuration);
            ProvideService(new ServiceContainer());
            ProvideService(scanner);
        }

        return;

        void ProvideService<T>(T service) where T : IServiceType
        {
            Debug.Assert(typeof(T).GetServiceKind().HasFlag(ServiceKind.ProvidedService), "Provided service must have Service attribute");
            Service<T>.Provide(service);
            LoadedServices.Add(typeof(T));
        }
#else
        ProvideService(dalamud);
        ProvideService(fs);
        ProvideService(configuration);
        ProvideService(new ServiceContainer());
        ProvideService(scanner);
        return;

        void ProvideService<T>(T service) where T : IServiceType => Service<T>.Provide(service);
#endif
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

        foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsAssignableTo(typeof(IServiceType)) && !x.IsInterface && !x.IsAbstract))
        {
            var serviceKind = serviceType.GetServiceKind();
            Debug.Assert(serviceKind != ServiceKind.None, $"Service<{serviceType.FullName}> did not specify a kind");

            // Let IoC know about the interfaces this service implements
            serviceContainer.RegisterInterfaces(serviceType);
            
            // Scoped service do not go through Service<T> and are never early loaded
            if (serviceKind.HasFlag(ServiceKind.ScopedService))
                continue;

            var genericWrappedServiceType = typeof(Service<>).MakeGenericType(serviceType);
            
            var getTask = (Task)genericWrappedServiceType
                                .InvokeMember(
                                    "GetAsync",
                                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                    null,
                                    null,
                                    null);

            getAsyncTaskMap[serviceType] = getTask;

            // We don't actually need to load provided services, something else does
            if (serviceKind.HasFlag(ServiceKind.ProvidedService))
                continue;

            Debug.Assert(
                serviceKind.HasFlag(ServiceKind.EarlyLoadedService) ||
                serviceKind.HasFlag(ServiceKind.BlockingEarlyLoadedService),
                "At this point, service must be either early loaded or blocking early loaded");

            if (serviceKind.HasFlag(ServiceKind.BlockingEarlyLoadedService))
            {
                blockingEarlyLoadingServices.Add(serviceType);
            }
            else
            {
                earlyLoadingServices.Add(serviceType);
            }

            var typeAsServiceT = ServiceHelpers.GetAsService(serviceType);
            dependencyServicesMap[serviceType] = ServiceHelpers.GetDependencies(typeAsServiceT)
                                                               .Select(x => typeof(Service<>).MakeGenericType(x))
                                                               .ToList();
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var whenBlockingComplete = Task.WhenAll(blockingEarlyLoadingServices.Select(x => getAsyncTaskMap[x]));
                while (await Task.WhenAny(whenBlockingComplete, Task.Delay(120000)) != whenBlockingComplete)
                {
                    if (NativeFunctions.MessageBoxW(
                            IntPtr.Zero,
                            "Dalamud is taking a long time to load. Would you like to continue without Dalamud?\n" +
                            "This can be caused by a faulty plugin, or a bug in Dalamud.",
                            "Dalamud",
                            NativeFunctions.MessageBoxType.IconWarning | NativeFunctions.MessageBoxType.YesNo) == 6)
                    {
                        throw new TimeoutException(
                            "Failed to load services in the given time limit, " +
                            "and the user chose to continue without Dalamud.");                        
                    }
                }

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
                        var depUnderlyingServiceType = dependency.GetGenericArguments().First();
                        var depResolveTask = getAsyncTaskMap.GetValueOrDefault(depUnderlyingServiceType);

                        if (depResolveTask == null)
                        {
                            Log.Error("{Type}: {Dependency} has no resolver task", serviceType.FullName!, dependency.FullName!);
                            Debug.Assert(false, $"No resolver for dependent service {depUnderlyingServiceType.FullName}");
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
                                        nameof(Service<IServiceType>.StartLoader),
                                        BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic,
                                        null,
                                        null,
                                        null));
                    servicesToLoad.Remove(serviceType);

#if DEBUG
                    tasks.Add(tasks.Last().ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                            return;
                        lock (LoadedServices)
                        {
                            LoadedServices.Add(serviceType);
                        }
                    }));
#endif
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

            var typeAsServiceT = ServiceHelpers.GetAsService(serviceType);
            dependencyServicesMap[serviceType] = ServiceHelpers.GetDependencies(typeAsServiceT);

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
        
#if DEBUG
        lock (LoadedServices)
        {
            LoadedServices.Clear();
        }
#endif

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
        var attr = type.GetCustomAttribute<ServiceAttribute>(true)?.GetType();
        if (attr == null)
            return ServiceKind.None;
        
        Debug.Assert(
            type.IsAssignableTo(typeof(IServiceType)),
            "Service did not inherit from IServiceType");

        if (attr.IsAssignableTo(typeof(BlockingEarlyLoadedServiceAttribute)))
            return ServiceKind.BlockingEarlyLoadedService;
        
        if (attr.IsAssignableTo(typeof(EarlyLoadedServiceAttribute)))
            return ServiceKind.EarlyLoadedService;
        
        if (attr.IsAssignableTo(typeof(ScopedServiceAttribute)))
            return ServiceKind.ScopedService;

        return ServiceKind.ProvidedService;
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
    public abstract class ServiceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAttribute"/> class.
        /// </summary>
        /// <param name="kind">The kind of the service.</param>
        protected ServiceAttribute(ServiceKind kind) => this.Kind = kind;

        /// <summary>
        /// Gets the kind of the service.
        /// </summary>
        public ServiceKind Kind { get; }
    }

    /// <summary>
    /// Indicates that the class is a service, that is provided by some other source.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProvidedServiceAttribute : ServiceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProvidedServiceAttribute"/> class.
        /// </summary>
        public ProvidedServiceAttribute()
            : base(ServiceKind.ProvidedService)
        {
        }
    }

    /// <summary>
    /// Indicates that the class is a service, and will be instantiated automatically on startup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EarlyLoadedServiceAttribute : ServiceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EarlyLoadedServiceAttribute"/> class.
        /// </summary>
        public EarlyLoadedServiceAttribute()
            : this(ServiceKind.EarlyLoadedService)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EarlyLoadedServiceAttribute"/> class.
        /// </summary>
        /// <param name="kind">The service kind.</param>
        protected EarlyLoadedServiceAttribute(ServiceKind kind)
            : base(kind)
        {
        }
    }

    /// <summary>
    /// Indicates that the class is a service, and will be instantiated automatically on startup,
    /// blocking game main thread until it completes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BlockingEarlyLoadedServiceAttribute : EarlyLoadedServiceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockingEarlyLoadedServiceAttribute"/> class.
        /// </summary>
        public BlockingEarlyLoadedServiceAttribute()
            : base(ServiceKind.BlockingEarlyLoadedService)
        {
        }
    }

    /// <summary>
    /// Indicates that the class is a service that will be created specifically for a
    /// service scope, and that it cannot be created outside of a scope.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedServiceAttribute : ServiceAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScopedServiceAttribute"/> class.
        /// </summary>
        public ScopedServiceAttribute()
            : base(ServiceKind.ScopedService)
        {
        }
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
