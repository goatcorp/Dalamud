using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

using Serilog;

// ReSharper disable InconsistentNaming

namespace Dalamud.Injector
{
    /// <summary>
    /// Class responsible for starting the game and stripping ACL protections from processes.
    /// </summary>
    public static class GameStart
    {
        /// <summary>
        /// Start a process without ACL protections.
        /// </summary>
        /// <param name="workingDir">The working directory.</param>
        /// <param name="exePath">The path to the executable file.</param>
        /// <param name="arguments">Arguments to pass to the executable file.</param>
        /// <param name="dontFixAcl">Don't actually fix the ACL.</param>
        /// <param name="dontUnelevate">Don't unelevate, if ran as Administrator. Has no effect if UAC is disabled.</param>
        /// <param name="beforeResume">Action to execute before the process is started.</param>
        /// <param name="waitForGameWindow">Wait for the game window to be ready before proceeding.</param>
        /// <returns>The started process.</returns>
        /// <exception cref="Win32Exception">Thrown when a win32 error occurs.</exception>
        /// <exception cref="GameStartException">Thrown when the process did not start correctly.</exception>
        public static Process LaunchGame(string workingDir, string exePath, string arguments, bool dontFixAcl, bool dontUnelevate, Action<Process>? beforeResume, bool waitForGameWindow = true)
        {
            Process process = null;

            var psecDesc = nint.Zero;
            if (!dontFixAcl)
            {
                var userName = Environment.UserName;

                var pExplicitAccess = default(PInvoke.EXPLICIT_ACCESS);
                PInvoke.BuildExplicitAccessWithName(
                    ref pExplicitAccess,
                    userName,
                    PInvoke.STANDARD_RIGHTS_ALL | PInvoke.SPECIFIC_RIGHTS_ALL & ~PInvoke.PROCESS_VM_WRITE,
                    PInvoke.GRANT_ACCESS,
                    0);

                if (PInvoke.SetEntriesInAcl(1, ref pExplicitAccess, IntPtr.Zero, out var newAcl) != 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!PInvoke.InitializeSecurityDescriptor(out var secDesc, PInvoke.SECURITY_DESCRIPTOR_REVISION))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!PInvoke.SetSecurityDescriptorDacl(ref secDesc, true, newAcl, false))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                psecDesc = Marshal.AllocHGlobal(Marshal.SizeOf<PInvoke.SECURITY_DESCRIPTOR>());
                Marshal.StructureToPtr(secDesc, psecDesc, true);
            }

            var procAttrListBuffer = nint.Zero;
            var parentProcessHandle = nint.Zero;
            if (!dontUnelevate)
            {
                try
                {
                    if (PInvoke.GetWindowThreadProcessId(PInvoke.GetShellWindow(), out var shellProcessId) == 0)
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                    parentProcessHandle = PInvoke.OpenProcess(PInvoke.PROCESS_CREATE_PROCESS, false, shellProcessId);
                    if (parentProcessHandle == nint.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    var nAttrListSize = nint.Zero;

                    // The following call is expected to fail with a specific win32 error code.
                    if (PInvoke.InitializeProcThreadAttributeList(nint.Zero, 1, 0, ref nAttrListSize)
                        || Marshal.GetLastWin32Error() != PInvoke.ERROR_INSUFFICIENT_BUFFER)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    procAttrListBuffer = Marshal.AllocHGlobal(nAttrListSize);
                    // This one should succeed.
                    if (!PInvoke.InitializeProcThreadAttributeList(procAttrListBuffer, 1, 0, ref nAttrListSize))
                    {
                        Marshal.FreeHGlobal(procAttrListBuffer);
                        procAttrListBuffer = nint.Zero;
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    unsafe
                    {
                        if (!PInvoke.UpdateProcThreadAttribute(
                                procAttrListBuffer,
                                0,
                                PInvoke.PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                                (nint)(&parentProcessHandle),
                                Marshal.SizeOf<nint>(),
                                nint.Zero,
                                nint.Zero))
                        {
                            PInvoke.DeleteProcThreadAttributeList(procAttrListBuffer);
                            Marshal.FreeHGlobal(procAttrListBuffer);
                            procAttrListBuffer = nint.Zero;
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "[GameStart] Failed to prepare for launching the game unelevated.");
                }
            }

            var lpProcessInformation = default(PInvoke.PROCESS_INFORMATION);
            try
            {
                var lpProcessAttributes = new PInvoke.SECURITY_ATTRIBUTES
                {
                    nLength = Marshal.SizeOf<PInvoke.SECURITY_ATTRIBUTES>(),
                    lpSecurityDescriptor = psecDesc,
                    bInheritHandle = false,
                };

                var lpStartupInfo = new PInvoke.STARTUPINFOEX
                {
                    StartupInfo = new PInvoke.STARTUPINFO
                    {
                        cb = procAttrListBuffer == 0 ? Marshal.SizeOf<PInvoke.STARTUPINFO>() : Marshal.SizeOf<PInvoke.STARTUPINFOEX>(),
                    },
                    lpAttributeList = procAttrListBuffer,
                };

                var envvars = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().ToDictionary(x => (string)x.Key, x => (string)x.Value);
                envvars["__COMPAT_LAYER"] = string.Join(
                    ' ',
                    Regex.Split(envvars.GetValueOrDefault("__COMPAT_LAYER", string.Empty), "\\s+")
                         .Where(x => !string.IsNullOrEmpty(x))
                         .Append("RunAsInvoker")
                         .Distinct());

                if (!PInvoke.CreateProcess(
                        exePath,
                        $"\"{exePath}\" {arguments}",
                        ref lpProcessAttributes,
                        nint.Zero,
                        false,
                        PInvoke.CREATE_SUSPENDED | PInvoke.CREATE_UNICODE_ENVIRONMENT | (procAttrListBuffer == 0 ? 0 : PInvoke.EXTENDED_STARTUPINFO_PRESENT),
                        string.Join('\0', envvars.Select(x => $"{x.Key}={x.Value}").Append(string.Empty)),
                        workingDir,
                        ref lpStartupInfo,
                        out lpProcessInformation))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                if (!dontFixAcl)
                    DisableSeDebug(lpProcessInformation.hProcess);

                process = new ExistingProcess(lpProcessInformation.hProcess);

                beforeResume?.Invoke(process);

                if (PInvoke.ResumeThread(lpProcessInformation.hThread) == uint.MaxValue)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                // Ensure that the game main window is prepared
                if (waitForGameWindow)
                {
                    try
                    {
                        var tries = 0;
                        const int maxTries = 1200;
                        const int timeout = 50;

                        do
                        {
                            Thread.Sleep(timeout);

                            if (process.HasExited)
                                throw new GameStartException();

                            if (tries > maxTries)
                                throw new GameStartException($"Couldn't find game window after {maxTries * timeout}ms");

                            tries++;
                        }
                        while (TryFindGameWindow(process) == IntPtr.Zero);
                    }
                    catch (InvalidOperationException)
                    {
                        throw new GameStartException("Could not read process information.");
                    }
                }

                if (!dontFixAcl)
                    CopyAclFromSelfToTargetProcess(lpProcessInformation.hProcess);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[GameStart] Uncaught error during initialization, trying to kill process");

                try
                {
                    process?.Kill();
                }
                catch (Exception killEx)
                {
                    Log.Error(killEx, "[GameStart] Could not kill process");
                }

                throw;
            }
            finally
            {
                if (psecDesc != nint.Zero)
                    Marshal.FreeHGlobal(psecDesc);
                if (lpProcessInformation.hThread != nint.Zero)
                    PInvoke.CloseHandle(lpProcessInformation.hThread);
                if (parentProcessHandle != nint.Zero)
                    PInvoke.CloseHandle(parentProcessHandle);
                if (procAttrListBuffer != nint.Zero)
                {
                    PInvoke.DeleteProcThreadAttributeList(procAttrListBuffer);
                    Marshal.FreeHGlobal(procAttrListBuffer);
                }
            }

            return process;
        }

        /// <summary>
        /// Copies ACL of current process to the target process.
        /// </summary>
        /// <param name="hProcess">Native handle to the target process.</param>
        /// <exception cref="Win32Exception">Thrown when a win32 error occurs.</exception>
        public static void CopyAclFromSelfToTargetProcess(IntPtr hProcess)
        {
            if (PInvoke.GetSecurityInfo(
                    PInvoke.GetCurrentProcess(),
                    PInvoke.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                    PInvoke.SECURITY_INFORMATION.DACL_SECURITY_INFORMATION,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out var pACL,
                    IntPtr.Zero,
                    IntPtr.Zero) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (PInvoke.SetSecurityInfo(
                    hProcess,
                    PInvoke.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                    PInvoke.SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | PInvoke.SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pACL,
                    IntPtr.Zero) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// Claim a SE Debug Privilege.
        /// </summary>
        public static void ClaimSeDebug()
        {
            var hToken = PInvoke.INVALID_HANDLE_VALUE;
            try
            {
                if (!PInvoke.OpenThreadToken(PInvoke.GetCurrentThread(), PInvoke.TOKEN_QUERY | PInvoke.TOKEN_ADJUST_PRIVILEGES, false, out hToken))
                {
                    if (Marshal.GetLastWin32Error() != PInvoke.ERROR_NO_TOKEN)
                        throw new Exception("ClaimSeDebug.OpenProcessToken#1", new Win32Exception(Marshal.GetLastWin32Error()));

                    if (!PInvoke.ImpersonateSelf(PInvoke.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
                        throw new Exception("ClaimSeDebug.ImpersonateSelf", new Win32Exception(Marshal.GetLastWin32Error()));

                    if (!PInvoke.OpenThreadToken(PInvoke.GetCurrentThread(), PInvoke.TOKEN_QUERY | PInvoke.TOKEN_ADJUST_PRIVILEGES, false, out hToken))
                        throw new Exception("ClaimSeDebug.OpenProcessToken#2", new Win32Exception(Marshal.GetLastWin32Error()));
                }

                var luidDebugPrivilege = default(PInvoke.LUID);
                if (!PInvoke.LookupPrivilegeValue(null, PInvoke.SE_DEBUG_NAME, ref luidDebugPrivilege))
                    throw new Exception("ClaimSeDebug.LookupPrivilegeValue", new Win32Exception(Marshal.GetLastWin32Error()));

                var tpLookup = new PInvoke.TOKEN_PRIVILEGES()
                {
                    PrivilegeCount = 1,
                    Privileges = new PInvoke.LUID_AND_ATTRIBUTES[1]
                    {
                        new PInvoke.LUID_AND_ATTRIBUTES()
                        {
                            Luid = luidDebugPrivilege,
                            Attributes = PInvoke.SE_PRIVILEGE_ENABLED,
                        },
                    },
                };

                if (!PInvoke.AdjustTokenPrivileges(hToken, false, ref tpLookup, 0, IntPtr.Zero, IntPtr.Zero))
                    throw new Exception("ClaimSeDebug.AdjustTokenPrivileges", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            finally
            {
                if (hToken != PInvoke.INVALID_HANDLE_VALUE && hToken != IntPtr.Zero)
                    PInvoke.CloseHandle(hToken);
            }
        }

        private static void DisableSeDebug(IntPtr processHandle)
        {
            if (!PInvoke.OpenProcessToken(processHandle, PInvoke.TOKEN_QUERY | PInvoke.TOKEN_ADJUST_PRIVILEGES, out var tokenHandle))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var luidDebugPrivilege = default(PInvoke.LUID);
            if (!PInvoke.LookupPrivilegeValue(null, PInvoke.SE_DEBUG_NAME, ref luidDebugPrivilege))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var requiredPrivileges = new PInvoke.PRIVILEGE_SET
            {
                PrivilegeCount = 1,
                Control = PInvoke.PRIVILEGE_SET_ALL_NECESSARY,
                Privilege = new PInvoke.LUID_AND_ATTRIBUTES[1],
            };

            requiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
            requiredPrivileges.Privilege[0].Attributes = PInvoke.SE_PRIVILEGE_ENABLED;

            if (!PInvoke.PrivilegeCheck(tokenHandle, ref requiredPrivileges, out bool bResult))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // SeDebugPrivilege is enabled; try disabling it
            if (bResult)
            {
                var tokenPrivileges = new PInvoke.TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Privileges = new PInvoke.LUID_AND_ATTRIBUTES[1],
                };

                tokenPrivileges.Privileges[0].Luid = luidDebugPrivilege;
                tokenPrivileges.Privileges[0].Attributes = PInvoke.SE_PRIVILEGE_REMOVED;

                if (!PInvoke.AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            PInvoke.CloseHandle(tokenHandle);
        }

        private static IntPtr TryFindGameWindow(Process process)
        {
            IntPtr hwnd = IntPtr.Zero;
            while ((hwnd = PInvoke.FindWindowEx(IntPtr.Zero, hwnd, "FFXIVGAME", IntPtr.Zero)) != IntPtr.Zero)
            {
                PInvoke.GetWindowThreadProcessId(hwnd, out uint pid);

                if (pid == process.Id && PInvoke.IsWindowVisible(hwnd))
                {
                    break;
                }
            }

            return hwnd;
        }

        /// <summary>
        /// Exception thrown when the process has exited before a window could be found.
        /// </summary>
        public class GameStartException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="GameStartException"/> class.
            /// </summary>
            /// <param name="message">The message to pass on.</param>
            public GameStartException(string? message = null)
                : base(message ?? "Game exited prematurely.")
            {
            }
        }

        // Definitions taken from PInvoke.net (with some changes)
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:Use built-in type alias", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1400:Access modifier should be declared", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:Field names should begin with lower-case letter", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "WINAPI conventions")]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "WINAPI conventions")]
        private static class PInvoke
        {
            #region Constants
            public const string SE_DEBUG_NAME = "SeDebugPrivilege";

            public const UInt32 STANDARD_RIGHTS_ALL = 0x001F0000;
            public const UInt32 SPECIFIC_RIGHTS_ALL = 0x0000FFFF;
            public const UInt32 PROCESS_VM_WRITE = 0x0020;

            public const UInt32 GRANT_ACCESS = 1;

            public const UInt32 SECURITY_DESCRIPTOR_REVISION = 1;

            public const UInt32 CREATE_SUSPENDED = 0x00000004;
            public const UInt32 CREATE_UNICODE_ENVIRONMENT = 0x00000400;
            public const UInt32 EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

            public const IntPtr PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;

            public const UInt32 PROCESS_CREATE_PROCESS = 0x00000080;

            public const UInt32 TOKEN_QUERY = 0x0008;
            public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;

            public const UInt32 PRIVILEGE_SET_ALL_NECESSARY = 1;

            public const UInt32 SE_PRIVILEGE_ENABLED = 0x00000002;
            public const UInt32 SE_PRIVILEGE_REMOVED = 0x00000004;

            public const UInt32 ERROR_NO_TOKEN = 0x000003F0;
            public const UInt32 ERROR_INSUFFICIENT_BUFFER = 0x0000007A;

            public static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

            public enum MULTIPLE_TRUSTEE_OPERATION
            {
                NO_MULTIPLE_TRUSTEE,
                TRUSTEE_IS_IMPERSONATE,
            }

            public enum TRUSTEE_FORM
            {
                TRUSTEE_IS_SID,
                TRUSTEE_IS_NAME,
                TRUSTEE_BAD_FORM,
                TRUSTEE_IS_OBJECTS_AND_SID,
                TRUSTEE_IS_OBJECTS_AND_NAME,
            }

            public enum TRUSTEE_TYPE
            {
                TRUSTEE_IS_UNKNOWN,
                TRUSTEE_IS_USER,
                TRUSTEE_IS_GROUP,
                TRUSTEE_IS_DOMAIN,
                TRUSTEE_IS_ALIAS,
                TRUSTEE_IS_WELL_KNOWN_GROUP,
                TRUSTEE_IS_DELETED,
                TRUSTEE_IS_INVALID,
                TRUSTEE_IS_COMPUTER,
            }

            public enum SE_OBJECT_TYPE
            {
                SE_UNKNOWN_OBJECT_TYPE,
                SE_FILE_OBJECT,
                SE_SERVICE,
                SE_PRINTER,
                SE_REGISTRY_KEY,
                SE_LMSHARE,
                SE_KERNEL_OBJECT,
                SE_WINDOW_OBJECT,
                SE_DS_OBJECT,
                SE_DS_OBJECT_ALL,
                SE_PROVIDER_DEFINED_OBJECT,
                SE_WMIGUID_OBJECT,
                SE_REGISTRY_WOW64_32KEY,
            }

            [Flags]
            public enum SECURITY_INFORMATION
            {
                OWNER_SECURITY_INFORMATION = 1,
                GROUP_SECURITY_INFORMATION = 2,
                DACL_SECURITY_INFORMATION = 4,
                SACL_SECURITY_INFORMATION = 8,
                UNPROTECTED_SACL_SECURITY_INFORMATION = 0x10000000,
                UNPROTECTED_DACL_SECURITY_INFORMATION = 0x20000000,
                PROTECTED_SACL_SECURITY_INFORMATION = 0x40000000,
            }

            public enum SECURITY_IMPERSONATION_LEVEL
            {
                SecurityAnonymous,
                SecurityIdentification,
                SecurityImpersonation,
                SecurityDelegation,
            }
            #endregion

            #region Methods

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(
                uint processAccess,
                bool bInheritHandle,
                uint processId);

            [DllImport("user32.dll")]
            public static extern IntPtr GetShellWindow();

            [DllImport("kernel32.dll", SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool InitializeProcThreadAttributeList(
                IntPtr lpAttributeList,
                int dwAttributeCount,
                int dwFlags,
                ref IntPtr lpSize);

            [DllImport("kernel32.dll", SetLastError=true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UpdateProcThreadAttribute(
                IntPtr lpAttributeList,
                uint dwFlags,
                IntPtr nAttribute,
                IntPtr lpValue,
                IntPtr cbSize,
                IntPtr lpPreviousValue,
                IntPtr lpReturnSize);

            [DllImport("kernel32.dll", SetLastError=true)]
            public static extern void DeleteProcThreadAttributeList(
                IntPtr lpAttributeList);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern void BuildExplicitAccessWithName(
                ref EXPLICIT_ACCESS pExplicitAccess,
                string pTrusteeName,
                uint accessPermissions,
                uint accessMode,
                uint inheritance);

            [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int SetEntriesInAcl(
                int cCountOfExplicitEntries,
                ref EXPLICIT_ACCESS pListOfExplicitEntries,
                IntPtr oldAcl,
                out IntPtr newAcl);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool InitializeSecurityDescriptor(
                out SECURITY_DESCRIPTOR pSecurityDescriptor,
                uint dwRevision);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool SetSecurityDescriptorDacl(
                ref SECURITY_DESCRIPTOR pSecurityDescriptor,
                bool bDaclPresent,
                IntPtr pDacl,
                bool bDaclDefaulted);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool CreateProcess(
               string? lpApplicationName,
               string? lpCommandLine,
               ref SECURITY_ATTRIBUTES lpProcessAttributes,
               IntPtr lpThreadAttributes,
               bool bInheritHandles,
               UInt32 dwCreationFlags,
               string? lpEnvironment,
               string? lpCurrentDirectory,
               [In] ref STARTUPINFOEX lpStartupInfo,
               out PROCESS_INFORMATION lpProcessInformation);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint ResumeThread(IntPtr hThread);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool ImpersonateSelf(
                SECURITY_IMPERSONATION_LEVEL impersonationLevel);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool OpenProcessToken(
                IntPtr processHandle,
                UInt32 desiredAccess,
                out IntPtr tokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool OpenThreadToken(
                IntPtr threadHandle,
                uint desiredAccess,
                bool openAsSelf,
                out IntPtr tokenHandle);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref LUID lpLuid);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool PrivilegeCheck(
                IntPtr clientToken,
                ref PRIVILEGE_SET requiredPrivileges,
                out bool pfResult);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern bool AdjustTokenPrivileges(
                IntPtr tokenHandle,
                bool disableAllPrivileges,
                ref TOKEN_PRIVILEGES newState,
                int cbPreviousState,
                IntPtr previousState,
                IntPtr cbOutPreviousState);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern uint GetSecurityInfo(
                IntPtr handle,
                SE_OBJECT_TYPE objectType,
                SECURITY_INFORMATION securityInfo,
                IntPtr pSidOwner,
                IntPtr pSidGroup,
                out IntPtr pDacl,
                IntPtr pSacl,
                IntPtr pSecurityDescriptor);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern uint SetSecurityInfo(
                IntPtr handle,
                SE_OBJECT_TYPE objectType,
                SECURITY_INFORMATION securityInfo,
                IntPtr psidOwner,
                IntPtr psidGroup,
                IntPtr pDacl,
                IntPtr pSacl);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetCurrentProcess();

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr GetCurrentThread();

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, IntPtr windowTitle);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool IsWindowVisible(IntPtr hWnd);

            #endregion

            #region Structures

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 0)]
            public struct TRUSTEE : IDisposable
            {
                public IntPtr pMultipleTrustee;
                public MULTIPLE_TRUSTEE_OPERATION MultipleTrusteeOperation;
                public TRUSTEE_FORM TrusteeForm;
                public TRUSTEE_TYPE TrusteeType;
                private IntPtr ptstrName;

                public string Name => Marshal.PtrToStringAuto(this.ptstrName) ?? string.Empty;

#pragma warning disable CA1416

                void IDisposable.Dispose()
                {
                    if (this.ptstrName != IntPtr.Zero) Marshal.Release(this.ptstrName);
                }

#pragma warning restore CA1416
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 0)]
            public struct EXPLICIT_ACCESS
            {
                uint grfAccessPermissions;
                uint grfAccessMode;
                uint grfInheritance;
                TRUSTEE Trustee;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_DESCRIPTOR
            {
                public byte Revision;
                public byte Sbz1;
                public UInt16 Control;
                public IntPtr Owner;
                public IntPtr Group;
                public IntPtr Sacl;
                public IntPtr Dacl;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct STARTUPINFO
            {
                public Int32 cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public Int32 dwX;
                public Int32 dwY;
                public Int32 dwXSize;
                public Int32 dwYSize;
                public Int32 dwXCountChars;
                public Int32 dwYCountChars;
                public Int32 dwFillAttribute;
                public Int32 dwFlags;
                public Int16 wShowWindow;
                public Int16 cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct STARTUPINFOEX
            {
                public STARTUPINFO StartupInfo;
                public IntPtr lpAttributeList;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public int dwProcessId;
                public UInt32 dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct SECURITY_ATTRIBUTES
            {
                public int nLength;
                public IntPtr lpSecurityDescriptor;
                public bool bInheritHandle;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LUID
            {
                public UInt32 LowPart;
                public Int32 HighPart;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PRIVILEGE_SET
            {
                public UInt32 PrivilegeCount;
                public UInt32 Control;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LUID_AND_ATTRIBUTES[] Privilege;
            }

            public struct LUID_AND_ATTRIBUTES
            {
                public LUID Luid;
                public UInt32 Attributes;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct TOKEN_PRIVILEGES
            {
                public UInt32 PrivilegeCount;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public LUID_AND_ATTRIBUTES[] Privileges;
            }
            #endregion
        }
    }
}
