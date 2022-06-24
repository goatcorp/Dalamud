using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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
        private static readonly TaskCompletionSource<T>? InstanceTcs;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Task<T> InstanceTask;

        static Service()
        {
            if (!typeof(IProvidedServiceObject).IsAssignableFrom(typeof(T)))
            {
                InstanceTcs = null;
                InstanceTask = Task.Run(async () =>
                {
                    using (Timings.Start($"{typeof(T).Namespace} Enable"))
                    {
                        ServiceManager.Log.Debug("Service<{0}>: Begin construction", typeof(T).Name);
                        try
                        {
                            var x = await ConstructObject();
                            ServiceManager.Log.Debug("Service<{0}>: Construction complete", typeof(T).Name);
                            return x;
                        }
                        catch (Exception e)
                        {
                            ServiceManager.Log.Error(e, "Service<{0}>: Construction failure", typeof(T).Name);
                            throw;
                        }
                    }
                });
            }
            else
            {
                ServiceManager.Log.Debug("Service<{0}>: Placeholder set", typeof(T).Name);
                InstanceTcs = new TaskCompletionSource<T>();
                InstanceTask = InstanceTcs.Task;
            }

            var attr = typeof(T).GetCustomAttribute<PluginInterfaceAttribute>();
            if (attr != null)
            {
                Service<ServiceContainer>.Get().RegisterSingleton(InstanceTask);
                ServiceManager.Log.Debug("Service<{0}>: Exposed to plugins", typeof(T).Name);
            }
        }

        /// <summary>
        /// Dummy function for calling static constructor.
        /// </summary>
        public static void Initialize() { }

        /// <summary>
        /// Sets the type in the service locator to the given object.
        /// Only applicable for Service&lt;IProvidedServiceObject&gt;.
        /// </summary>
        /// <param name="obj">Object to set.</param>
        public static void Provide(T obj)
        {
            Debug.Assert(
                typeof(IProvidedServiceObject).IsAssignableFrom(typeof(T)),
                "Provide is usable only when the service is an IProvidedServiceObject.");

            InstanceTcs?.SetResult(obj);
            ServiceManager.Log.Debug("Service<{0}>: Provided", typeof(T).Name);
        }

        /// <summary>
        /// Pull the instance out of the service locator, waiting if necessary.
        /// </summary>
        /// <returns>The object.</returns>
        public static T Get()
        {
            if (!InstanceTask.IsCompleted)
                InstanceTask.Wait();
            return InstanceTask.Result;
        }

        /// <summary>
        /// Pull the instance out of the service locator, waiting if necessary.
        /// </summary>
        /// <returns>The object.</returns>
        public static async Task<T> GetAsync() => await InstanceTask;

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable() => InstanceTask.IsCompleted ? InstanceTask.Result : default;

        private static async Task<object?> GetServiceObjectConstructArgument(Type type)
        {
            if (type == typeof(ServiceManager.Tag))
                return null;

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

        private static async Task<T> ConstructObject()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;

            foreach (var ctor in typeof(T).GetConstructors(flags))
            {
                var arginfo = ctor.GetParameters();
                if (arginfo[0].ParameterType != typeof(ServiceManager.Tag))
                    continue;

                var instance = (T)FormatterServices.GetUninitializedObject(typeof(T));
                foreach (var prop in typeof(T).GetProperties(
                             BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
                             BindingFlags.NonPublic))
                {
                    if (!prop.GetCustomAttributes(typeof(ServiceAttribute)).Any())
                        continue;
                    prop.SetValue(
                        instance,
                        await GetServiceObjectConstructArgument(prop.PropertyType),
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null,
                        null,
                        null);
                }

                var args = await Task.WhenAll(arginfo.Select(x => GetServiceObjectConstructArgument(x.ParameterType)));
                ctor.Invoke(instance, args);
                return instance;
            }

            throw new InvalidOperationException("Missing constructor whose first parameter type is ServiceManager.Tag");
        }
    }
}
