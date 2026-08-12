using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Serilog;

// ReSharper disable InconsistentNaming

namespace Dalamud.Injector
{
    /// <summary>
    /// Helpers for creating AppContainer profiles, capability SIDs, filesystem grants and loopback exemption.
    /// </summary>
    internal static class AppContainerHelper
    {
        // We use specific rights rather than GENERIC_* so that the ACE we write is identical to what we later
        // compare against in HasAccess, because the kernel maps specific rights to generic ones

        /// <summary>
        /// Access mask for read and execute grants.
        /// </summary>
        public const uint AccessReadExecute = PInvoke.FILE_GENERIC_READ | PInvoke.FILE_GENERIC_EXECUTE;

        /// <summary>
        /// Access mask for modify grants (read/write/execute/delete).
        /// </summary>
        public const uint AccessModify = PInvoke.FILE_GENERIC_READ | PInvoke.FILE_GENERIC_WRITE | PInvoke.FILE_GENERIC_EXECUTE | PInvoke.DELETE;

        // HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS)
        private const int HResultAlreadyExists = unchecked((int)0x800700B7);

        private static readonly Dictionary<string, uint> WellKnownCapabilityRids = new(StringComparer.OrdinalIgnoreCase)
        {
            { "internetClient", PInvoke.SECURITY_CAPABILITY_INTERNET_CLIENT },
            { "internetClientServer", PInvoke.SECURITY_CAPABILITY_INTERNET_CLIENT_SERVER },
            { "privateNetworkClientServer", PInvoke.SECURITY_CAPABILITY_PRIVATE_NETWORK_CLIENT_SERVER },
        };

        /// <summary>
        /// Create or open the AppContainer profile and prepare caps.
        /// </summary>
        /// <param name="containerName">The internal name of the container.</param>
        /// <param name="displayName">The display name of the container.</param>
        /// <param name="description">The description of the container.</param>
        /// <param name="capabilityNames">
        /// Names of capabilities the container should have access to.
        /// Well-known ones are mapped to fixed RIDs, others are derived by name instead.
        /// </param>
        /// <returns>A launch context that can be used to start processes in a containerized manner.</returns>
        public static AppContainerLaunchContext CreateContext(string containerName, string displayName, string description, IReadOnlyList<string> capabilityNames)
        {
            var ctx = new AppContainerLaunchContext();
            try
            {
                var capSids = new List<IntPtr>();
                foreach (var name in capabilityNames)
                {
                    if (WellKnownCapabilityRids.TryGetValue(name, out var rid))
                    {
                        var authority = new PInvoke.SID_IDENTIFIER_AUTHORITY
                        {
                            Value = new byte[] { 0, 0, 0, 0, 0, 15 }, // SECURITY_APP_PACKAGE_AUTHORITY
                        };

                        if (!PInvoke.AllocateAndInitializeSid(
                                ref authority,
                                2,
                                PInvoke.SECURITY_CAPABILITY_BASE_RID,
                                rid,
                                0,
                                0,
                                0,
                                0,
                                0,
                                0,
                                out var sid))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }

                        ctx.AddOwnedSid(sid, true);
                        capSids.Add(sid);
                    }
                    else
                    {
                        // Need to derive SIDs for named caps
                        if (!PInvoke.DeriveCapabilitySidsFromName(
                                name,
                                out var groupSids,
                                out var groupCount,
                                out var sids,
                                out var sidCount))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }

                        for (var i = 0; i < groupCount; i++)
                            ctx.AddOwnedSid(Marshal.ReadIntPtr(groupSids, i * IntPtr.Size), false);
                        PInvoke.LocalFree(groupSids);

                        for (var i = 0; i < sidCount; i++)
                        {
                            var sid = Marshal.ReadIntPtr(sids, i * IntPtr.Size);
                            ctx.AddOwnedSid(sid, false);
                            capSids.Add(sid);
                        }

                        PInvoke.LocalFree(sids);
                    }
                }

                ctx.BuildCapabilityArray(capSids);

                var hr = PInvoke.CreateAppContainerProfile(
                    containerName,
                    displayName,
                    description,
                    ctx.CapabilitiesPtr,
                    (uint)ctx.CapabilityCount,
                    out var containerSid);

                if (hr == HResultAlreadyExists)
                {
                    Marshal.ThrowExceptionForHR(
                        PInvoke.DeriveAppContainerSidFromAppContainerName(containerName, out containerSid));
                }
                else
                {
                    Marshal.ThrowExceptionForHR(hr);
                }

                ctx.SetContainerSid(containerSid);
                return ctx;
            }
            catch
            {
                ctx.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Check whether the given SID already has at least the provided access on a path.
        /// </summary>
        /// <param name="path">Path of an existing file or directory.</param>
        /// <param name="sid">The SID to check.</param>
        /// <param name="accessMask">The required access mask.</param>
        /// <returns>Whether the SID already holds the required access.</returns>
        public static bool HasAccess(string path, IntPtr sid, uint accessMask)
        {
            var err = PInvoke.GetNamedSecurityInfoW(
                path,
                PInvoke.SE_FILE_OBJECT,
                PInvoke.DACL_SECURITY_INFORMATION,
                IntPtr.Zero,
                IntPtr.Zero,
                out var dacl,
                IntPtr.Zero,
                out var securityDescriptor);
            if (err != 0)
                return false;

            try
            {
                if (dacl == IntPtr.Zero)
                    return false;

                var trustee = BuildTrustee(sid);
                if (PInvoke.GetEffectiveRightsFromAclW(dacl, ref trustee, out var effective) != 0)
                    return false;

                return (effective & accessMask) == accessMask;
            }
            finally
            {
                if (securityDescriptor != IntPtr.Zero)
                    PInvoke.LocalFree(securityDescriptor);
            }
        }

        /// <summary>
        /// Ensure the given SID has at least accessMask on a path, writing the DACL when the access
        /// is not already present.
        /// </summary>
        /// <param name="path">Path of an existing file or directory.</param>
        /// <param name="sid">The SID to grant to.</param>
        /// <param name="accessMask">The required access mask.</param>
        /// <returns>What was needed to satisfy the request.</returns>
        /// <exception cref="Win32Exception">Thrown when a win32 error other than access denied occurs.</exception>
        public static GrantResult EnsureAccess(string path, IntPtr sid, uint accessMask)
        {
            if (HasAccess(path, sid, accessMask))
                return GrantResult.AlreadyGranted;

            try
            {
                GrantAccess(path, sid, accessMask);
                return GrantResult.Granted;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == PInvoke.ERROR_ACCESS_DENIED)
            {
                // Writing a DACL needs WRITE_DAC
                return GrantResult.AccessDenied;
            }
        }

        /// <summary>
        /// Grant the given SID access to a filesystem path (inheritable to children).
        /// </summary>
        /// <param name="path">Path of an existing file or directory.</param>
        /// <param name="sid">The SID to grant to.</param>
        /// <param name="accessMask">The access mask to grant.</param>
        /// <exception cref="Win32Exception">Thrown when a win32 error occurs.</exception>
        public static void GrantAccess(string path, IntPtr sid, uint accessMask)
        {
            var err = PInvoke.GetNamedSecurityInfoW(
                path,
                PInvoke.SE_FILE_OBJECT,
                PInvoke.DACL_SECURITY_INFORMATION,
                IntPtr.Zero,
                IntPtr.Zero,
                out var oldDacl,
                IntPtr.Zero,
                out var securityDescriptor);
            if (err != 0)
                throw new Win32Exception((int)err, $"GetNamedSecurityInfo failed for {path}");

            var newDacl = IntPtr.Zero;
            try
            {
                var ea = new PInvoke.EXPLICIT_ACCESS_W
                {
                    grfAccessPermissions = accessMask,
                    grfAccessMode = PInvoke.GRANT_ACCESS,
                    grfInheritance = PInvoke.SUB_CONTAINERS_AND_OBJECTS_INHERIT,
                    Trustee = BuildTrustee(sid),
                };

                err = PInvoke.SetEntriesInAclW(1, ref ea, oldDacl, out newDacl);
                if (err != 0)
                    throw new Win32Exception((int)err, $"SetEntriesInAcl failed for {path}");

                err = PInvoke.SetNamedSecurityInfoW(
                    path,
                    PInvoke.SE_FILE_OBJECT,
                    PInvoke.DACL_SECURITY_INFORMATION,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    newDacl,
                    IntPtr.Zero);
                if (err != 0)
                    throw new Win32Exception((int)err, $"SetNamedSecurityInfo failed for {path}");
            }
            finally
            {
                if (newDacl != IntPtr.Zero)
                    PInvoke.LocalFree(newDacl);
                if (securityDescriptor != IntPtr.Zero)
                    PInvoke.LocalFree(securityDescriptor);
            }
        }

        /// <summary>
        /// Try to add the container SID to the network isolation loopback exemption list.
        /// Requires elevation and returns false/logs when not possible.
        /// </summary>
        /// <param name="ctx">The launch context.</param>
        /// <returns>Whether the exemption is in place.</returns>
        public static bool TryAddLoopbackExemption(AppContainerLaunchContext ctx)
        {
            try
            {
                var err = PInvoke.NetworkIsolationGetAppContainerConfig(out var count, out var existing);
                if (err != 0)
                {
                    Log.Warning("NetworkIsolationGetAppContainerConfig failed: {Err}", err);
                    return false;
                }

                var entrySize = Marshal.SizeOf<PInvoke.SID_AND_ATTRIBUTES>();
                for (var i = 0; i < count; i++)
                {
                    var entry = Marshal.PtrToStructure<PInvoke.SID_AND_ATTRIBUTES>(existing + (i * entrySize));
                    if (entry.Sid != IntPtr.Zero && PInvoke.EqualSid(entry.Sid, ctx.ContainerSid))
                    {
                        Log.Verbose("Loopback exemption already present for {Sid}", ctx.ContainerSidString);
                        return true;
                    }
                }

                // Set() replaces the whole list so we need to append instead
                var newList = Marshal.AllocHGlobal(entrySize * (int)(count + 1));
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var entry = Marshal.PtrToStructure<PInvoke.SID_AND_ATTRIBUTES>(existing + (i * entrySize));
                        Marshal.StructureToPtr(entry, newList + (i * entrySize), false);
                    }

                    Marshal.StructureToPtr(
                        new PInvoke.SID_AND_ATTRIBUTES { Sid = ctx.ContainerSid, Attributes = 0 },
                        newList + ((int)count * entrySize),
                        false);

                    err = PInvoke.NetworkIsolationSetAppContainerConfig(count + 1, newList);
                    if (err != 0)
                    {
                        Log.Warning(
                            "Could not add loopback exemption (error {Err}). " +
                            "Make sure you are running this elevated or run this once from an elevated prompt instead: CheckNetIsolation.exe LoopbackExempt -a -p={Sid}",
                            err,
                            ctx.ContainerSidString);
                        return false;
                    }

                    Log.Information("Loopback exemption added for {Sid}", ctx.ContainerSidString);
                    return true;
                }
                finally
                {
                    Marshal.FreeHGlobal(newList);

                    // TODO: Free existing?
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to configure loopback exemption");
                return false;
            }
        }

        /// <summary>
        /// Whether the current process is running elevated.
        /// </summary>
        /// <returns>True if running with an elevated token.</returns>
        public static bool IsElevated()
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Convert a SID to its string form.
        /// </summary>
        /// <param name="sid">The SID.</param>
        /// <returns>The SID in string form.</returns>
        internal static string SidToString(IntPtr sid)
        {
            if (!PInvoke.ConvertSidToStringSidW(sid, out var strPtr))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                return Marshal.PtrToStringUni(strPtr) ?? string.Empty;
            }
            finally
            {
                PInvoke.LocalFree(strPtr);
            }
        }

        private static PInvoke.TRUSTEE_W BuildTrustee(IntPtr sid) => new()
        {
            pMultipleTrustee = IntPtr.Zero,
            MultipleTrusteeOperation = 0, // NO_MULTIPLE_TRUSTEE
            TrusteeForm = PInvoke.TRUSTEE_IS_SID,
            TrusteeType = PInvoke.TRUSTEE_IS_WELL_KNOWN_GROUP,
            ptstrName = sid,
        };

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:Use built-in type alias", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "WINAPI conventions")]
        internal static class PInvoke
        {
            public const uint DELETE = 0x00010000;

            public const uint FILE_GENERIC_READ = 0x00120089;
            public const uint FILE_GENERIC_WRITE = 0x00120116;
            public const uint FILE_GENERIC_EXECUTE = 0x001200A0;

            public const int ERROR_ACCESS_DENIED = 5;

            public const uint SE_GROUP_ENABLED = 0x00000004;

            public const uint SECURITY_CAPABILITY_BASE_RID = 3;
            public const uint SECURITY_CAPABILITY_INTERNET_CLIENT = 1;
            public const uint SECURITY_CAPABILITY_INTERNET_CLIENT_SERVER = 2;
            public const uint SECURITY_CAPABILITY_PRIVATE_NETWORK_CLIENT_SERVER = 3;

            public const int SE_FILE_OBJECT = 1;
            public const uint DACL_SECURITY_INFORMATION = 4;

            public const int GRANT_ACCESS = 1;
            public const uint SUB_CONTAINERS_AND_OBJECTS_INHERIT = 0x3;
            public const int TRUSTEE_IS_SID = 0;
            public const int TRUSTEE_IS_WELL_KNOWN_GROUP = 5;

            [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
            public static extern int CreateAppContainerProfile(
                string pszAppContainerName,
                string pszDisplayName,
                string pszDescription,
                IntPtr pCapabilities,
                uint dwCapabilityCount,
                out IntPtr ppSidAppContainerSid);

            [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
            public static extern int DeriveAppContainerSidFromAppContainerName(
                string pszAppContainerName,
                out IntPtr ppsidAppContainerSid);

            [DllImport("kernelbase.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeriveCapabilitySidsFromName(
                string capName,
                out IntPtr capabilityGroupSids,
                out uint capabilityGroupSidCount,
                out IntPtr capabilitySids,
                out uint capabilitySidCount);

            [DllImport("advapi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AllocateAndInitializeSid(
                ref SID_IDENTIFIER_AUTHORITY pIdentifierAuthority,
                byte nSubAuthorityCount,
                uint nSubAuthority0,
                uint nSubAuthority1,
                uint nSubAuthority2,
                uint nSubAuthority3,
                uint nSubAuthority4,
                uint nSubAuthority5,
                uint nSubAuthority6,
                uint nSubAuthority7,
                out IntPtr pSid);

            [DllImport("advapi32.dll")]
            public static extern IntPtr FreeSid(IntPtr pSid);

            [DllImport("advapi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EqualSid(IntPtr pSid1, IntPtr pSid2);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool ConvertSidToStringSidW(IntPtr sid, out IntPtr stringSid);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
            public static extern uint GetNamedSecurityInfoW(
                string pObjectName,
                int objectType,
                uint securityInfo,
                IntPtr ppsidOwner,
                IntPtr ppsidGroup,
                out IntPtr ppDacl,
                IntPtr ppSacl,
                out IntPtr ppSecurityDescriptor);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
            public static extern uint GetEffectiveRightsFromAclW(
                IntPtr pAcl,
                ref TRUSTEE_W pTrustee,
                out uint pAccessRights);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
            public static extern uint SetEntriesInAclW(
                uint cCountOfExplicitEntries,
                ref EXPLICIT_ACCESS_W pListOfExplicitEntries,
                IntPtr oldAcl,
                out IntPtr newAcl);

            [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
            public static extern uint SetNamedSecurityInfoW(
                string pObjectName,
                int objectType,
                uint securityInfo,
                IntPtr psidOwner,
                IntPtr psidGroup,
                IntPtr pDacl,
                IntPtr pSacl);

            [DllImport("firewallapi.dll")]
            public static extern uint NetworkIsolationGetAppContainerConfig(
                out uint pdwNumPublicAppCs,
                out IntPtr appContainerSids);

            [DllImport("firewallapi.dll")]
            public static extern uint NetworkIsolationSetAppContainerConfig(
                uint dwNumPublicAppCs,
                IntPtr appContainerSids);

            [DllImport("kernel32.dll")]
            public static extern IntPtr LocalFree(IntPtr hMem);

            [StructLayout(LayoutKind.Sequential)]
            public struct SID_IDENTIFIER_AUTHORITY
            {
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
                public byte[] Value;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SID_AND_ATTRIBUTES
            {
                public IntPtr Sid;
                public uint Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TRUSTEE_W
            {
                public IntPtr pMultipleTrustee;
                public int MultipleTrusteeOperation;
                public int TrusteeForm;
                public int TrusteeType;
                public IntPtr ptstrName;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct EXPLICIT_ACCESS_W
            {
                public uint grfAccessPermissions;
                public int grfAccessMode;
                public uint grfInheritance;
                public TRUSTEE_W Trustee;
            }
        }
    }
}
