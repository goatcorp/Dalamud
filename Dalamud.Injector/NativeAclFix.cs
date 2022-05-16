using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Injector.Exceptions;
using Serilog;

using static Dalamud.Injector.NativeFunctions;

// ReSharper disable InconsistentNaming

namespace Dalamud.Injector;

/// <summary>
/// Class responsible for stripping ACL protections from processes.
/// </summary>
public static class NativeAclFix
{
    /// <summary>
    /// Start a process without ACL protections.
    /// </summary>
    /// <param name="workingDir">The working directory.</param>
    /// <param name="exePath">The path to the executable file.</param>
    /// <param name="arguments">Arguments to pass to the executable file.</param>
    /// <param name="beforeResume">Action to execute before the process is started.</param>
    /// <returns>The started process.</returns>
    /// <exception cref="Win32Exception">Thrown when a win32 error occurs.</exception>
    /// <exception cref="GameExitedException">Thrown when the process did not start correctly.</exception>
    public static Process LaunchGame(string workingDir, string exePath, string arguments, Action<Process> beforeResume)
    {
        Process process = null;

        var userName = Environment.UserName;

        var pExplicitAccess = default(EXPLICIT_ACCESS);
        BuildExplicitAccessWithName(
            ref pExplicitAccess,
            userName,
            STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL & ~PROCESS_VM_WRITE,
            ACCESS_MODE.GRANT_ACCESS,
            0);

        if (SetEntriesInAcl(1, ref pExplicitAccess, IntPtr.Zero, out var newAcl) != 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!InitializeSecurityDescriptor(out var secDesc, SECURITY_DESCRIPTOR_REVISION))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!SetSecurityDescriptorDacl(ref secDesc, true, newAcl, false))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var psecDesc = Marshal.AllocHGlobal(Marshal.SizeOf<SECURITY_DESCRIPTOR>());
        Marshal.StructureToPtr(secDesc, psecDesc, true);

        var lpProcessInformation = default(PROCESS_INFORMATION);
        try
        {
            var lpProcessAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                lpSecurityDescriptor = psecDesc,
                bInheritHandle = false,
            };

            var lpStartupInfo = new STARTUPINFO
            {
                cb = Marshal.SizeOf<STARTUPINFO>(),
            };

            var compatLayerPrev = Environment.GetEnvironmentVariable("__COMPAT_LAYER");

            Environment.SetEnvironmentVariable("__COMPAT_LAYER", "RunAsInvoker");
            try
            {
                if (!CreateProcess(
                        null,
                        $"\"{exePath}\" {arguments}",
                        ref lpProcessAttributes,
                        IntPtr.Zero,
                        false,
                        CREATE_SUSPENDED,
                        IntPtr.Zero,
                        workingDir,
                        ref lpStartupInfo,
                        out lpProcessInformation))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("__COMPAT_LAYER", compatLayerPrev);
            }

            DisableSeDebug(lpProcessInformation.hProcess);

            process = new ExistingProcess(lpProcessInformation.hProcess);

            beforeResume?.Invoke(process);

            ResumeThread(lpProcessInformation.hThread);

            // Ensure that the game main window is prepared
            try
            {
                do
                {
                    process.WaitForInputIdle();

                    Thread.Sleep(100);
                }
                while (TryFindGameWindow(process) == IntPtr.Zero);
            }
            catch (InvalidOperationException)
            {
                throw new GameExitedException();
            }

            if (GetSecurityInfo(
                    GetCurrentProcess(),
                    SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                    DACL_SECURITY_INFORMATION,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out var pACL,
                    IntPtr.Zero,
                    IntPtr.Zero) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            if (SetSecurityInfo(
                    lpProcessInformation.hProcess,
                    SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                    DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    pACL,
                    IntPtr.Zero) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[NativeAclFix] Uncaught error during initialization, trying to kill process");

            try
            {
                process?.Kill();
            }
            catch (Exception killEx)
            {
                Log.Error(killEx, "[NativeAclFix] Could not kill process");
            }

            throw;
        }
        finally
        {
            Marshal.FreeHGlobal(psecDesc);
            CloseHandle(lpProcessInformation.hThread);
        }

        return process;
    }

    private static void DisableSeDebug(IntPtr processHandle)
    {
        if (!OpenProcessToken(processHandle, TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out var tokenHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var luidDebugPrivilege = default(LUID);
        if (!LookupPrivilegeValue(null, "SeDebugPrivilege", ref luidDebugPrivilege))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var requiredPrivileges = new PRIVILEGE_SET
        {
            PrivilegeCount = 1,
            Control = PRIVILEGE_SET_ALL_NECESSARY,
            Privilege = new LUID_AND_ATTRIBUTES[1],
        };

        requiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
        requiredPrivileges.Privilege[0].Attributes = SE_PRIVILEGE_ENABLED;

        if (!PrivilegeCheck(tokenHandle, ref requiredPrivileges, out bool bResult))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // SeDebugPrivilege is enabled; try disabling it
        if (bResult)
        {
            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES[1],
            };

            tokenPrivileges.Privileges[0].Luid = luidDebugPrivilege;
            tokenPrivileges.Privileges[0].Attributes = SE_PRIVILEGE_REMOVED;

            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        CloseHandle(tokenHandle);
    }

    private static IntPtr TryFindGameWindow(Process process)
    {
        IntPtr hwnd = IntPtr.Zero;
        while ((hwnd = FindWindowEx(IntPtr.Zero, hwnd, "FFXIVGAME", IntPtr.Zero)) != IntPtr.Zero)
        {
            GetWindowThreadProcessId(hwnd, out uint pid);

            if (pid == process.Id && IsWindowVisible(hwnd))
            {
                break;
            }
        }

        return hwnd;
    }
}
