using System;
using System.Reflection;

using Dalamud.IoC;

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
        private static T? _object;

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

            _object = obj;

            RegisterInIoCContainer(_object);

            return _object;
        }

        public static T Set()
        {
            _object = (T)Activator.CreateInstance(typeof(T), true);

            RegisterInIoCContainer(_object);

            return _object;
        }

        public static T Set(params object[] args)
        {
            var obj = (T?)Activator.CreateInstance(typeof(T), args, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance);

            // ReSharper disable once JoinNullCheckWithUsage
            if (obj == null)
            {
                throw new Exception("what he fuc");
            }

            _object = obj;

            RegisterInIoCContainer(_object);

            return obj;
        }

        private static void RegisterInIoCContainer(T instance)
        {
            var type = typeof(T);
            var attr = type.GetCustomAttribute<PluginInterfaceAttribute>();

            if (attr == null)
            {
                return;
            }

            // attempt to get service locator
            var ioc = Service<Container>.GetNullable();
            if (ioc == null)
            {
                return;
            }

            ioc.RegisterSingleton(instance);
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the object instance isn't present in the service locator.</exception>
        public static T Get()
        {
            if (_object == null)
            {
                throw new InvalidOperationException($"{nameof(T)} hasn't been registered in the service locator!");
            }

            return _object;
        }

        /// <summary>
        /// Attempt to pull the instance out of the service locator.
        /// </summary>
        /// <returns>The object if registered, null otherwise.</returns>
        public static T? GetNullable()
        {
            return _object;
        }
    }
}
