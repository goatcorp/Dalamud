using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Serilog;

namespace Dalamud.Plugin.Ipc.Internal;

/// <summary>
/// Binds attribute-annotated IPC members onto call gates.
/// </summary>
internal static class IpcBinder
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Creates subscriber bindings for an instance type.
    /// </summary>
    /// <typeparam name="T">The IPC type.</typeparam>
    /// <param name="pi">The plugin interface.</param>
    /// <param name="instance">The instance to bind.</param>
    /// <param name="prefix">The name prefix, or null for none.</param>
    /// <returns>The registration for this binding batch.</returns>
    public static IpcRegistrationImpl<T> CreateSubscribers<T>(IDalamudPluginInterface pi, T instance, string? prefix) where T : class
    {
        var reg = new IpcRegistrationImpl<T>(instance);
        BindSubscribers(pi, typeof(T), instance, prefix ?? string.Empty, InstanceFlags, reg);
        return reg;
    }

    /// <summary>
    /// Creates subscriber bindings for a static type.
    /// </summary>
    /// <param name="pi">The plugin interface.</param>
    /// <param name="staticType">The type whose static members should be bound.</param>
    /// <param name="prefix">The name prefix, or null for none.</param>
    /// <returns>The registration for this binding batch.</returns>
    public static IpcRegistrationImpl CreateSubscribersStatic(IDalamudPluginInterface pi, Type staticType, string? prefix)
    {
        var reg = new IpcRegistrationImpl();
        BindSubscribers(pi, staticType, null, prefix ?? string.Empty, StaticFlags, reg);
        return reg;
    }

    /// <summary>
    /// Creates provider bindings for an instance type.
    /// </summary>
    /// <typeparam name="T">The IPC type.</typeparam>
    /// <param name="pi">The plugin interface.</param>
    /// <param name="instance">The instance to register.</param>
    /// <param name="prefix">The name prefix, or null to use <see cref="IDalamudPluginInterface.InternalName"/>.</param>
    /// <returns>The registration for this binding batch.</returns>
    public static IpcRegistrationImpl<T> CreateProviders<T>(IDalamudPluginInterface pi, T instance, string? prefix) where T : class
    {
        var reg = new IpcRegistrationImpl<T>(instance);
        BindProviders(pi, typeof(T), instance, prefix ?? pi.InternalName, InstanceFlags, reg);
        return reg;
    }

    /// <summary>
    /// Creates provider bindings for a static type.
    /// </summary>
    /// <param name="pi">The plugin interface.</param>
    /// <param name="staticType">The type whose static members should be registered.</param>
    /// <param name="prefix">The name prefix, or null to use <see cref="IDalamudPluginInterface.InternalName"/>.</param>
    /// <returns>The registration for this binding batch.</returns>
    public static IpcRegistrationImpl CreateProvidersStatic(IDalamudPluginInterface pi, Type staticType, string? prefix)
    {
        var reg = new IpcRegistrationImpl();
        BindProviders(pi, staticType, null, prefix ?? pi.InternalName, StaticFlags, reg);
        return reg;
    }

    private static void BindSubscribers(
        IDalamudPluginInterface pi,
        Type type,
        object? instance,
        string createPrefix,
        BindingFlags flags,
        IpcRegistrationImpl reg)
    {
        var typePrefix = type.GetCustomAttribute<IpcPrefixAttribute>()?.Prefix;
        var pluginName = pi.InternalName;

        foreach (var member in EnumerateFieldsAndProperties(type, flags))
        {
            var attr = member.GetCustomAttribute<IpcAttribute>();
            if (attr == null)
                continue;

            try
            {
                var tag = IpcNameResolver.Resolve(
                    attr.Name,
                    attr.ApplyPrefix,
                    member.Name,
                    typePrefix,
                    createPrefix,
                    pluginName);

                var memberType = member.GetMemberType();
                if (!IsIpcCallableType(memberType, out var isAction, out var typeArgs))
                {
                    throw new InvalidOperationException($"[Ipc] {type.Name}.{member.Name} must be an IpcFunc or IpcAction type.");
                }

                object wrapper;
                if (isAction)
                {
                    var gate = GetSubscriber(pi, [.. typeArgs, typeof(object)], tag);
                    wrapper = Activator.CreateInstance(memberType, gate) ?? throw new InvalidOperationException($"Failed to create {memberType}");
                }
                else
                {
                    var gate = GetSubscriber(pi, typeArgs, tag);
                    wrapper = Activator.CreateInstance(memberType, gate) ?? throw new InvalidOperationException($"Failed to create {memberType}");
                }

                member.SetValue(instance, wrapper);
                reg.AddCallable((IIpcBoundCallable)wrapper);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Ipc] Failed to bind subscriber {Type}.{Member}", type.Name, member.Name);
                throw;
            }
        }

        foreach (var method in type.GetMethods(flags))
        {
            var attr = method.GetCustomAttribute<IpcEventAttribute>();
            if (attr == null)
                continue;

            if (method.ReturnType != typeof(void))
                throw new InvalidOperationException($"[IpcEvent] {type.Name}.{method.Name} must return void.");

            try
            {
                var tag = IpcNameResolver.Resolve(attr.Name, attr.ApplyPrefix, method.Name, typePrefix, createPrefix, pluginName);

                var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                if (paramTypes.Length > 8)
                    throw new InvalidOperationException("[IpcEvent] At most 8 parameters are supported.");

                var gateTypes = paramTypes.Append(typeof(object)).ToArray();
                var gate = GetSubscriber(pi, gateTypes, tag);
                var handler = method.CreateDelegate(
                    GetActionType(paramTypes),
                    method.IsStatic ? null : instance);

                gate.GetType().GetMethod("Subscribe")!.Invoke(gate, [handler]);

                reg.AddDisposeAction(() =>
                {
                    gate.GetType().GetMethod("Unsubscribe")!.Invoke(gate, [handler]);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Ipc] Failed to bind event subscriber {Type}.{Member}", type.Name, method.Name);
                throw;
            }
        }
    }

    private static void BindProviders(IDalamudPluginInterface pi, Type type, object? instance, string createPrefix, BindingFlags flags, IpcRegistrationImpl reg)
    {
        var typePrefix = type.GetCustomAttribute<IpcPrefixAttribute>()?.Prefix;
        var pluginName = pi.InternalName;

        foreach (var method in type.GetMethods(flags))
        {
            var attr = method.GetCustomAttribute<IpcAttribute>();
            if (attr == null)
                continue;

            try
            {
                var tag = IpcNameResolver.Resolve(attr.Name, attr.ApplyPrefix, method.Name, typePrefix, createPrefix, pluginName);

                var isAction = method.ReturnType == typeof(void);
                var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                if (paramTypes.Length > 8)
                    throw new InvalidOperationException("[Ipc] At most 8 parameters are supported.");

                var gateTypes = isAction ? [.. paramTypes, typeof(object)] : paramTypes.Append(method.ReturnType).ToArray();
                var gate = GetProvider(pi, gateTypes, tag);
                var delType = isAction ? GetActionType(paramTypes) : GetFuncType(paramTypes, method.ReturnType);
                var del = method.CreateDelegate(delType, method.IsStatic ? null : instance);

                if (isAction)
                    gate.GetType().GetMethod("RegisterAction")!.Invoke(gate, [del]);
                else
                    gate.GetType().GetMethod("RegisterFunc")!.Invoke(gate, [del]);

                reg.AddDisposeAction(() =>
                {
                    if (isAction)
                        gate.GetType().GetMethod("UnregisterAction")!.Invoke(gate, null);
                    else
                        gate.GetType().GetMethod("UnregisterFunc")!.Invoke(gate, null);
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Ipc] Failed to bind provider {Type}.{Member}", type.Name, method.Name);
                throw;
            }
        }

        foreach (var member in EnumerateFieldsAndProperties(type, flags))
        {
            var attr = member.GetCustomAttribute<IpcEventAttribute>();
            if (attr == null)
                continue;

            try
            {
                var tag = IpcNameResolver.Resolve(
                    attr.Name,
                    attr.ApplyPrefix,
                    member.Name,
                    typePrefix,
                    createPrefix,
                    pluginName);

                var memberType = member.GetMemberType();
                if (!IsIpcActionType(memberType, out var typeArgs))
                {
                    throw new InvalidOperationException(
                        $"[IpcEvent] {type.Name}.{member.Name} must be an IpcAction type.");
                }

                var gateTypes = typeArgs.Append(typeof(object)).ToArray();
                var gate = GetProvider(pi, gateTypes, tag);
                var sendMethod = gate.GetType().GetMethod("SendMessage")!;
                var sendDel = Delegate.CreateDelegate(GetActionType(typeArgs), gate, sendMethod);

                var createSender = memberType.GetMethod("CreateSender", BindingFlags.Public | BindingFlags.Static, null, [GetActionType(typeArgs)], null)!;
                var wrapper = createSender.Invoke(null, [sendDel])!;

                member.SetValue(instance, wrapper);
                reg.AddCallable((IIpcBoundCallable)wrapper);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Ipc] Failed to bind event provider {Type}.{Member}", type.Name, member.Name);
                throw;
            }
        }
    }

    private static object GetSubscriber(IDalamudPluginInterface pi, Type[] genericArgs, string tag)
    {
        var method = FindGetIpcMethod(pi.GetType(), "GetIpcSubscriber", genericArgs.Length)
                     ?? FindGetIpcMethod(typeof(IDalamudPluginInterface), "GetIpcSubscriber", genericArgs.Length)
                     ?? throw new InvalidOperationException(
                         $"Could not find GetIpcSubscriber with {genericArgs.Length} type args.");
        return method.MakeGenericMethod(genericArgs).Invoke(pi, [tag])!;
    }

    private static object GetProvider(IDalamudPluginInterface pi, Type[] genericArgs, string tag)
    {
        var method = FindGetIpcMethod(pi.GetType(), "GetIpcProvider", genericArgs.Length)
                     ?? FindGetIpcMethod(typeof(IDalamudPluginInterface), "GetIpcProvider", genericArgs.Length)
                     ?? throw new InvalidOperationException(
                         $"Could not find GetIpcProvider with {genericArgs.Length} type args.");
        return method.MakeGenericMethod(genericArgs).Invoke(pi, [tag])!;
    }

    private static MethodInfo? FindGetIpcMethod(Type type, string name, int genericArgCount)
    {
        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
        {
            if (method.Name == name && method.IsGenericMethodDefinition && method.GetGenericArguments().Length == genericArgCount)
            {
                return method;
            }
        }

        return null;
    }

    private static bool IsIpcCallableType(Type type, out bool isAction, out Type[] typeArgs)
    {
        isAction = false;
        typeArgs = [];

        if (!type.IsGenericType && type == typeof(IpcAction))
        {
            isAction = true;
            return true;
        }

        if (!type.IsGenericType)
            return false;

        var def = type.GetGenericTypeDefinition();
        if (def.Name.StartsWith("IpcAction", StringComparison.Ordinal)
            && def.Namespace == typeof(IpcAction).Namespace)
        {
            isAction = true;
            typeArgs = type.GetGenericArguments();
            return true;
        }

        if (def.Name.StartsWith("IpcFunc", StringComparison.Ordinal)
            && def.Namespace == typeof(IpcFunc<>).Namespace)
        {
            isAction = false;
            typeArgs = type.GetGenericArguments();
            return true;
        }

        return false;
    }

    private static bool IsIpcActionType(Type type, out Type[] typeArgs)
    {
        typeArgs = [];
        if (type == typeof(IpcAction))
            return true;

        if (!type.IsGenericType)
            return false;

        var def = type.GetGenericTypeDefinition();
        if (def.Name.StartsWith("IpcAction", StringComparison.Ordinal)
            && def.Namespace == typeof(IpcAction).Namespace)
        {
            typeArgs = type.GetGenericArguments();
            return true;
        }

        return false;
    }

    private static Type GetActionType(Type[] paramTypes) => paramTypes.Length switch
    {
        0 => typeof(Action),
        1 => typeof(Action<>).MakeGenericType(paramTypes),
        2 => typeof(Action<,>).MakeGenericType(paramTypes),
        3 => typeof(Action<,,>).MakeGenericType(paramTypes),
        4 => typeof(Action<,,,>).MakeGenericType(paramTypes),
        5 => typeof(Action<,,,,>).MakeGenericType(paramTypes),
        6 => typeof(Action<,,,,,>).MakeGenericType(paramTypes),
        7 => typeof(Action<,,,,,,>).MakeGenericType(paramTypes),
        8 => typeof(Action<,,,,,,,>).MakeGenericType(paramTypes),
        _ => throw new ArgumentOutOfRangeException(nameof(paramTypes)),
    };

    private static Type GetFuncType(Type[] paramTypes, Type returnType)
    {
        var args = paramTypes.Append(returnType).ToArray();
        return args.Length switch
        {
            1 => typeof(Func<>).MakeGenericType(args),
            2 => typeof(Func<,>).MakeGenericType(args),
            3 => typeof(Func<,,>).MakeGenericType(args),
            4 => typeof(Func<,,,>).MakeGenericType(args),
            5 => typeof(Func<,,,,>).MakeGenericType(args),
            6 => typeof(Func<,,,,,>).MakeGenericType(args),
            7 => typeof(Func<,,,,,,>).MakeGenericType(args),
            8 => typeof(Func<,,,,,,,>).MakeGenericType(args),
            9 => typeof(Func<,,,,,,,,>).MakeGenericType(args),
            _ => throw new ArgumentOutOfRangeException(nameof(paramTypes)),
        };
    }

    private static IEnumerable<MemberInfo> EnumerateFieldsAndProperties(Type type, BindingFlags flags)
    {
        foreach (var field in type.GetFields(flags))
            yield return field;
        foreach (var prop in type.GetProperties(flags))
            yield return prop;
    }

    private static Type GetMemberType(this MemberInfo member) => member switch
    {
        FieldInfo f => f.FieldType,
        PropertyInfo p => p.PropertyType,
        _ => throw new InvalidOperationException(),
    };

    private static void SetValue(this MemberInfo member, object? instance, object value)
    {
        switch (member)
        {
            case FieldInfo f:
                f.SetValue(instance, value);
                break;
            case PropertyInfo p:
                if (!p.CanWrite)
                {
                    throw new InvalidOperationException($"Property {p.DeclaringType?.Name}.{p.Name} must have a setter to receive an IPC binding.");
                }

                p.SetValue(instance, value);
                break;
            default:
                throw new InvalidOperationException();
        }
    }
}
