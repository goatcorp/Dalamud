using System;
using System.Reflection;

using Dalamud.IoC;
using Serilog;

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
        private static T? instance;

        static Service()
        {
        }

        public static T Set(T obj)
        {
            // ReSharper disable once JoinNullCheckWithUsage
            if (obj == null)
            {
                throw new ArgumentNullException($"{nameof(obj)} is null!");
            }

            SetInstanceObject(obj);

            return instance!;
        }

        public static T Set()
        {
            var obj = (T)Activator.CreateInstance(typeof(T), true);

            SetInstanceObject(obj!);

            return instance!;
        }

        public static T Set(params object[] args)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.OptionalParamBinding;
            var obj = (T?)Activator.CreateInstance(typeof(T), flags, null, args, null, null);

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "This should not happen");
            }

            // ReSharper disable once JoinNullCheckWithUsage
            if (obj == null)
            {
                throw new Exception("what he fuc");
            }

            SetInstanceObject(obj);

            return obj;
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the object instance isn't present in the service locator.</exception>
        public static T Get()
        {
            if (instance == null)
            {
                throw new InvalidOperationException($"{typeof(T).FullName} hasn't been registered in the service locator!");
            }

            return instance;
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
            Service<T>.instance = instance;
            var availableToPlugins = RegisterInIoCContainer(instance);

            if (availableToPlugins)
            {
                Log.Information("Registered {ObjectType} into service locator & exposed to plugins!", typeof(T).FullName);
                return;
            }

            Log.Information("Registered {ObjectType} into service locator privately!", typeof(T).FullName);
        }

        private static bool RegisterInIoCContainer(T instance)
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<PluginInterfaceAttribute>();

            if (attr == null)
            {
                return false;
            }

            // attempt to get service locator
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
