using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;
using JetBrains.Annotations;

namespace Dalamud
{
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

        /// <summary>
        /// Gets task that gets completed when all blocking early loading services are done loading.
        /// </summary>
        public static Task BlockingResolved { get; } = BlockingServicesLoadedTaskCompletionSource.Task;

        /// <summary>
        /// Kicks off construction of services that can handle early loading.
        /// </summary>
        /// <returns>Task for initializing all services.</returns>
        public static async Task InitializeEarlyLoadableServices()
        {
            Service<ServiceContainer>.Provide(new ServiceContainer());

            var service = typeof(Service<>);
            var blockingEarlyLoadingServices = new List<Task>();

            var dependencyServicesMap = new Dictionary<Type, List<Type>>();
            var getAsyncTaskMap = new Dictionary<Type, Task>();

            foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attr = serviceType.GetCustomAttribute<Service>(true)?.GetType();
                if (attr?.IsAssignableTo(typeof(EarlyLoadedService)) != true)
                    continue;

                var getTask = (Task)service.MakeGenericType(serviceType).InvokeMember(
                    "GetAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                    null,
                    null,
                    null);
                if (attr.IsAssignableTo(typeof(BlockingEarlyLoadedService)))
                {
                    getAsyncTaskMap[serviceType] = getTask;
                    blockingEarlyLoadingServices.Add(getTask);
                }

                dependencyServicesMap[serviceType] =
                    (List<Type>)service
                                .MakeGenericType(serviceType)
                                .InvokeMember(
                                    "GetDependencyServices",
                                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                    null,
                                    null,
                                    null);
            }

            _ = Task.WhenAll(blockingEarlyLoadingServices).ContinueWith(x =>
            {
                try
                {
                    if (x.IsFaulted)
                        BlockingServicesLoadedTaskCompletionSource.SetException(x.Exception!);
                    else
                        BlockingServicesLoadedTaskCompletionSource.SetResult();
                }
                catch (Exception)
                {
                    // don't care, as this means task result/exception has already been set
                }
            }).ConfigureAwait(false);

            try
            {
                var tasks = new List<Task>();
                while (dependencyServicesMap.Any())
                {
                    tasks.Clear();
                    foreach (var (serviceType, dependencies) in dependencyServicesMap.ToList())
                    {
                        if (!dependencies.All(
                                x => !getAsyncTaskMap.ContainsKey(x) || getAsyncTaskMap[x].IsCompleted))
                            continue;

                        tasks.Add((Task)service.MakeGenericType(serviceType).InvokeMember(
                                      "StartLoader",
                                      BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                                      null,
                                      null,
                                      null));
                        dependencyServicesMap.Remove(serviceType);
                    }

                    if (!tasks.Any())
                        throw new InvalidOperationException("Unresolvable dependency cycle detected");

                    await Task.WhenAll(tasks);
                    foreach (var task in tasks)
                    {
                        if (task.IsFaulted)
                            throw task.Exception!;
                    }
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

                throw;
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
    }
}
