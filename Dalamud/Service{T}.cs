using System;
using System.Reflection;

using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Logging.Internal;

namespace Dalamud
{
    /// <summary>
    /// Basic service locator.
    /// </summary>
    /// <remarks>
    /// Only used internally within Dalamud, if plugins need access to things it should be _only_ via DI.
    /// </remarks>
    /// <typeparam name="T">The class you want to store in the service locator.</typeparam>
    internal static class Service<T> where T : class
    {
        private static readonly ModuleLog Log = new("SVC");

        private static T? instance;

        static Service()
        {
        }

        /// <summary>
        /// Sets the type in the service locator to the given object.
        /// </summary>
        /// <param name="obj">Object to set.</param>
        /// <returns>The set object.</returns>
        public static T Set(T obj)
        {
            SetInstanceObject(obj);

            return instance!;
        }

        /// <summary>
        /// Sets the type in the service locator via the default parameterless constructor.
        /// </summary>
        /// <returns>The set object.</returns>
        public static T Set()
        {
            if (instance != null)
                throw new Exception($"Service {typeof(T).FullName} was set twice");

            var obj = (T?)Activator.CreateInstance(typeof(T), true);

            SetInstanceObject(obj);

            return instance!;
        }

        /// <summary>
        /// Sets a type in the service locator via a constructor with the given parameter types.
        /// </summary>
        /// <param name="args">Constructor arguments.</param>
        /// <returns>The set object.</returns>
        public static T Set(params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args), $"Service locator was passed a null for type {typeof(T).FullName} parameterized constructor ");
            }

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
            var obj = (T?)Activator.CreateInstance(typeof(T), flags, null, args, null, null);

            SetInstanceObject(obj);

            return obj;
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the object instance is not present in the service locator.</exception>
        public static T Get()
        {
            return instance ?? throw new InvalidOperationException($"{typeof(T).FullName} has not been registered in the service locator!");
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable()
        {
            return instance;
        }

        private static void SetInstanceObject(T instance)
        {
            Service<T>.instance = instance ?? throw new ArgumentNullException(nameof(instance), $"Service locator received a null for type {typeof(T).FullName}");

            var availableToPlugins = RegisterInIoCContainer(instance);

            if (availableToPlugins)
                Log.Information($"Registered {typeof(T).FullName} into service locator and exposed to plugins");
            else
                Log.Information($"Registered {typeof(T).FullName} into service locator privately");
        }

        private static bool RegisterInIoCContainer(T instance)
        {
            var attr = typeof(T).GetCustomAttribute<PluginInterfaceAttribute>();
            if (attr == null)
            {
                return false;
            }

            var ioc = Service<ServiceContainer>.GetNullable();
            if (ioc == null)
            {
                return false;
            }

            ioc.RegisterSingleton(instance);

            return true;
        }
    }
}
