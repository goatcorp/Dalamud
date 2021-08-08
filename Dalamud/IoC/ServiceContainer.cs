using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Serilog;

namespace Dalamud.IoC
{
    /// <summary>
    /// A simple singleton-only IOC container that provides (optional) version-based dependency resolution.
    /// </summary>
    internal class ServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, ObjectInstance> _objectInstances = new();

        /// <summary>
        /// Register a singleton object of any type into the current IOC container
        /// </summary>
        /// <param name="instance">The existing instance to register in the container</param>
        /// <typeparam name="T">The interface to register</typeparam>
        public void RegisterSingleton<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            this._objectInstances[typeof(T)] = new(instance);
        }

        /// <summary>
        /// Register a singleton object of any type and implementing interface into the current IOC container.
        /// </summary>
        /// <param name="impl"></param>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TImpl"></typeparam>
        public void RegisterSingleton<TInterface, TImpl>(TImpl impl)
        {
            if (impl == null)
            {
                throw new ArgumentNullException(nameof(impl));
            }

            this._objectInstances[typeof(TInterface)] = new(impl);
        }

        private ConstructorInfo? FindApplicableCtor<T>(object[] scopedObjects) where T : class
        {
            var type = typeof(T);

            return this.FindApplicableCtor(type, scopedObjects);
        }

        private ConstructorInfo? FindApplicableCtor(Type type, object[] scopedObjects)
        {
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            // get a list of all the available types (scoped + singleton)
            var types = scopedObjects
                        .Select(x => x.GetType())
                        .Union(this._objectInstances.Select(x => x.Key));

            // todo: this is a bit shit and is more of a first pass for now
            foreach (var ctor in ctors)
            {
                var @params = ctor.GetParameters();

                var failed = @params.Any(p => !types.Contains(p.ParameterType));

                if (!failed)
                {
                    return ctor;
                }
            }

            return null;
        }

        /// <summary>
        /// Create an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? Create<T>(params object[] scopedObjects) where T : class
        {
            var type = typeof(T);

            return this.Create(type, scopedObjects) as T;
        }

        public object? Create(Type objectType, params object[] scopedObjects)
        {
            var ctor = this.FindApplicableCtor(objectType, scopedObjects);
            if (ctor == null)
            {
                Log.Error(
                    "failed to create {TypeName}, unable to find any services to satisfy the dependencies in the ctor",
                    objectType.FullName
                );

                return null;
            }

            // validate dependency versions (if they exist)
            var @params = ctor.GetParameters().Select(p =>
            {
                var attr = p.GetCustomAttribute(typeof(RequiredVersionAttribute)) as RequiredVersionAttribute;

                return new
                {
                    p.ParameterType,
                    RequiredVersion = attr,
                };
            });

            var versionCheck = @params.Any(p =>
            {
                var declVersion = p.ParameterType.GetCustomAttribute(typeof(InterfaceVersionAttribute)) as InterfaceVersionAttribute;

                // if there's no requested/required version, just ignore it
                if (p.RequiredVersion == null || declVersion == null)
                {
                    return true;
                }

                if (declVersion.Version == p.RequiredVersion.Version)
                {
                    return true;
                }

                Log.Error(
                    "requested version: {ReqVersion} does not match the impl version: {ImplVersion} for param type {ParamType}",
                    p.RequiredVersion.Version,
                    declVersion.Version,
                    p.ParameterType.FullName
                );

                return false;
            });

            if (!versionCheck)
            {
                Log.Error(
                    "failed to create {TypeName}, a RequestedVersion could not be satisfied",
                    objectType.FullName
                );

                return null;
            }

            var resolvedParams = @params.Select(p => this.GetService(p.ParameterType, scopedObjects)).ToArray();

            return Activator.CreateInstance(objectType, resolvedParams);
        }

        public object? GetService(Type serviceType)
        {
            var weakRef = this._objectInstances.TryGetValue(serviceType, out var service);
            if (weakRef && service.Instance.IsAlive)
            {
                return service.Instance.Target;
            }

            return null;
        }

        private object? GetService(Type serviceType, object[] scopedObjects)
        {
            var singletonService = this.GetService(serviceType);
            if (singletonService != null)
            {
                return singletonService;
            }

            // resolve dependency from scoped objects
            return scopedObjects.FirstOrDefault(x => x.GetType() == serviceType);
        }
    }
}
