using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using Dalamud.Injector.Exceptions;
using Serilog;

using static Dalamud.Injector.NativeFunctions;

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
        var processInformation = default(PROCESS_INFORMATION);
        var pSecDesc = IntPtr.Zero;

        try
        {
            CreateSecurityDescriptor(out pSecDesc);
            CreateProcessSuspended(exePath, arguments, workingDir, pSecDesc, ref process, ref processInformation);
            DisableSeDebug(processInformation.hProcess);

            beforeResume?.Invoke(process);

            ResumeProcess(processInformation);
            WaitForGamewindow(process);
            UpdateSecurityInfo(processInformation);
        }
        catch (Exception ex)
        {
            if (process is null || process.HasExited)
            {
                Log.Error(ex, "[NativeAclFix] Uncaught error during initialization");
            }
            else
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
            }

            throw;
        }
        finally
        {
            Marshal.FreeHGlobal(pSecDesc);
            CloseHandle(processInformation.hThread);
        }

        return process;
    }

    /// <summary>
    /// Create a security descriptor.
    /// </summary>
    /// <param name="pSecDesc">Pointer to the new security descriptor.</param>
    private static void CreateSecurityDescriptor(out IntPtr pSecDesc)
    {
        var pExplicitAccess = default(EXPLICIT_ACCESS);
        BuildExplicitAccessWithName(
            ref pExplicitAccess,
            Environment.UserName,
            (STANDARD_RIGHTS_ALL | SPECIFIC_RIGHTS_ALL) & ~PROCESS_VM_WRITE,
            ACCESS_MODE.GRANT_ACCESS,
            0);

        if (SetEntriesInAcl(1, ref pExplicitAccess, IntPtr.Zero, out var newAcl) != 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!InitializeSecurityDescriptor(out var secDesc, SECURITY_DESCRIPTOR_REVISION))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        if (!SetSecurityDescriptorDacl(ref secDesc, true, newAcl, false))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        pSecDesc = Marshal.AllocHGlobal(Marshal.SizeOf<SECURITY_DESCRIPTOR>());
        Marshal.StructureToPtr(secDesc, pSecDesc, true);
    }

    /// <summary>
    /// Create a new process, suspended.
    /// </summary>
    /// <param name="exePath">Path to the exe.</param>
    /// <param name="arguments">Stringified arguments.</param>
    /// <param name="workingDir">Working directory.</param>
    /// <param name="pSecDesc">Pointer to a security descriptor.</param>
    /// <param name="process">Target process.</param>
    /// <param name="processInformation">Target process information.</param>
    private static void CreateProcessSuspended(string exePath, string arguments, string workingDir, IntPtr pSecDesc, ref Process process, ref PROCESS_INFORMATION processInformation)
    {
        var lpProcessAttributes = new SECURITY_ATTRIBUTES
        {
            nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
            lpSecurityDescriptor = pSecDesc,
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
                    out processInformation))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("__COMPAT_LAYER", compatLayerPrev);
        }

        process = new ExistingProcess(processInformation.hProcess);
    }

    /// <summary>
    /// Disable SeDebug is present.
    /// </summary>
    /// <param name="processHandle">Process handle.</param>
    private static void DisableSeDebug(IntPtr processHandle)
    {
        if (!OpenProcessToken(processHandle, TOKEN_QUERY | TOKEN_ADJUST_PRIVILEGES, out var tokenHandle))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var luidDebugPrivilege = default(LUID);
        if (!LookupPrivilegeValue(null, "SeDebugPrivilege", ref luidDebugPrivilege))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var requiredPrivileges = new PRIVILEGE_SET
        {
            PrivilegeCount = 1,
            Control = PRIVILEGE_SET_ALL_NECESSARY,
            Privilege = new LUID_AND_ATTRIBUTES[1],
        };

        requiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
        requiredPrivileges.Privilege[0].Attributes = SE_PRIVILEGE_ENABLED;

        if (!PrivilegeCheck(tokenHandle, ref requiredPrivileges, out var result))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        // SeDebugPrivilege is enabled; try disabling it
        if (result)
        {
            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES[1],
            };

            tokenPrivileges.Privileges[0].Luid = luidDebugPrivilege;
            tokenPrivileges.Privileges[0].Attributes = SE_PRIVILEGE_REMOVED;

            if (!AdjustTokenPrivileges(tokenHandle, false, ref tokenPrivileges, 0, IntPtr.Zero, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        CloseHandle(tokenHandle);
    }

    /// <summary>
    /// Resume the suspended process.
    /// </summary>
    /// <param name="processInformation">Process information.</param>
    private static void ResumeProcess(PROCESS_INFORMATION processInformation)
    {
        if (ResumeThread(processInformation.hThread) == 0xFFFF_FFFF)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>
    /// Wait for the game window to be reeady.
    /// </summary>
    /// <param name="process">Target process.</param>
    private static void WaitForGamewindow(Process process)
    {
        try
        {
            do
            {
                process.WaitForInputIdle();
                Thread.Sleep(100);
            }
            while (TryFindGameWindow(process) == IntPtr.Zero);
        }
        catch (InvalidOperationException ex)
        {
            throw new GameExitedException(ex);
        }
    }

    /// <summary>
    /// Loop until a window with FFXIVGAME is present.
    /// </summary>
    /// <param name="process">Process to match.</param>
    /// <returns>A window handle.</returns>
    private static IntPtr TryFindGameWindow(Process process)
    {
        var hwnd = IntPtr.Zero;
        while ((hwnd = FindWindowEx(IntPtr.Zero, hwnd, "FFXIVGAME", IntPtr.Zero)) != IntPtr.Zero)
        {
            _ = GetWindowThreadProcessId(hwnd, out var pid);

            if (pid == process.Id && IsWindowVisible(hwnd))
            {
                break;
            }
        }

        return hwnd;
    }

    /// <summary>
    /// Update the process security info.
    /// </summary>
    /// <param name="processInformation">Target process security info.</param>
    private static void UpdateSecurityInfo(PROCESS_INFORMATION processInformation)
    {
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
            processInformation.hProcess,
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
}
