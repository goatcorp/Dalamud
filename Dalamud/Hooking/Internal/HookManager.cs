using System;
using System.Collections.Generic;

using Dalamud.Memory;

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
                    var value = Environment.GetEnvironmentVariable("XL_WINEONLINUX");
                    isRunningLinux = value is not null;
                }

                return isRunningLinux;
            }
        }

        /// <summary>
        /// Gets a static list of tracked and registered hooks.
        /// </summary>
        internal static List<HookInfo> TrackedHooks { get; } = new();

        /// <summary>
        /// Gets a static list of original code for a hooked address.
        /// </summary>
        internal static List<(IntPtr Address, byte[] Original)> Originals { get; } = new();

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
                    Log.Debug($"Reverting hook at 0x{address.ToInt64():X}");
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
