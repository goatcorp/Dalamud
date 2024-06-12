using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Dalamud.Logging.Internal;
using Dalamud.Memory;

using Iced.Intel;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// This class manages the final disposition of hooks, cleaning up any that have not reverted their changes.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class HookManager : IInternalDisposableService
{
    /// <summary>
    /// Logger shared with <see cref="CastingHook{T, TBase}"/>.
    /// Logger shared with <see cref="Unhooker"/>.
    /// </summary>
    internal static readonly ModuleLog Log = new("HM");

    [ServiceManager.ServiceConstructor]
    private HookManager()
    {
    }

    /// <summary>
    /// Gets sync root object for all hook operations.
    /// </summary>
    internal static object HookSyncRoot { get; } = new();

    /// <summary>
    /// Gets a static dictionary of tracked and registered hooks.
    /// The caller must hold the global hook lock when accessing this dictionary.
    /// </summary>
    internal static Dictionary<Guid, HookInfo> Hooks { get; } = [];

    /// <summary>
    /// Gets a static dictionary that maps hooked addresses to their corresponding unhooker.
    /// The caller must hold the global hook lock when accessing this dictionary.
    /// </summary>
    internal static Dictionary<nint, Unhooker> Unhookers { get; } = [];

    /// <summary>
    /// Gets a static dictionary that maps hooked addresses to their corresponding hook stacker.
    /// </summary>
    internal static ConcurrentDictionary<nint, IDalamudHook> HookStackTracker { get; } = [];

    /// <summary>
    /// Creates a new Unhooker instance for the provided address if no such unhooker was already registered, or returns
    /// an existing instance if the address was registered previously. By default, the unhooker will restore between 0
    /// and 0x32 bytes depending on the detected size of the hook. To specify the minimum and maximum bytes restored
    /// manually, use <see cref="RegisterUnhooker(nint, int, int)"/>.
    /// </summary>
    /// <param name="address">The address of the instruction.</param>
    /// <returns>A new Unhooker instance.</returns>
    public static Unhooker RegisterUnhooker(nint address)
    {
        return RegisterUnhooker(address, 0, 0x32);
    }

    /// <summary>
    /// Creates a new Unhooker instance for the provided address if no such unhooker was already registered, or returns
    /// an existing instance if the address was registered previously.
    /// </summary>
    /// <param name="address">The address of the instruction.</param>
    /// <param name="minBytes">The minimum amount of bytes to restore when unhooking.</param>
    /// <param name="maxBytes">The maximum amount of bytes to restore when unhooking.</param>
    /// <returns>A new Unhooker instance.</returns>
    public static Unhooker RegisterUnhooker(nint address, int minBytes, int maxBytes)
    {
        lock (HookSyncRoot)
        {
            Log.Verbose($"Registering hook at 0x{address.ToInt64():X} (minBytes=0x{minBytes:X}, maxBytes=0x{maxBytes:X})");
            if (!Unhookers.TryGetValue(address, out var unhooker))
            {
                unhooker = new Unhooker(address, minBytes, maxBytes);
                Unhookers[address] = unhooker;
            }

            return unhooker;
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        lock (HookSyncRoot)
        {
            RevertHooks();
            Hooks.Clear();
            Unhookers.Clear();
            HookStackTracker.Clear();
        }
    }

    /// <summary>
    /// Creates a hook. The hook constructor will only be called if the given address is not already hooked.
    /// Otherwise stacks the hook on top of the existing one. The backend name will always be determined
    /// by the first hook added at the specified address.
    /// This will create a hook inside the priority band specified.
    /// Dalamud internal hooks will always run first before normal priority, but after high-priority hooks.
    /// </summary>
    /// <typeparam name="T">Delegate of the hook.</typeparam>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="hookConstructor">Function that takes a delegate and creates a backend hook, if needed.</param>
    /// <param name="priority">Priority band of the hook.</param>
    /// <param name="precedence">Precendence of the hook. Positive values will lead to the hook having higher effective
    /// raw priority (called earlier), and negative values lead to lower effective raw priority (called later).</param>
    /// <param name="callingAssembly">Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    internal static Hook<T> CreateHook<T>(nint address, T detour, Func<T, Hook<T>> hookConstructor, HookPriority priority, int precedence, Assembly callingAssembly) where T : Delegate
    {
        var rawPriority = (byte)0;

        static byte CalculateRawPriority(int precedence, int precedenceRange, int lowestPriority)
        {
            var basePriority = lowestPriority + precedenceRange;

            if (precedence < -precedenceRange)
            {
                Log.Warning($"Clamping precedence to {-precedenceRange}; hook precendence should fall between [{-precedenceRange}, {precedenceRange}], was {precedence}");
                precedence = -precedenceRange;
            }

            if (precedence > precedenceRange)
            {
                Log.Warning($"Clamping precedence to {precedenceRange}; hook precendence should fall between [{-precedenceRange}, {precedenceRange}], was {precedence}");
                precedence = precedenceRange;
            }

            return (byte)(basePriority + precedence);
        }

        switch (priority)
        {
            case HookPriority.AfterNotify:
                rawPriority = 0;
                break;
            case HookPriority.NormalPriority:
                var isDalamudHook = callingAssembly == Assembly.GetExecutingAssembly();
                rawPriority = CalculateRawPriority(precedence, isDalamudHook ? 32 : 63, isDalamudHook ? 127 : 1);
                break;
            case HookPriority.HighPriority:
                rawPriority = CalculateRawPriority(precedence, 31, 192);
                break;
            case HookPriority.BeforeNotify:
                rawPriority = 255;
                break;
        }

        return CreateHook<T>(address, detour, hookConstructor, rawPriority, callingAssembly);
    }

    /// <summary>
    /// Follow a JMP or Jcc instruction to the next logical location.
    /// The caller must hold the global hook lock.
    /// </summary>
    /// <param name="address">Address of the instruction.</param>
    /// <returns>The address referenced by the jmp.</returns>
    internal static nint FollowJmp(nint address)
    {
        while (true)
        {
            var hasOtherHooks = HookManager.Unhookers.ContainsKey(address);
            if (hasOtherHooks)
            {
                // This address has been hooked already. Do not follow a jmp into a trampoline of our own making.
                Log.Verbose($"Detected hook trampoline at {address.ToInt64():X}, stopping jump resolution.");
                return address;
            }

            if (address.ToInt64() <= 0)
                throw new InvalidOperationException($"Address was <= 0, this can't be happening?! ({address:X})");

            var bytes = MemoryHelper.ReadRaw(address, 8);

            var codeReader = new ByteArrayCodeReader(bytes);
            var decoder = Decoder.Create(64, codeReader);
            decoder.IP = (ulong)address.ToInt64();
            decoder.Decode(out var inst);

            if (inst.Mnemonic == Mnemonic.Jmp)
            {
                var kind = inst.Op0Kind;

                IntPtr newAddress;
                switch (inst.Op0Kind)
                {
                    case OpKind.NearBranch64:
                    case OpKind.NearBranch32:
                    case OpKind.NearBranch16:
                        newAddress = (IntPtr)inst.NearBranchTarget;
                        break;
                    case OpKind.Immediate16:
                    case OpKind.Immediate8to16:
                    case OpKind.Immediate8to32:
                    case OpKind.Immediate8to64:
                    case OpKind.Immediate32to64:
                    case OpKind.Immediate32 when IntPtr.Size == 4:
                    case OpKind.Immediate64:
                        newAddress = (IntPtr)inst.GetImmediate(0);
                        break;
                    case OpKind.Memory when inst.IsIPRelativeMemoryOperand:
                        newAddress = (IntPtr)inst.IPRelativeMemoryAddress;
                        newAddress = Marshal.ReadIntPtr(newAddress);
                        break;
                    case OpKind.Memory:
                        newAddress = (IntPtr)inst.MemoryDisplacement64;
                        newAddress = Marshal.ReadIntPtr(newAddress);
                        break;
                    default:
                        var debugBytes = string.Join(" ", bytes.Take(inst.Length).Select(b => $"{b:X2}"));
                        throw new Exception($"Unknown OpKind {inst.Op0Kind} from {debugBytes}");
                }

                Log.Verbose($"Resolving assembly jump ({kind}) from {address.ToInt64():X} to {newAddress.ToInt64():X}");
                address = newAddress;
            }
            else
            {
                break;
            }
        }

        return address;
    }

    /// <summary>
    /// Creates a hook. The hook constructor will only be called if the given address is not already hooked.
    /// Otherwise stacks the hook on top of the existing one. The backend name will always be determined
    /// by the first hook added at the specified address.
    /// </summary>
    /// <param name="address">A memory address to install a hook.</param>
    /// <param name="detour">Callback function. Delegate must have a same original function prototype.</param>
    /// <param name="hookConstructor">Function that takes a delegate and creates a backend hook, if needed.</param>
    /// <param name="rawPriority">Raw priority of the hook.</param>
    /// <param name ="callingAssembly"> Calling assembly.</param>
    /// <returns>The hook with the supplied parameters.</returns>
    private static Hook<T> CreateHook<T>(nint address, T detour, Func<T, Hook<T>> hookConstructor, byte rawPriority, Assembly callingAssembly) where T : Delegate
    {
        lock (HookSyncRoot)
        {
            if (!HookStackTracker.TryGetValue(address, out var hookStacker))
            {
                hookStacker = new HookStacker<T>(address);
                var backendDetour = (hookStacker as HookStacker<T>).BackendDelegate;
                (hookStacker as HookStacker<T>).SetBackend(hookConstructor(backendDetour));
            }

            var hookStackerType = hookStacker.GetType().GetGenericArguments()[0];

            if (hookStackerType == typeof(T))
            {
                var hook = new HollowHook<T>(address, hookStacker as HookStacker<T>);
                var hookInfo = new HookInfo(hook, detour, callingAssembly, rawPriority);
                (hookStacker as HookStacker<T>).Add(hookInfo);
                Hooks.TryAdd(Guid.NewGuid(), hookInfo);
                return hook;
            }
            else
            {
                Log.Debug($"Stacking hook of type {typeof(T).Name} onto {hookStackerType} at address {address}");
                var baseHookType = typeof(HollowHook<>).MakeGenericType(hookStackerType);
                var baseHook = Activator.CreateInstance(baseHookType, address, hookStacker);
                var hookType = typeof(CastingHook<,>).MakeGenericType(typeof(T), hookStackerType);
                var hook = Activator.CreateInstance(hookType, address, baseHook, detour);
                var baseDetour = hookType.GetMethod("GetBaseHookDetour").Invoke(hook, []);
                var baseHookInfo = new HookInfo((IDalamudHook)baseHook, (Delegate)baseDetour, callingAssembly, rawPriority);
                hookStacker.GetType().GetMethod("Add").Invoke(hookStacker, [baseHookInfo]);
                var hookInfo = new HookInfo((IDalamudHook)hook, detour, callingAssembly, rawPriority);
                Hooks.TryAdd(Guid.NewGuid(), hookInfo);
                return hook as Hook<T>;
            }
        }
    }

    /// <summary>
    /// Reverts all Hooks.
    /// The caller must hold the global hook lock.
    /// </summary>
    private static void RevertHooks()
    {
        foreach (var unhooker in Unhookers.Values)
        {
            unhooker.Unhook();
        }
    }
}
