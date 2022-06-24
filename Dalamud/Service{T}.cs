using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Utility.Timing;
using JetBrains.Annotations;

namespace Dalamud
{
    /// <summary>
    /// Basic service locator.
    /// </summary>
    /// <remarks>
    /// Only used internally within Dalamud, if plugins need access to things it should be _only_ via DI.
    /// </remarks>
    /// <typeparam name="T">The class you want to store in the service locator.</typeparam>
    internal static class Service<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly TaskCompletionSource<T>? InstanceTcs;

        // ReSharper disable once StaticMemberInGenericType
        private static readonly Task<T> InstanceTask;

        static Service()
        {
            var exposeToPlugins = typeof(T).GetCustomAttribute<PluginInterfaceAttribute>() != null;
            if (exposeToPlugins)
                ServiceManager.Log.Debug("Service<{0}>: Static ctor called; will be exposed to plugins", typeof(T).Name);
            else
                ServiceManager.Log.Debug("Service<{0}>: Static ctor called", typeof(T).Name);

            var attr = typeof(T).GetCustomAttribute<ServiceManager.Service>(true)?.GetType();
            if (attr?.IsAssignableTo(typeof(ServiceManager.EarlyLoadedService)) == true)
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
                InstanceTcs = new TaskCompletionSource<T>();
                InstanceTask = InstanceTcs.Task;
            }

            if (exposeToPlugins)
                Service<ServiceContainer>.Get().RegisterSingleton(InstanceTask);
        }

        /// <summary>
        /// Sets the type in the service locator to the given object.
        /// </summary>
        /// <param name="obj">Object to set.</param>
        public static void Provide(T obj)
        {
            InstanceTcs!.SetResult(obj);
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
        [UsedImplicitly]
        public static async Task<T> GetAsync() => await InstanceTask;

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable() => InstanceTask.IsCompleted ? InstanceTask.Result : default;

        private static async Task<object?> GetServiceObjectConstructArgument(Type type)
        {
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
            const BindingFlags ctorBindingFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
            var ctor = typeof(T)
                       .GetConstructors(ctorBindingFlags)
                       .Single(x => x.GetCustomAttributes(typeof(ServiceManager.ServiceConstructor), true).Any());
            var args = await Task.WhenAll(
                           ctor.GetParameters().Select(x => GetServiceObjectConstructArgument(x.ParameterType)));
            return (T)ctor.Invoke(args)!;
        }
    }
}
