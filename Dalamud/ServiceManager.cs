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
        public static void InitializeEarlyLoadableServices()
        {
            Service<ServiceContainer>.Provide(new ServiceContainer());

            var service = typeof(Service<>);
            var blockingEarlyLoadingServices = new List<Task>();
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
                    blockingEarlyLoadingServices.Add(getTask);
            }

            Task.WhenAll(blockingEarlyLoadingServices)
                .ContinueWith(_ => BlockingServicesLoadedTaskCompletionSource.SetResult());
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
