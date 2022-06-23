using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility.Timing;

namespace Dalamud
{
    /// <summary>
    /// Basic service locator.
    /// </summary>
    /// <remarks>
    /// Only used internally within Dalamud, if plugins need access to things it should be _only_ via DI.
    /// </remarks>
    /// <typeparam name="T">The class you want to store in the service locator.</typeparam>
    internal static class Service<T> where T : IServiceObject
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly TaskCompletionSource? InstanceTcs;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Task InstanceTask;

        // ReSharper disable once StaticMemberInGenericType
        private static T? instance;

        static Service()
        {
            if (!typeof(IProvidedServiceObject).IsAssignableFrom(typeof(T)))
            {
                ServiceManager.Log.Debug("Service<{0}>: Begin task", typeof(T).Name);
                InstanceTcs = null;
                InstanceTask = new Task(() =>
                {
                    using (Timings.Start($"{typeof(T).Namespace} Enable"))
                    {
                        const BindingFlags flags =
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;

                        ServiceManager.Log.Debug("Service<{0}>: Begin construction", typeof(T).Name);
                        try
                        {
                            var obj = (T)Activator.CreateInstance(
                                typeof(T), flags, null, new object[] {ServiceManager.TagInstance}, null, null);
                            SetInstanceObject(obj);
                            ServiceManager.Log.Debug("Service<{0}>: Construction complete", typeof(T).Name);
                        }
                        catch (Exception e)
                        {
                            ServiceManager.Log.Error(e, "Service<{0}>: Construction failure", typeof(T).Name);
                        }
                    }
                });
                InstanceTask.ConfigureAwait(false);
                InstanceTask.Start();
            }
            else
            {
                ServiceManager.Log.Debug("Service<{0}>: Placeholder set", typeof(T).Name);
                InstanceTcs = new TaskCompletionSource();
                InstanceTask = InstanceTcs.Task;
            }
        }

        /// <summary>
        /// Dummy function for calling static constructor.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Sets the type in the service locator to the given object.
        /// </summary>
        /// <param name="obj">Object to set.</param>
        public static void Provide(T obj)
        {
            Debug.Assert(
                typeof(IProvidedServiceObject).IsAssignableFrom(typeof(T)),
                "Provide is usable only when the service is an IProvidedServiceObject.");

            SetInstanceObject(obj);
            ServiceManager.Log.Debug("Service<{0}>: Provided", typeof(T).Name);
        }

        /// <summary>
        /// Pull the instance out of the service locator, waiting if necessary.
        /// </summary>
        /// <returns>The object.</returns>
        public static T Get()
        {
            InstanceTask.Wait();
            if (InstanceTask.IsFaulted)
                throw InstanceTask.Exception!;
            return instance;
        }

        /// <summary>
        /// Pull the instance out of the service locator, waiting if necessary.
        /// </summary>
        /// <returns>The object.</returns>
        public static async Task<T> GetAsync()
        {
            await InstanceTask;
            return instance;
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable() => instance;

        private static void SetInstanceObject(T newInstance)
        {
            instance = newInstance;
            InstanceTcs?.SetResult();

            var availableToPlugins = RegisterInIoCContainer(newInstance);

            ServiceManager.Log.Information(
                availableToPlugins
                    ? $"Registered {typeof(T).FullName} into service locator and exposed to plugins"
                    : $"Registered {typeof(T).FullName} into service locator privately");
        }

        private static bool RegisterInIoCContainer(T newInstance)
        {
            var attr = typeof(T).GetCustomAttribute<PluginInterfaceAttribute>();
            if (attr == null)
                return false;

            var ioc = Service<ServiceContainer>.GetNullable();
            if (ioc == null)
                return false;

            ioc.RegisterSingleton(newInstance);

            return true;
        }
    }
}
