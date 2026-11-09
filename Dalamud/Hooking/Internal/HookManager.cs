using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Game;
using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Dalamud.Utility;

using Iced.Intel;

namespace Dalamud.Hooking.Internal;

/// <summary>
/// This class manages the final disposition of hooks, cleaning up any that have not reverted their changes.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class HookManager : IInternalDisposableService
{
    /// <summary>
    /// Logger shared with <see cref="Unhooker"/>.
    /// </summary>
    internal static readonly ModuleLog Log = ModuleLog.Create<HookManager>();

    [ServiceManager.ServiceDependency]
    private readonly Framework framework = Service<Framework>.Get();

    [ServiceManager.ServiceConstructor]
    private HookManager()
    {
    }

    /// <summary>
    /// Gets the lock for hook registration, enabling, disabling, and disposal.
    /// </summary>
    internal static Lock HookEnableSyncRoot { get; } = new();

    /// <summary>
    /// Gets a static list of tracked and registered hooks.
    /// </summary>
    internal static ConcurrentDictionary<Guid, HookInfo> TrackedHooks { get; } = new();

    /// <summary>
    /// Gets a static dictionary of unhookers for a hooked address.
    /// </summary>
    internal static ConcurrentDictionary<IntPtr, Unhooker> Unhookers { get; } = new();

    /// <summary>
    /// Gets a static dictionary of the number of hooks on a given address.
    /// </summary>
    internal static ConcurrentDictionary<IntPtr, List<IDalamudHook?>> MultiHookTracker { get; } = new();

    /// <summary>
    /// Creates a new Unhooker instance for the provided address if no such unhooker was already registered, or returns
    /// an existing instance if the address was registered previously. By default, the unhooker will restore between 0
    /// and 0x32 bytes depending on the detected size of the hook. To specify the minimum and maximum bytes restored
    /// manually, use <see cref="RegisterUnhooker(System.IntPtr, int, int)"/>.
    /// </summary>
    /// <param name="address">The address of the instruction.</param>
    /// <returns>A new Unhooker instance.</returns>
    public static Unhooker RegisterUnhooker(IntPtr address)
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
    public static Unhooker RegisterUnhooker(IntPtr address, int minBytes, int maxBytes)
    {
        // Avoid bootstrapper module enumeration unless verbose diagnostics need it.
        if (Serilog.Log.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
            Log.Verbose($"Registering hook at {Util.DescribeAddress(address)} (minBytes=0x{minBytes:X}, maxBytes=0x{maxBytes:X})");
        return Unhookers.GetOrAdd(address, _ => new Unhooker(address, minBytes, maxBytes));
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        // Reduce chances of trampling code that is currently executing by reverting on main thread
        this.framework.Run(() =>
        {
            RevertHooks();
            TrackedHooks.Clear();
            Unhookers.Clear();
        }).Wait();
    }

    /// <summary>
    /// Rejects duplicate callback invocation lists at the same address to prevent recursive hook chains.
    /// </summary>
    /// <param name="address">The resolved address to hook.</param>
    /// <param name="detour">The proposed callback.</param>
    /// <remarks>
    /// Hold <see cref="HookEnableSyncRoot"/> from this check through native hook creation and registration.
    /// Disabled hooks remain registered until disposal because they can be enabled again.
    /// </remarks>
    internal static void ThrowIfDuplicateDetour(IntPtr address, Delegate detour)
    {
        var callbacks = detour.GetInvocationList();
        foreach (var existing in TrackedHooks.Values)
        {
            if (existing.Hook.IsDisposed || existing.Address != address)
                continue;

            var existingCallbacks = existing.Delegate.GetInvocationList();
            if (existingCallbacks.Length != callbacks.Length)
                continue;

            var matches = true;
            for (var i = 0; matches && i < callbacks.Length; i++)
            {
                var callback = callbacks[i];
                var existingCallback = existingCallbacks[i];
                if (callback.Target is null && callback.Method.DeclaringType is { } declaringType &&
                    typeof(Delegate).IsAssignableFrom(declaringType))
                {
                    // Native delegates can share Method and Target; equality also checks the function pointer.
                    matches = callback.Equals(existingCallback);
                }
                else
                {
                    // Managed callback identity is independent of delegate type.
                    matches = callback.Method == existingCallback.Method &&
                              ReferenceEquals(callback.Target, existingCallback.Target);
                }
            }

            if (matches)
            {
                throw new InvalidOperationException(
                    $"The detour {detour.Method.DeclaringType?.FullName}.{detour.Method.Name} is already registered " +
                    $"at 0x{address:X} by {existing.Assembly.GetName().Name}. " +
                    "Dispose the previous hook before creating a replacement.");
            }
        }
    }

    /// <summary>
    /// Follow a JMP or Jcc instruction to the next logical location.
    /// </summary>
    /// <param name="address">Address of the instruction.</param>
    /// <returns>The address referenced by the jmp.</returns>
    internal static IntPtr FollowJmp(IntPtr address)
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
                    case OpKind.Memory when inst.IsJmpNearIndirect:
                    case OpKind.Memory when inst.IsJmpFarIndirect:
                        Log.Verbose($"Encountered indirect assembly jump ({kind}) from {address.ToInt64():X}, returning unresolved");
                        return address;
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

    private static void RevertHooks()
    {
        foreach (var unhooker in Unhookers.Values)
        {
            unhooker.Unhook();
        }
    }
}
