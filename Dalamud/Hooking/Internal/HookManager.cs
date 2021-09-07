using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Logging.Internal;
using Dalamud.Memory;
using Iced.Intel;
using Microsoft.Win32;

namespace Dalamud.Hooking.Internal
{
    /// <summary>
    /// This class manages the final disposition of hooks, cleaning up any that have not reverted their changes.
    /// </summary>
    internal class HookManager : IDisposable
    {
        private static readonly ModuleLog Log = new("HM");
        private static bool checkLinuxOnce = true;
        private static bool isRunningLinux = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HookManager"/> class.
        /// </summary>
        public HookManager()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the client is running under Linux Wine.
        /// </summary>
        /// <returns>A value indicating whether the game is running under Wine.</returns>
        internal static bool DirtyLinuxUser
        {
            get
            {
                if (checkLinuxOnce)
                {
                    checkLinuxOnce = false;

                    bool Check1()
                    {
                        return Environment.GetEnvironmentVariable("XL_WINEONLINUX") != null;
                    }

                    bool Check2()
                    {
                        var hModule = NativeFunctions.GetModuleHandleW("ntdll.dll");
                        var proc1 = NativeFunctions.GetProcAddress(hModule, "wine_get_version");
                        var proc2 = NativeFunctions.GetProcAddress(hModule, "wine_get_build_id");

                        return proc1 != IntPtr.Zero || proc2 != IntPtr.Zero;
                    }

                    bool Check3()
                    {
                        return Registry.CurrentUser.OpenSubKey(@"Software\Wine") != null ||
                               Registry.LocalMachine.OpenSubKey(@"Software\Wine") != null;
                    }

                    if (isRunningLinux = Check1() || Check2() || Check3())
                    {
                        Log.Information($"Dalamud detected running on Wine");
                    }
                }

                return isRunningLinux;
            }
        }

        /// <summary>
        /// Gets a static list of tracked and registered hooks.
        /// </summary>
        internal static List<HookInfo> TrackedHooks { get; } = new();

        /// <summary>
        /// Gets a static dictionary of original code for a hooked address.
        /// </summary>
        internal static Dictionary<IntPtr, byte[]> Originals { get; } = new();

        /// <inheritdoc/>
        public void Dispose()
        {
            RevertHooks();
            TrackedHooks.Clear();
            Originals.Clear();
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
                var hasOtherHooks = HookManager.Originals.ContainsKey(address);
                if (hasOtherHooks)
                {
                    // This address has been hooked already. Do not follow a jmp into a trampoline of our own making.
                    Log.Verbose($"Detected hook trampoline at {address.ToInt64():X}, stopping jump resolution.");
                    return address;
                }

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

        private static unsafe void RevertHooks()
        {
            foreach (var (address, originalBytes) in Originals)
            {
                var i = 0;
                var current = (byte*)address;
                // Find how many bytes have been modified by comparing to the saved original
                for (; i < originalBytes.Length; i++)
                {
                    if (current[i] == originalBytes[i])
                        break;
                }

                var snippet = originalBytes[0..i];

                if (i > 0)
                {
                    Log.Verbose($"Reverting hook at 0x{address.ToInt64():X} ({snippet.Length} bytes)");
                    MemoryHelper.ChangePermission(address, i, MemoryProtection.ExecuteReadWrite, out var oldPermissions);
                    MemoryHelper.WriteRaw(address, snippet);
                    MemoryHelper.ChangePermission(address, i, oldPermissions);
                }
            }
        }
    }
}
