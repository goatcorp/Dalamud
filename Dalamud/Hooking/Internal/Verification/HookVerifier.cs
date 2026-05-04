using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
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
internal static partial class HookVerifier
{
    private static readonly ModuleLog Log = new("HookVerifier");

    /// <summary>
    /// Hook verification targets that don't exist in ClientStructs.
    /// </summary>
    private static readonly VerificationEntry[] ToVerify = [];

    private static readonly string ClientStructsInteropNamespacePrefix = string.Join(".", nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.Interop));

    private static FrozenDictionary<nint, VerificationEntry[]> allToVerify = FrozenDictionary<nint, VerificationEntry[]>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookVerifier"/> class.
    /// </summary>
    public static void Initialize()
    {
        var csAssembly = Assembly.GetAssembly(typeof(ZoneClient))!;
        var csTypes = csAssembly.GetTypes();

        var verifyContainer = new List<VerificationEntry>(1024);

        Parallel.ForEach(
            csTypes,
            csType =>
            {
                var methods = csType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
                if (methods.Length == 0)
                    return;

                var fullName = ClientStructsNamespaceTrim().Replace(csType.FullName!, string.Empty).Replace(".", "::");

                Type? addressesType = null;
                Type? delegateType = null;
                foreach (var method in methods)
                {
                    if (Attribute.IsDefined(method, typeof(ObsoleteAttribute))) continue;

                    if (!Attribute.IsDefined(method, typeof(MemberFunctionAttribute)) &&
                        !Attribute.IsDefined(method, typeof(StaticAddressAttribute)))
                        continue;

                    addressesType ??= csAssembly.GetType(csType.FullName + "+Addresses");

                    if (addressesType == null)
                    {
                        Log.Warning(
                            "Could not find Addresses type for {Type}, skipping verification for all members",
                            csType.FullName);
                        continue;
                    }

                    var address = addressesType.GetField(method.Name, BindingFlags.Static | BindingFlags.Public)
                                               ?.GetValue(null);
                    if (address is not Address addressValue)
                    {
                        Log.Warning(
                            "Could not find address for {Type}.{Member}, skipping verification",
                            csType.FullName,
                            method.Name);
                        continue;
                    }

                    var name = fullName + "." + method.Name;
                    if (method.GetCustomAttribute<MemberFunctionAttribute>() is { } memberFunctionAttribute)
                    {
                        if (!method.IsStatic)
                        {
                            delegateType ??= csAssembly.GetType(csType.FullName + "+Delegates");
                            if (delegateType == null)
                            {
                                Log.Warning(
                                    "Could not find delegate type for {Type}.{Member}, skipping verification",
                                    csType.FullName,
                                    method.Name);
                                continue;
                            }

                            var delegateMember = delegateType.GetMember(method.Name);
                            if (delegateMember.Length != 0)
                            {
                                verifyContainer.Add(
                                    new VerificationEntry(
                                        name,
                                        memberFunctionAttribute.Signature,
                                        addressValue.Value,
                                        (Type)delegateMember[0]));
                            }
                            else
                            {
                                verifyContainer.Add(
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
                            verifyContainer.Add(
                                new VerificationEntry(
                                    name,
                                    memberFunctionAttribute.Signature,
                                    addressValue.Value,
                                    Parameters: method.GetParameters(),
                                    ReturnType: method.ReturnType));
                        }
                    }
                    else if (method.GetCustomAttribute<StaticAddressAttribute>() is { } staticAddressAttribute)
                    {
                        verifyContainer.Add(
                            new VerificationEntry(
                                name,
                                staticAddressAttribute.Signature,
                                addressValue.Value,
                                Parameters: method.GetParameters(),
                                ReturnType: method.ReturnType));
                    }
                }
            });

        verifyContainer.AddRange(ToVerify);

        allToVerify = verifyContainer.GroupBy(v => v.Address).ToFrozenDictionary(v => v.Key, v => v.ToArray());
        Log.Verbose("Initialized HookVerifier with {Count} entries to verify", allToVerify.Sum(kv => kv.Value.Length));

        verifyContainer.Clear();
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

        // Is Developer Mode enabled?
        var configuration = Service<DalamudConfiguration>.GetNullable();
        if (configuration?.DevMode != true)
            return true;

        // Nothing to verify for this hook?
        if (!allToVerify.TryGetValue(address, out var entries))
            return true;

        var passedType = typeof(T);
        var isAssemblyMarshaled = !Attribute.IsDefined(passedType.Assembly, typeof(DisableRuntimeMarshallingAttribute));
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

    [GeneratedRegex($@"^{nameof(FFXIVClientStructs)}\.({nameof(FFXIVClientStructs.FFXIV)}|{nameof(FFXIVClientStructs.Havok)}|{nameof(FFXIVClientStructs.Interop)}|{nameof(FFXIVClientStructs.STD)})\.", RegexOptions.Singleline)]
    private static partial Regex ClientStructsNamespaceTrim();

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
