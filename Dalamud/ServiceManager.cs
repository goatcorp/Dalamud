using System.Reflection;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;

namespace Dalamud
{
    internal static class ServiceManager
    {
        /// <summary>
        /// Static log facility for Service{T}, to avoid duplicate instances for different types.
        /// </summary>
        public static readonly ModuleLog Log = new("SVC");

        /// <summary>
        /// Kicks off construction of services that can handle early loading.
        /// </summary>
        public static void InitializeEarlyLoadableServices()
        {
            Service<ServiceContainer>.Provide(new ServiceContainer());

            var service = typeof(Service<>);
            foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (serviceType.GetInterface(nameof(IEarlyLoadableServiceObject)) == null)
                    continue;

                service.MakeGenericType(serviceType).InvokeMember(
                    "Initialize",
                    BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                    null,
                    null,
                    null);
            }
        }

        /// <summary>
        /// Tag class to identify constructors to be called from Dalamud core.
        /// </summary>
        public class Tag
        {
            private Tag()
            {
            }
        }
    }
}
