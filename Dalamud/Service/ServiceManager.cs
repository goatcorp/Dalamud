using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Storage;
using Dalamud.Utility;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

// API10 TODO: Move to Dalamud.Service namespace. Some plugins reflect this... including my own, oops. There's a todo
// for more reflective APIs, so I'll just leave it for now.
namespace Dalamud;

// TODO:
// - Unify dependency walking code(load/unload)
// - Visualize/output .dot or imgui thing

/// <summary>
/// Class to initialize <see cref="Service{T}"/>.
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
    private static readonly CancellationTokenSource UnloadCancellationTokenSource = new();

    private static ManualResetEvent unloadResetEvent = new(false);

    private static LoadingDialog loadingDialog = new();

    /// <summary>
    /// Delegate for registering startup blocker task.<br />
    /// Do not use this delegate outside the constructor.
    /// </summary>
    /// <param name="t">The blocker task.</param>
    /// <param name="justification">The justification for using this feature.</param>
    [InjectableType]
    public delegate void RegisterStartupBlockerDelegate(Task t, string justification);

    /// <summary>
    /// Delegate for registering services that should be unloaded before self.<br />
    /// Intended for use with <see cref="Plugin.Internal.PluginManager"/>. If you think you need to use this outside
    /// of that, consider having a discussion first.<br />
    /// Do not use this delegate outside the constructor.
    /// </summary>
    /// <param name="unloadAfter">Services that should be unloaded first.</param>
    /// <param name="justification">The justification for using this feature.</param>
    [InjectableType]
    public delegate void RegisterUnloadAfterDelegate(IEnumerable<Type> unloadAfter, string justification);
    
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
    /// Gets a cancellation token that will be cancelled once Dalamud needs to unload, be it due to a failure state
    /// during initialization or during regular operation.
    /// </summary>
    public static CancellationToken UnloadCancellationToken => UnloadCancellationTokenSource.Token;

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
    /// Gets the concrete types of services, i.e. the non-abstract non-interface types.
    /// </summary>
    /// <returns>The enumerable of service types, that may be enumerated only once per call.</returns>
    public static IEnumerable<Type> GetConcreteServiceTypes() =>
        Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.IsAssignableTo(typeof(IServiceType)) && !x.IsInterface && !x.IsAbstract);

    /// <summary>
    /// Kicks off construction of services that can handle early loading.
    /// </summary>
    /// <returns>Task for initializing all services.</returns>
    public static async Task InitializeEarlyLoadableServices()
    {
        using var serviceInitializeTimings = Timings.Start("Services Init");

        var earlyLoadingServices = new HashSet<Type>();
        var blockingEarlyLoadingServices = new HashSet<Type>();
        var providedServices = new HashSet<Type>();

        var dependencyServicesMap = new Dictionary<Type, List<Type>>();
        var getAsyncTaskMap = new Dictionary<Type, Task>();

        var serviceContainer = Service<ServiceContainer>.Get();
        
        foreach (var serviceType in GetConcreteServiceTypes())
        {
            var serviceKind = serviceType.GetServiceKind();

            CheckServiceTypeContracts(serviceType);

            // Let IoC know about the interfaces this service implements
            serviceContainer.RegisterInterfaces(serviceType);
            
            // Scoped service do not go through Service<T> and are never early loaded
            if (serviceKind.HasFlag(ServiceKind.ScopedService))
                continue;

            var genericWrappedServiceType = typeof(Service<>).MakeGenericType(serviceType);
            
            var getTask = (Task)genericWrappedServiceType
                                .InvokeMember(
                                    nameof(Service<IServiceType>.GetAsync),
                                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                    null,
                                    null,
                                    null);

            getAsyncTaskMap[serviceType] = getTask;

            // We don't actually need to load provided services, something else does
            if (serviceKind.HasFlag(ServiceKind.ProvidedService))
            {
                providedServices.Add(serviceType);
                continue;
            }

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
            dependencyServicesMap[serviceType] = ServiceHelpers.GetDependencies(typeAsServiceT, false)
                                                               .Select(x => typeof(Service<>).MakeGenericType(x))
                                                               .ToList();
        }

        var blockerTasks = new List<Task>();
        _ = Task.Run(async () =>
        {
            try
            {
                // Wait for all blocking constructors to complete first.
                await WaitWithTimeoutConsent(blockingEarlyLoadingServices.Select(x => getAsyncTaskMap[x]),
                    LoadingDialog.State.LoadingDalamud);

                // All the BlockingEarlyLoadedService constructors have been run,
                // and blockerTasks now will not change. Now wait for them.
                // Note that ServiceManager.CallWhenServicesReady does not get to register a blocker.
                await WaitWithTimeoutConsent(blockerTasks,
                    LoadingDialog.State.LoadingPlugins);

                Log.Verbose("=============== BLOCKINGSERVICES & TASKS INITIALIZED ===============");
                Timings.Event("BlockingServices Initialized");
                BlockingServicesLoadedTaskCompletionSource.SetResult();
                loadingDialog.HideAndJoin();
            }
            catch (Exception e)
            {
                try
                {
                    BlockingServicesLoadedTaskCompletionSource.SetException(e);
                }
                catch (InvalidOperationException)
                {
                    // ignored, may have been set by the try/catch below
                }

                Log.Error(e, "Failed resolving blocking services");
            }

            return;

            async Task WaitWithTimeoutConsent(IEnumerable<Task> tasksEnumerable, LoadingDialog.State state)
            {
                var tasks = tasksEnumerable.AsReadOnlyCollection();
                if (tasks.Count == 0)
                    return;
                
                // Time we wait until showing the loading dialog
                const int loadingDialogTimeout = 10000;

                var aggregatedTask = Task.WhenAll(tasks);
                while (await Task.WhenAny(aggregatedTask, Task.Delay(loadingDialogTimeout)) != aggregatedTask)
                {
                    loadingDialog.Show();
                    loadingDialog.CanHide = true;
                    loadingDialog.CurrentState = state;
                }
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

                    // This object will be used in a task. Each task must receive a new object.
                    var startLoaderArgs = new List<object>();
                    if (serviceType.GetCustomAttribute<BlockingEarlyLoadedServiceAttribute>() is not null)
                    {
                        startLoaderArgs.Add(
                            new RegisterStartupBlockerDelegate(
                                (task, justification) =>
                                {
#if DEBUG
                                    if (CurrentConstructorServiceType.Value != serviceType)
                                        throw new InvalidOperationException("Forbidden.");
#endif
                                    blockerTasks.Add(task);

                                    // No need to store the justification; the fact that the reason is specified is good enough.
                                    _ = justification;
                                }));
                    }

                    tasks.Add((Task)typeof(Service<>)
                                    .MakeGenericType(serviceType)
                                    .InvokeMember(
                                        nameof(Service<IServiceType>.StartLoader),
                                        BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic,
                                        null,
                                        null,
                                        new object[] { startLoaderArgs }));
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
                {
                    // No more services we can start loading for now.
                    // Either we're waiting for provided services, or there's a dependency cycle.
                    providedServices.RemoveWhere(x => getAsyncTaskMap[x].IsCompleted);
                    if (providedServices.Any())
                        await Task.WhenAny(providedServices.Select(x => getAsyncTaskMap[x]));
                    else
                        throw new InvalidOperationException("Unresolvable dependency cycle detected");
                    continue;
                }

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
            UnloadCancellationTokenSource.Cancel();

            Log.Error(e, "Failed resolving services");
            try
            {
                BlockingServicesLoadedTaskCompletionSource.SetException(e);
                loadingDialog.HideAndJoin();
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
        UnloadCancellationTokenSource.Cancel();
        
        var framework = Service<Framework>.GetNullable(Service<Framework>.ExceptionPropagationMode.None);
        if (framework is { IsInFrameworkUpdateThread: false, IsFrameworkUnloading: false })
        {
            framework.RunOnFrameworkThread(UnloadAllServices).Wait();
            return;
        }

        unloadResetEvent.Reset();

        var dependencyServicesMap = new Dictionary<Type, IReadOnlyCollection<Type>>();
        var allToUnload = new HashSet<Type>();
        var unloadOrder = new List<Type>();
        
        Log.Information("==== COLLECTING SERVICES TO UNLOAD ====");
        
        foreach (var serviceType in GetConcreteServiceTypes())
        {
            if (!serviceType.IsAssignableTo(typeof(IServiceType)))
                continue;
            
            // Scoped services shall never be unloaded here.
            // Their lifetime must be managed by the IServiceScope that owns them. If it leaks, it's their fault.
            if (serviceType.GetServiceKind() == ServiceKind.ScopedService)
                continue;

            Log.Verbose("Calling GetDependencyServices for '{ServiceName}'", serviceType.FullName!);

            var typeAsServiceT = ServiceHelpers.GetAsService(serviceType);
            dependencyServicesMap[serviceType] = ServiceHelpers.GetDependencies(typeAsServiceT, true);

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

    /// <summary>Validate service type contracts, and throws exceptions accordingly.</summary>
    /// <param name="serviceType">An instance of <see cref="Type"/> that is supposed to be a service type.</param>
    /// <remarks>Does nothing on non-debug builds.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckServiceTypeContracts(Type serviceType)
    {
#if DEBUG
        try
        {
            if (!serviceType.IsAssignableTo(typeof(IServiceType)))
                throw new InvalidOperationException($"Non-{nameof(IServiceType)} passed.");
            if (serviceType.GetServiceKind() == ServiceKind.None)
                throw new InvalidOperationException("Service type is not specified.");

            var isServiceDisposable =
                serviceType.IsAssignableTo(typeof(IInternalDisposableService));
            var isAnyDisposable =
                isServiceDisposable
                || serviceType.IsAssignableTo(typeof(IDisposable))
                || serviceType.IsAssignableTo(typeof(IAsyncDisposable)); 
            if (isAnyDisposable && !isServiceDisposable)
            {
                throw new InvalidOperationException(
                    $"A service must be an {nameof(IInternalDisposableService)} without specifying " +
                    $"{nameof(IDisposable)} nor {nameof(IAsyncDisposable)} if it is purely meant to be a service, " +
                    $"or an {nameof(IPublicDisposableService)} if it also is allowed to be constructed not as a " +
                    $"service to be used elsewhere and has to offer {nameof(IDisposable)} or " +
                    $"{nameof(IAsyncDisposable)}. See {nameof(ReliableFileStorage)} for an example of " +
                    $"{nameof(IPublicDisposableService)}.");
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"{serviceType.Name}: {e.Message}");
        }
#endif
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
        /// <param name="blockReason">Reason of blocking the game startup.</param>
        public BlockingEarlyLoadedServiceAttribute(string blockReason)
            : base(ServiceKind.BlockingEarlyLoadedService)
        {
            this.BlockReason = blockReason;
        }

        /// <summary>Gets the reason of blocking the startup of the game.</summary>
        public string BlockReason { get; }
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
    /// Indicates that the method should be called when the services given in the marked method's parameters are ready.
    /// This will be executed immediately after the constructor has run, if all services specified as its parameters
    /// are already ready, or no parameter is given.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class CallWhenServicesReady : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallWhenServicesReady"/> class.
        /// </summary>
        /// <param name="justification">Specify the reason here.</param>
        public CallWhenServicesReady(string justification)
        {
            // No need to store the justification; the fact that the reason is specified is good enough.
            _ = justification;
        }
    }

    /// <summary>
    /// Indicates that something is a candidate for being considered as an injected parameter for constructors.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Delegate
        | AttributeTargets.Class
        | AttributeTargets.Struct
        | AttributeTargets.Enum
        | AttributeTargets.Interface)]
    public class InjectableTypeAttribute : Attribute
    {
    }
}
