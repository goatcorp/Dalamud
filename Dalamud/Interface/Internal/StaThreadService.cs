using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Utility;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Internal;

/// <summary>Dedicated thread for OLE operations, and possibly more native thread-serialized operations.</summary>
[ServiceManager.EarlyLoadedService]
internal partial class StaThreadService : IInternalDisposableService
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Thread thread;
    private readonly ThreadBoundTaskScheduler taskScheduler;
    private readonly TaskFactory taskFactory;

    private readonly TaskCompletionSource<HWND> messageReceiverHwndTask =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    [ServiceManager.ServiceConstructor]
    private StaThreadService()
    {
        try
        {
            this.thread = new(this.OleThreadBody);
            this.thread.SetApartmentState(ApartmentState.STA);

            this.taskScheduler = new(this.thread);
            this.taskScheduler.TaskQueued += this.TaskSchedulerOnTaskQueued;
            this.taskFactory = new(
                this.cancellationTokenSource.Token,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                this.taskScheduler);

            this.thread.Start();
            this.messageReceiverHwndTask.Task.Wait();
        }
        catch (Exception e)
        {
            this.cancellationTokenSource.Cancel();
            this.messageReceiverHwndTask.SetException(e);
            throw;
        }
    }

    /// <summary>Gets all the available clipboard formats.</summary>
    public IReadOnlySet<uint> AvailableClipboardFormats { get; private set; } = ImmutableSortedSet<uint>.Empty;

    /// <summary>Places a pointer to a specific data object onto the clipboard. This makes the data object accessible
    /// to the <see cref="OleGetClipboard(IDataObject**)"/> function.</summary>
    /// <param name="pdo">Pointer to the <see cref="IDataObject"/> interface on the data object from which the data to
    /// be placed on the clipboard can be obtained. This parameter can be NULL; in which case the clipboard is emptied.
    /// </param>
    /// <returns>This function returns <see cref="S.S_OK"/> on success.</returns>
    [LibraryImport("ole32.dll")]
    public static unsafe partial int OleSetClipboard(IDataObject* pdo);

    /// <inheritdoc cref="OleSetClipboard(IDataObject*)"/>
    public static unsafe void OleSetClipboard(ComPtr<IDataObject> pdo) =>
        Marshal.ThrowExceptionForHR(OleSetClipboard(pdo.Get()));

    /// <summary>Retrieves a data object that you can use to access the contents of the clipboard.</summary>
    /// <param name="pdo">Address of <see cref="IDataObject"/> pointer variable that receives the interface pointer to
    /// the clipboard data object.</param>
    /// <returns>This function returns <see cref="S.S_OK"/> on success.</returns>
    [LibraryImport("ole32.dll")]
    public static unsafe partial int OleGetClipboard(IDataObject** pdo);

    /// <inheritdoc cref="OleGetClipboard(IDataObject**)"/>
    public static unsafe ComPtr<IDataObject> OleGetClipboard()
    {
        var pdo = default(ComPtr<IDataObject>);
        Marshal.ThrowExceptionForHR(OleGetClipboard(pdo.GetAddressOf()));
        return pdo;
    }

    /// <summary>Calls the appropriate method or function to release the specified storage medium.</summary>
    /// <param name="stgm">Address of <see cref="STGMEDIUM"/> to release.</param>
    [LibraryImport("ole32.dll")]
    public static unsafe partial void ReleaseStgMedium(STGMEDIUM* stgm);

    /// <inheritdoc cref="ReleaseStgMedium(STGMEDIUM*)"/>
    public static unsafe void ReleaseStgMedium(ref STGMEDIUM stgm)
    {
        fixed (STGMEDIUM* pstgm = &stgm)
            ReleaseStgMedium(pstgm);
    }

    /// <inheritdoc/>
    void IInternalDisposableService.DisposeService()
    {
        this.cancellationTokenSource.Cancel();
        if (this.messageReceiverHwndTask.Task.IsCompletedSuccessfully)
            SendMessageW(this.messageReceiverHwndTask.Task.Result, WM.WM_CLOSE, 0, 0);

        this.thread.Join();
    }

    /// <summary>Runs a given delegate in the messaging thread.</summary>
    /// <param name="action">Delegate to run.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task"/> representating the state of the operation.</returns>
    public async Task Run(Action action, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.cancellationTokenSource.Token,
            cancellationToken);
        await this.taskFactory.StartNew(action, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Runs a given delegate in the messaging thread.</summary>
    /// <typeparam name="T">Type of the return value.</typeparam>
    /// <param name="func">Delegate to run.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task{T}"/> representating the state of the operation.</returns>
    public async Task<T> Run<T>(Func<T> func, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.cancellationTokenSource.Token,
            cancellationToken);
        return await this.taskFactory.StartNew(func, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Runs a given delegate in the messaging thread.</summary>
    /// <param name="func">Delegate to run.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task{T}"/> representating the state of the operation.</returns>
    public async Task Run(Func<Task> func, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.cancellationTokenSource.Token,
            cancellationToken);
        await await this.taskFactory.StartNew(func, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Runs a given delegate in the messaging thread.</summary>
    /// <typeparam name="T">Type of the return value.</typeparam>
    /// <param name="func">Delegate to run.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Task{T}"/> representating the state of the operation.</returns>
    public async Task<T> Run<T>(Func<Task<T>> func, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            this.cancellationTokenSource.Token,
            cancellationToken);
        return await await this.taskFactory.StartNew(func, cancellationToken).ConfigureAwait(true);
    }

    [LibraryImport("ole32.dll")]
    private static partial int OleInitialize(nint reserved);

    [LibraryImport("ole32.dll")]
    private static partial void OleUninitialize();

    [LibraryImport("ole32.dll")]
    private static partial int OleFlushClipboard();

    private void TaskSchedulerOnTaskQueued() =>
        PostMessageW(this.messageReceiverHwndTask.Task.Result, WM.WM_NULL, 0, 0);

    private void UpdateAvailableClipboardFormats(HWND hWnd)
    {
        if (!OpenClipboard(hWnd))
        {
            this.AvailableClipboardFormats = ImmutableSortedSet<uint>.Empty;
            return;
        }

        var formats = new SortedSet<uint>();
        for (var cf = EnumClipboardFormats(0); cf != 0; cf = EnumClipboardFormats(cf))
            formats.Add(cf);
        this.AvailableClipboardFormats = formats;
        CloseClipboard();
    }

    private LRESULT MessageReceiverWndProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
    {
        this.taskScheduler.Run();

        switch (uMsg)
        {
            case WM.WM_CLIPBOARDUPDATE:
                this.UpdateAvailableClipboardFormats(hWnd);
                break;

            case WM.WM_DESTROY:
                PostQuitMessage(0);
                return 0;
        }

        return DefWindowProcW(hWnd, uMsg, wParam, lParam);
    }

    private unsafe void OleThreadBody()
    {
        var hInstance = (HINSTANCE)Marshal.GetHINSTANCE(typeof(StaThreadService).Module);
        ushort wndClassAtom = 0;
        var gch = GCHandle.Alloc(this);
        try
        {
            ((HRESULT)OleInitialize(0)).ThrowOnError();

            fixed (char* name = typeof(StaThreadService).FullName!)
            {
                var wndClass = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    lpfnWndProc = &MessageReceiverWndProcStatic,
                    hInstance = hInstance,
                    hbrBackground = (HBRUSH)(COLOR.COLOR_BACKGROUND + 1),
                    lpszClassName = (ushort*)name,
                };

                wndClassAtom = RegisterClassExW(&wndClass);
                if (wndClassAtom == 0)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                this.messageReceiverHwndTask.SetResult(
                    CreateWindowExW(
                        0,
                        (ushort*)wndClassAtom,
                        (ushort*)name,
                        0,
                        CW_USEDEFAULT,
                        CW_USEDEFAULT,
                        CW_USEDEFAULT,
                        CW_USEDEFAULT,
                        default,
                        default,
                        hInstance,
                        (void*)GCHandle.ToIntPtr(gch)));

                [UnmanagedCallersOnly]
                static LRESULT MessageReceiverWndProcStatic(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
                {
                    nint gchn;
                    if (uMsg == WM.WM_NCCREATE)
                    {
                        gchn = (nint)((CREATESTRUCTW*)lParam)->lpCreateParams;
                        SetWindowLongPtrW(hWnd, GWLP.GWLP_USERDATA, gchn);
                    }
                    else
                    {
                        gchn = GetWindowLongPtrW(hWnd, GWLP.GWLP_USERDATA);
                    }

                    if (gchn == 0)
                        return DefWindowProcW(hWnd, uMsg, wParam, lParam);

                    return ((StaThreadService)GCHandle.FromIntPtr(gchn).Target!)
                        .MessageReceiverWndProc(hWnd, uMsg, wParam, lParam);
                }
            }

            AddClipboardFormatListener(this.messageReceiverHwndTask.Task.Result);
            this.UpdateAvailableClipboardFormats(this.messageReceiverHwndTask.Task.Result);

            for (MSG msg; GetMessageW(&msg, default, 0, 0);)
            {
                TranslateMessage(&msg);
                DispatchMessageW(&msg);
            }
        }
        catch (Exception e)
        {
            gch.Free();
            _ = OleFlushClipboard();
            OleUninitialize();
            if (wndClassAtom != 0)
                UnregisterClassW((ushort*)wndClassAtom, hInstance);
            this.messageReceiverHwndTask.TrySetException(e);
        }
    }
}
