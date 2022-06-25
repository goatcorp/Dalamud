using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        private static readonly TaskCompletionSource<T> InstanceTcs = new();

        static Service()
        {
            var exposeToPlugins = typeof(T).GetCustomAttribute<PluginInterfaceAttribute>() != null;
            if (exposeToPlugins)
                ServiceManager.Log.Debug("Service<{0}>: Static ctor called; will be exposed to plugins", typeof(T).Name);
            else
                ServiceManager.Log.Debug("Service<{0}>: Static ctor called", typeof(T).Name);

            if (exposeToPlugins)
                Service<ServiceContainer>.Get().RegisterSingleton(InstanceTcs.Task);
        }

        /// <summary>
        /// Initializes the service.
        /// </summary>
        /// <returns>The object.</returns>
        [UsedImplicitly]
        public static Task<T> StartLoader()
        {
            var attr = typeof(T).GetCustomAttribute<ServiceManager.Service>(true)?.GetType();
            if (attr?.IsAssignableTo(typeof(ServiceManager.EarlyLoadedService)) != true)
                throw new InvalidOperationException($"{typeof(T).Name} is not an EarlyLoadedService");

            return Task.Run(Timings.AttachTimingHandle(async () =>
            {
                ServiceManager.Log.Debug("Service<{0}>: Begin construction", typeof(T).Name);
                try
                {
                    var x = await ConstructObject();
                    if (attr?.IsAssignableTo(typeof(ServiceManager.BlockingEarlyLoadedService)) == true)
                        ServiceManager.Log.Debug("Service<{0}>: Construction complete", typeof(T).Name);
                    InstanceTcs.SetResult(x);
                    return x;
                }
                catch (Exception e)
                {
                    InstanceTcs.SetException(e);
                    if (attr?.IsAssignableTo(typeof(ServiceManager.BlockingEarlyLoadedService)) == true)
                        ServiceManager.Log.Error(e, "Service<{0}>: Construction failure", typeof(T).Name);
                    throw;
                }
            }));
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
            if (!InstanceTcs.Task.IsCompleted)
                InstanceTcs.Task.Wait();
            return InstanceTcs.Task.Result;
        }

        /// <summary>
        /// Pull the instance out of the service locator, waiting if necessary.
        /// </summary>
        /// <returns>The object.</returns>
        [UsedImplicitly]
        public static Task<T> GetAsync() => InstanceTcs.Task;

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable() => InstanceTcs.Task.IsCompleted ? InstanceTcs.Task.Result : default;

        /// <summary>
        /// Gets an enumerable containing Service&lt;T&gt;s that are required for this Service to initialize without blocking.
        /// </summary>
        /// <returns>List of dependency services.</returns>
        public static List<Type> GetDependencyServices()
        {
            var res = new List<Type>();
            res.AddRange(GetServiceConstructor()
                .GetParameters()
                .Select(x => x.ParameterType));
            res.AddRange(typeof(T)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(x => x.FieldType)
                .Where(x => x.GetCustomAttribute<ServiceManager.ServiceDependency>(true) != null));
            return res
                .Distinct()
                .Select(x => typeof(Service<>).MakeGenericType(x))
                .ToList();
        }

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

        private static ConstructorInfo GetServiceConstructor()
        {
            const BindingFlags ctorBindingFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
            return typeof(T)
                .GetConstructors(ctorBindingFlags)
                .Single(x => x.GetCustomAttributes(typeof(ServiceManager.ServiceConstructor), true).Any());
        }

        private static async Task<T> ConstructObject()
        {
            var ctor = GetServiceConstructor();
            var args = await Task.WhenAll(
                           ctor.GetParameters().Select(x => GetServiceObjectConstructArgument(x.ParameterType)));
            using (Timings.Start($"{typeof(T).Name} Construct"))
            {
                return (T)ctor.Invoke(args)!;
            }
        }
    }
}
