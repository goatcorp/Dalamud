using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Application.Network;

using InteropGenerator.Runtime;
using InteropGenerator.Runtime.Attributes;

namespace Dalamud.Hooking.Internal.Verification;

/// <summary>
/// Global utility that can verify whether hook delegates are correctly declared.
/// Initialized out-of-band, since Hook is instantiated all over the place without a service, so this cannot be
/// a service either.
/// </summary>
internal static class HookVerifier
{
    private static readonly ModuleLog Log = new("HookVerifier");

    private static readonly string PrefixFfxiv = $"{nameof(FFXIVClientStructs)}.{nameof(FFXIVClientStructs.FFXIV)}.";
    private static readonly string PrefixHavok = $"{nameof(FFXIVClientStructs)}.{nameof(FFXIVClientStructs.Havok)}.";
    private static readonly string PrefixInterop = $"{nameof(FFXIVClientStructs)}.{nameof(FFXIVClientStructs.Interop)}.";
    private static readonly string PrefixStd = $"{nameof(FFXIVClientStructs)}.{nameof(FFXIVClientStructs.STD)}.";

    /// <summary>
    /// Hook verification targets that don't exist in ClientStructs.
    /// </summary>
    private static readonly VerificationEntry[] ExternalVerificationTargets = [];

    private static readonly string ClientStructsInteropNamespacePrefix = string.Join(".", nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.Interop));

    private static FrozenDictionary<nint, VerificationEntry[]> allToVerify = FrozenDictionary<nint, VerificationEntry[]>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookVerifier"/> class.
    /// </summary>
    public static void Initialize()
    {
        var csAssembly = Assembly.GetAssembly(typeof(ZoneClient))!;
        var csTypes = csAssembly.GetTypes();

        var sw = Stopwatch.StartNew();

        var totalEntriesLists = csTypes
            .AsParallel()
            .Where(csType => csType.IsValueType && !csType.IsEnum && Attribute.IsDefined(csType, typeof(GenerateInteropAttribute)))
            .Select(csType =>
            {
                var methods = csType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (methods.Length == 0)
                    return [];

                var addressesType = csType.GetNestedType("Addresses", BindingFlags.Public | BindingFlags.NonPublic);
                if (addressesType == null)
                {
#if DEBUG
                    Log.Warning(
                        "Could not find Addresses type for {Type}, skipping verification for all members",
                        csType.FullName);
#endif
                    return [];
                }

                var addressesFields = addressesType.GetFields(BindingFlags.Static | BindingFlags.Public);
                var addressLookup = new Dictionary<string, Address>(addressesFields.Length, StringComparer.Ordinal);
                foreach (var fieldInfo in addressesFields)
                {
                    if (fieldInfo.GetValue(null) is Address addr)
                        addressLookup[fieldInfo.Name] = addr;
                }

                var delegateType = csType.GetNestedType("Delegates", BindingFlags.Public | BindingFlags.NonPublic);
                Dictionary<string, Type>? delegateLookup = null;
                if (delegateType != null)
                {
                    var nestedTypes = delegateType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                    delegateLookup = new Dictionary<string, Type>(nestedTypes.Length, StringComparer.Ordinal);
                    foreach (var type in nestedTypes)
                        delegateLookup[type.Name] = type;
                }

                string? fullName = null;
                var list = new List<VerificationEntry>(methods.Length);

                foreach (var method in methods)
                {
                    if (method.IsDefined(typeof(ObsoleteAttribute), false))
                        continue;

                    var memberFunctionAttribute = method.GetCustomAttribute<MemberFunctionAttribute>(false);
                    var staticAddressAttribute = memberFunctionAttribute == null
                        ? method.GetCustomAttribute<StaticAddressAttribute>(false)
                        : null;

                    if (memberFunctionAttribute == null && staticAddressAttribute == null)
                        continue;

                    if (!addressLookup.TryGetValue(method.Name, out var addressValue))
                    {
#if DEBUG
                        Log.Warning(
                            "Could not find address for {Type}.{Member}, skipping verification",
                            csType.FullName,
                            method.Name);
#endif
                        continue;
                    }

                    fullName ??= GetTrimmedFullName(csType.FullName!);
                    var name = fullName + "." + method.Name;

                    if (memberFunctionAttribute != null)
                    {
                        if (!method.IsStatic)
                        {
                            if (delegateType == null)
                            {
#if DEBUG
                                Log.Warning(
                                    "Could not find delegate type for {Type}.{Member}, skipping verification",
                                    csType.FullName,
                                    method.Name);
#endif
                                continue;
                            }

                            if (delegateLookup?.TryGetValue(method.Name, out var delegateMemberType) == true)
                            {
                                list.Add(
                                    new VerificationEntry(
                                        name,
                                        memberFunctionAttribute.Signature,
                                        addressValue.Value,
                                        delegateMemberType));
                            }
                            else
                            {
                                list.Add(
                                    new VerificationEntry(
                                        name,
                                        memberFunctionAttribute.Signature,
                                        addressValue.Value,
                                        Parameters: method.GetParameters(),
                                        ReturnType: method.ReturnType));
                            }
                        }
                        else
                        {
                            list.Add(
                                new VerificationEntry(
                                    name,
                                    memberFunctionAttribute.Signature,
                                    addressValue.Value,
                                    Parameters: method.GetParameters(),
                                    ReturnType: method.ReturnType));
                        }
                    }
                    else if (staticAddressAttribute != null)
                    {
                        list.Add(
                            new VerificationEntry(
                                name,
                                staticAddressAttribute.Signature,
                                addressValue.Value,
                                Parameters: method.GetParameters(),
                                ReturnType: method.ReturnType));
                    }
                }

                return list;
            })
            .ToArray();

        var grouping = new Dictionary<nint, List<VerificationEntry>>();

        foreach (var list in totalEntriesLists)
        {
            if (list.Count == 0)
                continue;

            foreach (var entry in list)
            {
                ref var group = ref CollectionsMarshal.GetValueRefOrAddDefault(grouping, entry.Address, out var exists);

                if (!exists)
                    group = [];

                group.Add(entry);
            }
        }

        foreach (var entry in ExternalVerificationTargets)
        {
            ref var group = ref CollectionsMarshal.GetValueRefOrAddDefault(grouping, entry.Address, out var exists);
            if (!exists)
                group = [];

            group.Add(entry);
        }

        allToVerify = grouping.ToFrozenDictionary(kv => kv.Key, kv => kv.Value.ToArray());

        sw.Stop();
        Log.Verbose("Initialized HookVerifier with {Count} entries to verify in {ms}ms", allToVerify.Sum(kv => kv.Value.Length), sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Verify the hook with the provided address and exception.
    /// </summary>
    /// <param name="address">The address of the function we are hooking.</param>
    /// <param name="hookCaller">The caller that is trying to create the hook.</param>
    /// <param name="exceptions">The exceptions when we think one of the hooks for this address is not correctly declared.</param>
    /// <typeparam name="T">The delegate type passed by the creator of the hook.</typeparam>
    /// <returns> <see langword="true"/> when we think the hook is not correctly declared, otherwise <see langword="false"/>. </returns>
    public static bool TryVerify<T>(IntPtr address, Assembly hookCaller, out HookVerificationException[] exceptions) where T : Delegate
    {
        exceptions = [];

        // Nothing to verify for this hook?
        if (!allToVerify.TryGetValue(address, out var entries))
            return true;

        var passedType = typeof(T);
        var isAssemblyMarshaled = !passedType.Assembly.IsDefined(typeof(DisableRuntimeMarshallingAttribute), false);
        string? failContext = null;

        var passedInvoke = passedType.GetMethod("Invoke")!;
        var passedParams = passedInvoke.GetParameters();

        var ret = true;
        foreach (var entry in entries)
        {
            // Check if entry is a delegate or method check
            ParameterInfo[] enforcedParams;
            bool mismatch;
            if (entry.TargetDelegateType != null)
            {
                // Directly compare delegates
                if (passedType == entry.TargetDelegateType)
                    continue;

                var enforcedInvoke = entry.TargetDelegateType.GetMethod("Invoke")!;

                // Compare Return Type
                mismatch = !CheckParam(passedInvoke.ReturnType, enforcedInvoke.ReturnType, isAssemblyMarshaled);

                // Compare Parameter Count
                enforcedParams = enforcedInvoke.GetParameters();
            }
            else
            {
                // Compare Return Type
                mismatch = !CheckParam(passedInvoke.ReturnType, entry.ReturnType!, isAssemblyMarshaled);

                // Compare Parameter Count
                enforcedParams = entry.Parameters!;
            }

            if (passedParams.Length != enforcedParams.Length)
            {
                mismatch = true;
                failContext = "Param count check.";
            }
            else if (!mismatch)
            {
                // Compare Parameter Types
                for (var i = 0; i < passedParams.Length; i++)
                {
                    if (!CheckParam(passedParams[i].ParameterType, enforcedParams[i].ParameterType, isAssemblyMarshaled))
                    {
                        mismatch = true;
                        failContext = "Param type check.";
                        break;
                    }
                }
            }
            else
            {
                failContext = "Return type check.";
            }

            if (mismatch)
            {
                var enforcedDelegate = entry.TargetDelegateType != null ?
                    HookVerificationException.GetSignature(entry.TargetDelegateType) :
                    $"{entry.ReturnType!.Name} ({string.Join(", ", entry.Parameters!.Select(p => p.ParameterType.Name))})";

                exceptions = [HookVerificationException.Create(address, passedType, enforcedDelegate, entry.Message, entry.Name, entry.Signature, failContext, hookCaller), .. exceptions];
                ret = false;
            }
        }

        return ret;
    }

    private static string GetTrimmedFullName(string originalName)
    {
        var startIdx = 0;

        if (originalName.StartsWith(PrefixFfxiv))
            startIdx = PrefixFfxiv.Length;
        else if (originalName.StartsWith(PrefixHavok))
            startIdx = PrefixHavok.Length;
        else if (originalName.StartsWith(PrefixInterop))
            startIdx = PrefixInterop.Length;
        else if (originalName.StartsWith(PrefixStd))
            startIdx = PrefixStd.Length;

        var dotCount = originalName.AsSpan()[startIdx..].Count('.');
        var sliceLength = originalName.Length - startIdx;
        var finalLength = sliceLength + dotCount; // every '.' adds 1 extra character for '::'

        return string.Create(finalLength, (originalName, startIdx, sliceLength), static (dest, state) =>
        {
            var src = state.originalName.AsSpan(state.startIdx, state.sliceLength);
            var destIdx = 0;

            for (var i = 0; i < src.Length; i++)
            {
                var c = src[i];
                if (c == '.')
                {
                    dest[destIdx++] = ':';
                    dest[destIdx++] = ':';
                }
                else
                {
                    dest[destIdx++] = c;
                }
            }
        });
    }

    private static bool CheckParam(Type paramLeft, Type paramRight, bool isMarshaled)
    {
        var sameType = paramLeft == paramRight;
        return sameType || SizeOf(paramLeft, isMarshaled) == SizeOf(paramRight, false);
    }

    private static int SizeOf(Type type, bool isMarshaled)
    {
        return type switch
        {
            _ when type == typeof(sbyte) || type == typeof(byte) || (type == typeof(bool) && !isMarshaled) => 1,
            _ when type == typeof(char) || type == typeof(short) || type == typeof(ushort) || type == typeof(Half) => 2,
            _ when type == typeof(int) || type == typeof(uint) || type == typeof(float) || (type == typeof(bool) && isMarshaled) => 4,
            _ when type == typeof(long) || type == typeof(ulong) || type == typeof(double) || type.IsPointer || type.IsFunctionPointer || type.IsUnmanagedFunctionPointer || (type.Name == "Pointer`1" && type.Namespace.AsSpan().SequenceEqual(ClientStructsInteropNamespacePrefix)) || type == typeof(CStringPointer) => 8,
            _ when type.Name.StartsWith("FixedSizeArray") => SizeOf(type.GetGenericArguments()[0], isMarshaled) * int.Parse(type.Name[14..type.Name.IndexOf('`')]),
            _ when type.GetCustomAttribute<InlineArrayAttribute>() is { Length: var length } => SizeOf(type.GetGenericArguments()[0], isMarshaled) * length,
            _ when IsStruct(type) && !type.IsGenericType && (type.StructLayoutAttribute?.Value ?? LayoutKind.Sequential) != LayoutKind.Sequential => type.StructLayoutAttribute?.Size ?? (int?)typeof(Unsafe).GetMethod("SizeOf")?.MakeGenericMethod(type).Invoke(null, null) ?? 0,
            _ when type.IsEnum => SizeOf(Enum.GetUnderlyingType(type), isMarshaled),
            _ when type.IsGenericType => Marshal.SizeOf(Activator.CreateInstance(type)!),
            _ => GetSizeOf(type),
        };
    }

    private static int GetSizeOf(Type type)
    {
        try
        {
            return Marshal.SizeOf(Activator.CreateInstance(type)!);
        }
        catch
        {
            return 0;
        }
    }

    private static bool IsStruct(Type type)
        => type != typeof(decimal) && type is { IsValueType: true, IsPrimitive: false, IsEnum: false };

    private record VerificationEntry(
        string Name,
        string Signature,
        nint Address,
        Type? TargetDelegateType = null,
        ParameterInfo[]? Parameters = null,
        Type? ReturnType = null,
        string Message = "Failed match against expected documentation.");
}
