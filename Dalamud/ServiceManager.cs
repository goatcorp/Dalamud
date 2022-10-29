using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

namespace Dalamud;

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

            Service<SigScanner>.Provide(
                new SigScanner(
                    true, new FileInfo(Path.Combine(cacheDir.FullName, $"{startInfo.GameVersion}.json"))));
            LoadedServices.Add(typeof(SigScanner));
        }

        using (Timings.Start("CS Resolver Init"))
        {
            FFXIVClientStructs.Resolver.InitializeParallel(
                new FileInfo(Path.Combine(cacheDir.FullName, $"{startInfo.GameVersion}_cs.json")));
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

        foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attr = serviceType.GetCustomAttribute<Service>(true)?.GetType();
            if (attr?.IsAssignableTo(typeof(EarlyLoadedService)) != true)
                continue;

            var getTask = (Task)typeof(Service<>)
                                .MakeGenericType(serviceType)
                                .InvokeMember(
                                    "GetAsync",
                                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                    null,
                                    null,
                                    null);

            if (attr.IsAssignableTo(typeof(BlockingEarlyLoadedService)))
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
                    if (dependencyServicesMap[serviceType].Any(
                            x => getAsyncTaskMap.GetValueOrDefault(x)?.IsCompleted == false))
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

        lock (LoadedServices)
        {
            while (LoadedServices.Any())
            {
                var serviceType = LoadedServices.Last();
                LoadedServices.RemoveAt(LoadedServices.Count - 1);

                typeof(Service<>)
                    .MakeGenericType(serviceType)
                    .InvokeMember(
                        "Unset",
                        BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic,
                        null,
                        null,
                        null);
            }
        }
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
    /// Indicates that the method should be called when the services given in the constructor are ready.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class CallWhenServicesReady : Attribute
    {
    }
}
