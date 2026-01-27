using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Logging.Internal;

using InteropGenerator.Runtime;

namespace Dalamud.Hooking.Internal.Verification;

/// <summary>
/// Global utility that can verify whether hook delegates are correctly declared.
/// Initialized out-of-band, since Hook is instantiated all over the place without a service, so this cannot be
/// a service either.
/// </summary>
internal static class HookVerifier
{
    private static readonly ModuleLog Log = new("HookVerifier");

    private static readonly VerificationEntry[] ToVerify =
    [
        new(
            "ActorControlSelf",
            "E8 ?? ?? ?? ?? 0F B7 0B 83 E9 64",
            typeof(ActorControlSelfDelegate), // TODO: change this to CS delegate
            "Signature changed in Patch 7.4") // 7.4 (new parameters)
    ];

    private static readonly string ClientStructsInteropNamespacePrefix = string.Join(".", nameof(FFXIVClientStructs), nameof(FFXIVClientStructs.Interop));

    private delegate void ActorControlSelfDelegate(uint category, uint eventId, uint param1, uint param2, uint param3, uint param4, uint param5, uint param6, uint param7, uint param8, ulong targetId, byte param9); // TODO: change this to CS delegate

    /// <summary>
    /// Initializes a new instance of the <see cref="HookVerifier"/> class.
    /// </summary>
    /// <param name="scanner">Process to scan in.</param>
    public static void Initialize(TargetSigScanner scanner)
    {
        foreach (var entry in ToVerify)
        {
            if (!scanner.TryScanText(entry.Signature, out var address))
            {
                Log.Error("Could not resolve signature for hook {Name} ({Sig})", entry.Name, entry.Signature);
                continue;
            }

            entry.Address = address;
        }
    }

    /// <summary>
    /// Verify the hook with the provided address and exception.
    /// </summary>
    /// <param name="address">The address of the function we are hooking.</param>
    /// <typeparam name="T">The delegate type passed by the creator of the hook.</typeparam>
    /// <exception cref="HookVerificationException">Exception thrown when we think the hook is not correctly declared.</exception>
    public static void Verify<T>(IntPtr address) where T : Delegate
    {
        var entry = ToVerify.FirstOrDefault(x => x.Address == address);

        // Nothing to verify for this hook?
        if (entry == null)
        {
            return;
        }

        var passedType = typeof(T);

        // Directly compare delegates
        if (passedType == entry.TargetDelegateType)
        {
            return;
        }

        var passedInvoke = passedType.GetMethod("Invoke")!;
        var enforcedInvoke = entry.TargetDelegateType.GetMethod("Invoke")!;

        // Compare Return Type
        var mismatch = !CheckParam(passedInvoke.ReturnType, enforcedInvoke.ReturnType);

        // Compare Parameter Count
        var passedParams = passedInvoke.GetParameters();
        var enforcedParams = enforcedInvoke.GetParameters();

        if (passedParams.Length != enforcedParams.Length)
        {
            mismatch = true;
        }
        else
        {
            // Compare Parameter Types
            for (var i = 0; i < passedParams.Length; i++)
            {
                if (!CheckParam(passedParams[i].ParameterType, enforcedParams[i].ParameterType))
                {
                    mismatch = true;
                    break;
                }
            }
        }

        if (mismatch)
        {
            throw HookVerificationException.Create(address, passedType, entry.TargetDelegateType, entry.Message);
        }
    }

    private static bool CheckParam(Type paramLeft, Type paramRight)
    {
        var sameType = paramLeft == paramRight;
        return sameType || SizeOf(paramLeft) == SizeOf(paramRight);
    }

    private static int SizeOf(Type type) 
    {
        return type switch {
            _ when type == typeof(sbyte) || type == typeof(byte) || type == typeof(bool) => 1,
            _ when type == typeof(char) || type == typeof(short) || type == typeof(ushort) || type == typeof(Half) => 2,
            _ when type == typeof(int) || type == typeof(uint) || type == typeof(float) => 4,
            _ when type == typeof(long) || type == typeof(ulong) || type == typeof(double) || type.IsPointer || type.IsFunctionPointer || type.IsUnmanagedFunctionPointer || (type.Name == "Pointer`1" && type.Namespace.AsSpan().SequenceEqual(ClientStructsInteropNamespacePrefix)) || type == typeof(CStringPointer) => 8,
            _ when type.Name.StartsWith("FixedSizeArray") => SizeOf(type.GetGenericArguments()[0]) * int.Parse(type.Name[14..type.Name.IndexOf('`')]),
            _ when type.GetCustomAttribute<InlineArrayAttribute>() is { Length: var length } => SizeOf(type.GetGenericArguments()[0]) * length,
            _ when IsStruct(type) && !type.IsGenericType && (type.StructLayoutAttribute?.Value ?? LayoutKind.Sequential) != LayoutKind.Sequential => type.StructLayoutAttribute?.Size ?? (int?)typeof(Unsafe).GetMethod("SizeOf")?.MakeGenericMethod(type).Invoke(null, null) ?? 0,
            _ when type.IsEnum => SizeOf(Enum.GetUnderlyingType(type)),
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
    {
        return type != typeof(decimal) && type is { IsValueType: true, IsPrimitive: false, IsEnum: false };
    }

    private record VerificationEntry(string Name, string Signature, Type TargetDelegateType, string Message)
    {
        public nint Address { get; set; }
    }
}
