using System;
using System.Reflection;

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
            return _object;
        }

        public static T Set()
        {
            _object = (T)Activator.CreateInstance(typeof(T), true);

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

            return obj;
        }

        public static T Get()
        {
            if (_object == null)
            {
                throw new InvalidOperationException($"{nameof(T)} hasn't been registered!");
            }

            return _object;
        }
    }
}
