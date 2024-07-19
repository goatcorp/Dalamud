using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

using TerraFX.Interop.Windows;

namespace Dalamud.Support;

/// <summary>Tracks the loaded process modules.</summary>
[ServiceManager.EarlyLoadedService]
internal sealed unsafe partial class CurrentProcessModules : IInternalDisposableService
{
    private static readonly ConcurrentQueue<string> LogQueue = new();
    private static readonly SemaphoreSlim LogSemaphore = new(0);

    private static Process? process;
    private static nint cookie;

    private readonly CancellationTokenSource logTaskStop = new();
    private readonly Task logTask;

    [ServiceManager.ServiceConstructor]
    private CurrentProcessModules()
    {
        var res = LdrRegisterDllNotification(0, &DllNotificationCallback, 0, out cookie);
        if (res != STATUS.STATUS_SUCCESS)
        {
            Log.Error("{what}: LdrRegisterDllNotification failure: 0x{err}", nameof(CurrentProcessModules), res);
            cookie = 0;
            this.logTask = Task.CompletedTask;
            return;
        }

        this.logTask = Task.Factory.StartNew(
            () =>
            {
                while (!this.logTaskStop.IsCancellationRequested)
                {
                    LogSemaphore.Wait();
                    while (LogQueue.TryDequeue(out var log))
                        Log.Verbose(log);
                }
            },
            this.logTaskStop.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private enum LdrDllNotificationReason : uint
    {
        Loaded = 1,
        Unloaded = 2,
    }

    /// <summary>Gets all the loaded modules, up to date.</summary>
    public static ProcessModuleCollection ModuleCollection
    {
        get
        {
            if (cookie == 0)
            {
                // This service has not been initialized; return a fresh copy without storing it.
                return Process.GetCurrentProcess().Modules;
            }

            if (process is null)
                Log.Verbose("{what}: Fetchling fresh copy of current process modules.", nameof(CurrentProcessModules));

            return (process ??= Process.GetCurrentProcess()).Modules;
        }
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        if (Interlocked.Exchange(ref cookie, 0) is var copy and not 0)
            LdrUnregisterDllNotification(copy);
        if (!this.logTask.IsCompleted)
        {
            this.logTaskStop.Cancel();
            LogSemaphore.Release();
            this.logTask.Wait();
        }
    }

    [UnmanagedCallersOnly]
    private static void DllNotificationCallback(
        LdrDllNotificationReason reason,
        LdrDllNotificationData* data,
        nint context) => process = null;

    /// <summary>
    /// Registers for notification when a DLL is first loaded.
    /// This notification occurs before dynamic linking takes place.<br /><br />
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/devnotes/ldrregisterdllnotification">Docs.</a>
    /// </summary>
    /// <param name="flags">This parameter must be zero.</param>
    /// <param name="notificationFunction">A pointer to a callback function to call when the DLL is loaded.</param>
    /// <param name="context">A pointer to context data for the callback function.</param>
    /// <param name="cookie">A pointer to a variable to receive an identifier for the callback function.
    /// This identifier is used to unregister the notification callback function.</param>
    /// <returns>Returns an NTSTATUS or error code.</returns>
    [LibraryImport("ntdll.dll", SetLastError = true)]
    private static partial int LdrRegisterDllNotification(
        uint flags,
        delegate* unmanaged<LdrDllNotificationReason, LdrDllNotificationData*, nint, void>
            notificationFunction,
        nint context,
        out nint cookie);

    /// <summary>
    /// Cancels DLL load notification previously registered by calling the LdrRegisterDllNotification function.<br />
    /// <br />
    /// <a href="https://learn.microsoft.com/en-us/windows/win32/devnotes/ldrunregisterdllnotification">Docs.</a>
    /// </summary>
    /// <param name="cookie">A pointer to the callback identifier received from the LdrRegisterDllNotification call
    /// that registered for notification.
    /// </param>
    /// <returns>Returns an NTSTATUS or error code.</returns>
    [LibraryImport("ntdll.dll", SetLastError = true)]
    private static partial int LdrUnregisterDllNotification(nint cookie);

    [StructLayout(LayoutKind.Sequential)]
    private struct LdrDllNotificationData
    {
        /// <summary>Reserved.</summary>
        public uint Flags;

        /// <summary>The full path name of the DLL module.</summary>
        public UNICODE_STRING* FullDllName;

        /// <summary>The base file name of the DLL module.</summary>
        public UNICODE_STRING* BaseDllName;

        /// <summary>A pointer to the base address for the DLL in memory.</summary>
        public nint DllBase;

        /// <summary>The size of the DLL image, in bytes.</summary>
        public uint SizeOfImage;
    }
}
