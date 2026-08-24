using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dalamud.Injector
{
    /// <summary>
    /// Owns the native resources needed to launch a process inside an AppContainer.
    /// Notably, the container SID and the prepared capability array for SECURITY_CAPABILITIES.
    /// </summary>
    public sealed class AppContainerLaunchContext : IDisposable
    {
        private readonly List<(IntPtr Ptr, bool IsFreeSid)> ownedSids = new();
        private IntPtr capabilitiesPtr = IntPtr.Zero;

        /// <summary>
        /// Gets a pointer to the AppContainer SID.
        /// </summary>
        public IntPtr ContainerSid { get; private set; } = IntPtr.Zero;

        /// <summary>
        /// Gets the string form of the AppContainer SID (S-1-15-2-...).
        /// </summary>
        public string ContainerSidString { get; private set; } = string.Empty;

        /// <summary>
        /// Gets a pointer to a native SID_AND_ATTRIBUTES array for SECURITY_CAPABILITIES.
        /// </summary>
        public IntPtr CapabilitiesPtr => this.capabilitiesPtr;

        /// <summary>
        /// Gets the number of capability entries.
        /// </summary>
        public int CapabilityCount { get; private set; }

        /// <summary>
        /// Gets or sets the directory the child process should use as TEMP/TMP, if any.
        /// </summary>
        public string? TempDirectoryOverride { get; set; }

        /// <summary>
        /// Gets or sets the runtime directory to pass to the child as DALAMUD_RUNTIME.
        /// Set in sandbox mode so Dalamud.Boot doesn't have to resolve it through the shell APIs.
        /// </summary>
        public string? RuntimeDirectoryOverride { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.capabilitiesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(this.capabilitiesPtr);
                this.capabilitiesPtr = IntPtr.Zero;
            }

            foreach (var (ptr, isFreeSid) in this.ownedSids)
            {
                if (isFreeSid)
                    AppContainerHelper.PInvoke.FreeSid(ptr);
                else
                    AppContainerHelper.PInvoke.LocalFree(ptr);
            }

            this.ownedSids.Clear();
            this.ContainerSid = IntPtr.Zero;
        }

        /// <summary>
        /// Set the container SID pointer owned by this context.
        /// </summary>
        /// <param name="sid">The SID, allocated by the profile APIs (freed with FreeSid).</param>
        internal void SetContainerSid(IntPtr sid)
        {
            this.ContainerSid = sid;
            this.ownedSids.Add((sid, true));
            this.ContainerSidString = AppContainerHelper.SidToString(sid);
        }

        /// <summary>
        /// Take ownership of capability SID.
        /// </summary>
        /// <param name="sid">The SID.</param>
        /// <param name="isFreeSid">Whether it must be freed with FreeSid (true) or LocalFree (false).</param>
        internal void AddOwnedSid(IntPtr sid, bool isFreeSid) => this.ownedSids.Add((sid, isFreeSid));

        /// <summary>
        /// Convert the given capability SIDs into a native SID_AND_ATTRIBUTES array.
        /// </summary>
        /// <param name="capabilitySids">The capability SIDs, already owned by this context.</param>
        internal void BuildCapabilityArray(IReadOnlyList<IntPtr> capabilitySids)
        {
            this.CapabilityCount = capabilitySids.Count;
            if (this.CapabilityCount == 0)
                return;

            var entrySize = Marshal.SizeOf<AppContainerHelper.PInvoke.SID_AND_ATTRIBUTES>();
            this.capabilitiesPtr = Marshal.AllocHGlobal(entrySize * this.CapabilityCount);
            for (var i = 0; i < this.CapabilityCount; i++)
            {
                var entry = new AppContainerHelper.PInvoke.SID_AND_ATTRIBUTES
                {
                    Sid = capabilitySids[i],
                    Attributes = AppContainerHelper.PInvoke.SE_GROUP_ENABLED,
                };
                Marshal.StructureToPtr(entry, this.capabilitiesPtr + (i * entrySize), false);
            }
        }
    }
}
