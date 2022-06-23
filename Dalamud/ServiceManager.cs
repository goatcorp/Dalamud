using System.Reflection;
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
        /// Instance of <see cref="Tag"/>.
        /// </summary>
        public static readonly Tag TagInstance = new Tag();

        /// <summary>
        /// Kicks off construction of services that can handle early loading.
        /// </summary>
        public static void InitializeEarlyLoadableServices()
        {
            var service = typeof(Service<>);
            foreach (var serviceType in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (serviceType.GetInterface(nameof(IEarlyLoadableServiceObject)) == null)
                    continue;

                Log.Debug("Found early loadable service: {0}", serviceType.Name);
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
            /// <summary>
            /// Initializes a new instance of the <see cref="Tag"/> class.
            /// </summary>
            internal Tag()
            {
            }
        }
    }
}
