using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Serilog;

namespace Dalamud.IoC.Internal
{
    /// <summary>
    /// A simple singleton-only IOC container that provides (optional) version-based dependency resolution.
    /// </summary>
    internal class ServiceContainer : IServiceProvider
    {
        private readonly Dictionary<Type, ObjectInstance> instances = new();

        /// <summary>
        /// Register a singleton object of any type into the current IOC container.
        /// </summary>
        /// <param name="instance">The existing instance to register in the container.</param>
        /// <typeparam name="T">The interface to register.</typeparam>
        public void RegisterSingleton<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            this.instances[typeof(T)] = new(instance);
        }

        /// <summary>
        /// Create an object.
        /// </summary>
        /// <param name="objectType">The type of object to create.</param>
        /// <param name="scopedObjects">Scoped objects to be included in the constructor.</param>
        /// <returns>The created object.</returns>
        public object? Create(Type objectType, params object[] scopedObjects)
        {
            var ctor = this.FindApplicableCtor(objectType, scopedObjects);
            if (ctor == null)
            {
                Log.Error("Failed to create {TypeName}, unable to find one or more services to satisfy the dependencies in the ctor", objectType.FullName);
                return null;
            }

            // validate dependency versions (if they exist)
            var parameters = ctor.GetParameters().Select(p =>
            {
                var parameterType = p.ParameterType;
                var requiredVersion = p.GetCustomAttribute(typeof(RequiredVersionAttribute)) as RequiredVersionAttribute;
                return (parameterType, requiredVersion);
            });

            var versionCheck = parameters.Any(p =>
            {
                // if there's no required version, ignore it
                if (p.requiredVersion == null)
                    return true;

                // if there's no requested version, ignore it
                var declVersion = p.parameterType.GetCustomAttribute<InterfaceVersionAttribute>();
                if (declVersion == null)
                    return true;

                if (declVersion.Version == p.requiredVersion.Version)
                    return true;

                Log.Error(
                    "Requested version {ReqVersion} does not match the implemented version {ImplVersion} for param type {ParamType}",
                    p.requiredVersion.Version,
                    declVersion.Version,
                    p.parameterType.FullName);

                return false;
            });

            if (!versionCheck)
            {
                Log.Error("Failed to create {TypeName}, a RequestedVersion could not be satisfied", objectType.FullName);
                return null;
            }

            var resolvedParams = parameters
                .Select(p =>
                {
                    var service = this.GetService(p.parameterType, scopedObjects);

                    if (service == null)
                    {
                        Log.Error("Requested service type {TypeName} was not available (null)", p.parameterType.FullName);
                    }

                    return service;
                })
                .ToArray();

            var hasNull = resolvedParams.Any(p => p == null);
            if (hasNull)
            {
                Log.Error("Failed to create {TypeName}, a requested service type could not be satisfied", objectType.FullName);
                return null;
            }

            return Activator.CreateInstance(objectType, resolvedParams);
        }

        /// <inheritdoc/>
        object? IServiceProvider.GetService(Type serviceType) => this.GetService(serviceType);

        private object? GetService(Type serviceType, object[] scopedObjects)
        {
            var singletonService = this.GetService(serviceType);
            if (singletonService != null)
            {
                return singletonService;
            }

            // resolve dependency from scoped objects
            var scoped = scopedObjects.FirstOrDefault(o => o.GetType() == serviceType);
            if (scoped == default)
            {
                return null;
            }

            return scoped;
        }

        private object? GetService(Type serviceType)
        {
            var hasInstance = this.instances.TryGetValue(serviceType, out var service);
            if (hasInstance && service.Instance.IsAlive)
            {
                return service.Instance.Target;
            }

            return null;
        }

        private ConstructorInfo? FindApplicableCtor(Type type, object[] scopedObjects)
        {
            // get a list of all the available types: scoped and singleton
            var types = scopedObjects
                .Select(o => o.GetType())
                .Union(this.instances.Keys)
                .ToArray();

            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var ctor in ctors)
            {
                if (this.ValidateCtor(ctor, types))
                {
                    return ctor;
                }
            }

            return null;
        }

        private bool ValidateCtor(ConstructorInfo ctor, Type[] types)
        {
            var parameters = ctor.GetParameters();
            foreach (var parameter in parameters)
            {
                var contains = types.Contains(parameter.ParameterType);
                if (!contains)
                {
                    Log.Error("Failed to validate {TypeName}, unable to find any services that satisfy the type", parameter.ParameterType.FullName);
                    return false;
                }
            }

            return true;
        }
    }
}
