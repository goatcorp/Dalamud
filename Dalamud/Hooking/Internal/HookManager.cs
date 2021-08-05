using System;
using System.Collections.Generic;

using Dalamud.Logging.Internal;
using Dalamud.Memory;
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
        /// <param name="dalamud">Dalamud instance.</param>
        internal HookManager(Dalamud dalamud)
        {
            _ = dalamud;
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

                if (i > 0)
                {
                    Log.Verbose($"Reverting hook at 0x{address.ToInt64():X}");
                    fixed (byte* original = originalBytes)
                    {
                        MemoryHelper.ChangePermission(address, i, MemoryProtection.ExecuteReadWrite, out var oldPermissions);
                        MemoryHelper.WriteRaw(address, originalBytes);
                        MemoryHelper.ChangePermission(address, i, oldPermissions);
                    }
                }
            }
        }
    }
}
