using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Plugin.Internal;
using Dalamud.Utility;

using Serilog;
using Serilog.Events;

using TerraFX.Interop.Windows;

using static TerraFX.Interop.Windows.TASKDIALOG_FLAGS;
using static TerraFX.Interop.Windows.Windows;

namespace Dalamud;

/// <summary>
/// Class providing an early-loading dialog.
/// </summary>
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
internal sealed class LoadingDialog
{
    private readonly RollingList<string> logs = new(20);
    private readonly TaskCompletionSource<HWND> hwndTaskDialog = new();

    private Thread? thread;
    private DateTime firstShowTime;

    /// <summary>
    /// Enum representing the state of the dialog.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// Show a message stating that Dalamud is currently loading.
        /// </summary>
        LoadingDalamud,

        /// <summary>
        /// Show a message stating that Dalamud is currently loading plugins.
        /// </summary>
        LoadingPlugins,

        /// <summary>
        /// Show a message stating that Dalamud is currently updating plugins.
        /// </summary>
        AutoUpdatePlugins,
    }

    /// <summary>Gets the queue where log entries that are not processed yet are stored.</summary>
    public static ConcurrentQueue<(string Line, LogEvent LogEvent)> NewLogEntries { get; } = new();

    /// <summary>Gets a value indicating whether the initial Dalamud loading dialog will not show again until next
    /// game restart.</summary>
    public static bool IsGloballyHidden { get; private set; }

    /// <summary>
    /// Gets or sets the current state of the dialog.
    /// </summary>
    public State CurrentState { get; set; } = State.LoadingDalamud;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog can be hidden by the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if called before the dialog has been created.</exception>
    public bool CanHide { get; set; }

    /// <summary>
    /// Show the dialog.
    /// </summary>
    public void Show()
    {
        if (IsGloballyHidden)
            return;

        if (this.thread is not null)
            return;

        this.thread = new Thread(this.ThreadStart)
        {
            Name = "Dalamud Loading Dialog",
        };
        this.thread.SetApartmentState(ApartmentState.STA);
        this.thread.Start();

        this.firstShowTime = DateTime.Now;
    }

    /// <summary>
    /// Hide the dialog.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task HideAndJoin()
    {
        IsGloballyHidden = true;
        if (this.hwndTaskDialog.TrySetCanceled() || this.hwndTaskDialog.Task.IsCanceled)
            return;

        try
        {
            SendMessageW(await this.hwndTaskDialog.Task, WM.WM_CLOSE, default, default);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }

        this.thread?.Join();
    }

    private unsafe void UpdateMainInstructionText(HWND hwnd)
    {
        fixed (void* pszText = this.CurrentState switch
               {
                   State.LoadingDalamud => Loc.Localize(
                       "LoadingDialogMainInstructionLoadingDalamud",
                       "Dalamud is loading..."),
                   State.LoadingPlugins => Loc.Localize(
                       "LoadingDialogMainInstructionLoadingPlugins",
                       "Waiting for plugins to load..."),
                   State.AutoUpdatePlugins => Loc.Localize(
                       "LoadingDialogMainInstructionAutoUpdatePlugins",
                       "Updating plugins..."),
                   _ => string.Empty, // should not happen
               })
        {
            SendMessageW(
                hwnd,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_MAIN_INSTRUCTION,
                (LPARAM)pszText);
        }
    }

    private unsafe void UpdateContentText(HWND hwnd)
    {
        var contentBuilder = new StringBuilder(
            Loc.Localize(
                "LoadingDialogContentInfo",
                "Some of the plugins you have installed through Dalamud are taking a long time to load.\n" +
                "This is likely normal, please wait a little while longer."));

        if (this.CurrentState == State.LoadingPlugins)
        {
            var tracker = Service<PluginManager>.GetNullable()?.StartupLoadTracking;
            if (tracker != null)
            {
                var nameString = string.Join(
                    ", ",
                    tracker.GetPendingInternalNames()
                           .Select(x => tracker.GetPublicName(x))
                           .Where(x => x != null));

                if (!nameString.IsNullOrEmpty())
                {
                    contentBuilder
                        .AppendLine()
                        .AppendLine()
                        .Append(
                            string.Format(
                                Loc.Localize("LoadingDialogContentCurrentPlugin", "Waiting for: {0}"),
                                nameString));
                }
            }
        }

        // Add some text if loading takes more than a few minutes
        if (DateTime.Now - this.firstShowTime > TimeSpan.FromMinutes(3))
        {
            contentBuilder
                .AppendLine()
                .AppendLine()
                .Append(
                    Loc.Localize(
                        "LoadingDialogContentTakingTooLong",
                        "It's been a while now. Please report this issue on our Discord server."));
        }

        fixed (void* pszText = contentBuilder.ToString())
        {
            SendMessageW(
                hwnd,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_CONTENT,
                (LPARAM)pszText);
        }
    }

    private unsafe void UpdateExpandedInformation(HWND hwnd)
    {
        const int maxCharactersPerLine = 80;

        if (NewLogEntries.IsEmpty)
            return;
        
        var sb = new StringBuilder();
        while (NewLogEntries.TryDequeue(out var e))
        {
            var t = e.Line.AsSpan();
            var first = true;
            while (!t.IsEmpty)
            {
                var i = t.IndexOfAny('\r', '\n');
                var line = i == -1 ? t : t[..i];
                t = i == -1 ? ReadOnlySpan<char>.Empty : t[(i + 1)..];
                if (line.IsEmpty)
                    continue;
                
                sb.Clear();
                if (first)
                    sb.Append($"{e.LogEvent.Timestamp:HH:mm:ss} | ");
                else
                    sb.Append("         | ");
                first = false;
                if (line.Length < maxCharactersPerLine)
                    sb.Append(line);
                else
                    sb.Append($"{line[..(maxCharactersPerLine - 3)]}...");
                this.logs.Add(sb.ToString());
            }
        }

        sb.Clear();
        foreach (var l in this.logs)
            sb.AppendLine(l);

        fixed (void* pszText = sb.ToString())
        {
            SendMessageW(
                hwnd,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_EXPANDED_INFORMATION,
                (LPARAM)pszText);
        }
    }

    private void UpdateButtonEnabled(HWND hwnd) =>
        SendMessageW(hwnd, (uint)TASKDIALOG_MESSAGES.TDM_ENABLE_BUTTON, IDOK, this.CanHide ? 1 : 0);

    private HRESULT TaskDialogCallback(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch ((TASKDIALOG_NOTIFICATIONS)msg)
        {
            case TASKDIALOG_NOTIFICATIONS.TDN_CREATED:
                if (!this.hwndTaskDialog.TrySetResult(hwnd))
                    return E.E_FAIL;

                this.UpdateMainInstructionText(hwnd);
                this.UpdateContentText(hwnd);
                this.UpdateExpandedInformation(hwnd);
                this.UpdateButtonEnabled(hwnd);
                SendMessageW(hwnd, (int)TASKDIALOG_MESSAGES.TDM_SET_PROGRESS_BAR_MARQUEE, 1, 0);

                // Bring to front
                ShowWindow(hwnd, SW.SW_SHOW);
                SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP.SWP_NOSIZE | SWP.SWP_NOMOVE);
                SetWindowPos(hwnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0, SWP.SWP_NOSIZE | SWP.SWP_NOMOVE);
                SetForegroundWindow(hwnd);
                SetFocus(hwnd);
                SetActiveWindow(hwnd);
                return S.S_OK;

            case TASKDIALOG_NOTIFICATIONS.TDN_TIMER:
                this.UpdateMainInstructionText(hwnd);
                this.UpdateContentText(hwnd);
                this.UpdateExpandedInformation(hwnd);
                this.UpdateButtonEnabled(hwnd);
                return S.S_OK;
        }

        return S.S_OK;
    }

    private unsafe void ThreadStart()
    {
        // We don't have access to the asset service here.
        var workingDirectory = Service<Dalamud>.Get().StartInfo.WorkingDirectory;
        using var extractedIcon =
            string.IsNullOrEmpty(workingDirectory)
                ? null
                : Icon.ExtractAssociatedIcon(Path.Combine(workingDirectory, "Dalamud.Injector.exe"));

        fixed (char* pszEmpty = "-")
        fixed (char* pszWindowTitle = "Dalamud")
        fixed (char* pszDalamudBoot = "Dalamud.Boot.dll")
        fixed (char* pszThemesManifestResourceName = "RT_MANIFEST_THEMES")
        fixed (char* pszHide = Loc.Localize("LoadingDialogHide", "Hide"))
        fixed (char* pszShowLatestLogs = Loc.Localize("LoadingDialogShowLatestLogs", "Show Latest Logs"))
        fixed (char* pszHideLatestLogs = Loc.Localize("LoadingDialogHideLatestLogs", "Hide Latest Logs"))
        {
            var taskDialogButton = new TASKDIALOG_BUTTON
            {
                nButtonID = IDOK,
                pszButtonText = pszHide,
            };
            var taskDialogConfig = new TASKDIALOGCONFIG
            {
                cbSize = (uint)sizeof(TASKDIALOGCONFIG),
                hwndParent = default,
                hInstance = (HINSTANCE)Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().ManifestModule),
                dwFlags = (int)TDF_CAN_BE_MINIMIZED |
                          (int)TDF_SHOW_MARQUEE_PROGRESS_BAR |
                          (int)TDF_EXPAND_FOOTER_AREA |
                          (int)TDF_CALLBACK_TIMER |
                          (extractedIcon is null ? 0 : (int)TDF_USE_HICON_MAIN),
                dwCommonButtons = 0,
                pszWindowTitle = pszWindowTitle,
                pszMainIcon = extractedIcon is null ? TD.TD_INFORMATION_ICON : (char*)extractedIcon.Handle,
                pszMainInstruction = null,
                pszContent = null,
                cButtons = 1,
                pButtons = &taskDialogButton,
                nDefaultButton = IDOK,
                cRadioButtons = 0,
                pRadioButtons = null,
                nDefaultRadioButton = 0,
                pszVerificationText = null,
                pszExpandedInformation = pszEmpty,
                pszExpandedControlText = pszShowLatestLogs,
                pszCollapsedControlText = pszHideLatestLogs,
                pszFooterIcon = null,
                pszFooter = null,
                pfCallback = &HResultFuncBinder,
                lpCallbackData = 0,
                cxWidth = 360,
            };

            HANDLE hActCtx = default;
            GCHandle gch = default;
            nuint cookie = 0;
            try
            {
                var actctx = new ACTCTXW
                {
                    cbSize = (uint)sizeof(ACTCTXW),
                    dwFlags = ACTCTX_FLAG_HMODULE_VALID | ACTCTX_FLAG_RESOURCE_NAME_VALID,
                    lpResourceName = pszThemesManifestResourceName,
                    hModule = GetModuleHandleW(pszDalamudBoot),
                };
                hActCtx = CreateActCtxW(&actctx);
                if (hActCtx == default)
                    throw new Win32Exception("CreateActCtxW failure.");

                if (!ActivateActCtx(hActCtx, &cookie))
                    throw new Win32Exception("ActivateActCtx failure.");

                gch = GCHandle.Alloc((Func<HWND, uint, WPARAM, LPARAM, HRESULT>)this.TaskDialogCallback);
                taskDialogConfig.lpCallbackData = GCHandle.ToIntPtr(gch);
                TaskDialogIndirect(&taskDialogConfig, null, null, null);
            }
            catch (Exception e)
            {
                Log.Error(e, "TaskDialogIndirect failure.");
            }
            finally
            {
                if (gch.IsAllocated)
                    gch.Free();
                if (cookie != 0)
                    DeactivateActCtx(0, cookie);
                ReleaseActCtx(hActCtx);
            }
        }

        IsGloballyHidden = true;

        return;

        [UnmanagedCallersOnly]
        static HRESULT HResultFuncBinder(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam, nint user) =>
            ((Func<HWND, uint, WPARAM, LPARAM, HRESULT>)GCHandle.FromIntPtr(user).Target!)
            .Invoke(hwnd, msg, wParam, lParam);
    }
}
