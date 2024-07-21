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
internal sealed unsafe class LoadingDialog
{
    private readonly RollingList<string> logs = new(20);

    private Thread? thread;
    private HWND hwndTaskDialog;
    private DateTime firstShowTime;
    private State currentState = State.LoadingDalamud;
    private bool canHide;

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
    public State CurrentState
    {
        get => this.currentState;
        set
        {
            if (this.currentState == value)
                return;

            this.currentState = value;
            this.UpdateMainInstructionText();
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog can be hidden by the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if called before the dialog has been created.</exception>
    public bool CanHide
    {
        get => this.canHide;
        set
        {
            if (this.canHide == value)
                return;

            this.canHide = value;
            this.UpdateButtonEnabled();
        }
    }

    /// <summary>
    /// Show the dialog.
    /// </summary>
    public void Show()
    {
        if (IsGloballyHidden)
            return;

        if (this.thread?.IsAlive == true)
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
    public void HideAndJoin()
    {
        IsGloballyHidden = true;
        if (this.thread?.IsAlive is not true)
            return;

        SendMessageW(this.hwndTaskDialog, WM.WM_CLOSE, default, default);
        this.thread.Join();
    }

    private void UpdateMainInstructionText()
    {
        if (this.hwndTaskDialog == default)
            return;

        fixed (void* pszText = this.currentState switch
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
                this.hwndTaskDialog,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_MAIN_INSTRUCTION,
                (LPARAM)pszText);
        }
    }

    private void UpdateContentText()
    {
        if (this.hwndTaskDialog == default)
            return;

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
                this.hwndTaskDialog,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_CONTENT,
                (LPARAM)pszText);
        }
    }

    private void UpdateExpandedInformation()
    {
        const int maxCharactersPerLine = 80;

        if (NewLogEntries.IsEmpty)
            return;
        while (NewLogEntries.TryDequeue(out var e))
        {
            var t = e.Line.AsSpan();
            while (!t.IsEmpty)
            {
                var i = t.IndexOfAny('\r', '\n');
                var line = i == -1 ? t : t[..i];
                t = i == -1 ? ReadOnlySpan<char>.Empty : t[(i + 1)..];
                if (line.IsEmpty)
                    continue;

                this.logs.Add(
                    line.Length < maxCharactersPerLine ? line.ToString() : $"{line[..(maxCharactersPerLine - 3)]}...");
            }
        }

        var sb = new StringBuilder();
        foreach (var l in this.logs)
            sb.AppendLine(l);

        fixed (void* pszText = sb.ToString())
        {
            SendMessageW(
                this.hwndTaskDialog,
                (uint)TASKDIALOG_MESSAGES.TDM_SET_ELEMENT_TEXT,
                (WPARAM)(int)TASKDIALOG_ELEMENTS.TDE_EXPANDED_INFORMATION,
                (LPARAM)pszText);
        }
    }

    private void UpdateButtonEnabled()
    {
        if (this.hwndTaskDialog == default)
            return;

        SendMessageW(this.hwndTaskDialog, (uint)TASKDIALOG_MESSAGES.TDM_ENABLE_BUTTON, IDOK, this.canHide ? 1 : 0);
    }

    private HRESULT TaskDialogCallback(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch ((TASKDIALOG_NOTIFICATIONS)msg)
        {
            case TASKDIALOG_NOTIFICATIONS.TDN_CREATED:
                this.hwndTaskDialog = hwnd;

                this.UpdateMainInstructionText();
                this.UpdateContentText();
                this.UpdateExpandedInformation();
                this.UpdateButtonEnabled();
                SendMessageW(hwnd, (int)TASKDIALOG_MESSAGES.TDM_SET_PROGRESS_BAR_MARQUEE, 1, 0);

                // Bring to front
                SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, SWP.SWP_NOSIZE | SWP.SWP_NOMOVE);
                SetWindowPos(hwnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0, SWP.SWP_NOSIZE | SWP.SWP_NOMOVE);
                ShowWindow(hwnd, SW.SW_SHOW);
                SetForegroundWindow(hwnd);
                SetFocus(hwnd);
                SetActiveWindow(hwnd);
                return S.S_OK;

            case TASKDIALOG_NOTIFICATIONS.TDN_DESTROYED:
                this.hwndTaskDialog = default;
                return S.S_OK;

            case TASKDIALOG_NOTIFICATIONS.TDN_TIMER:
                this.UpdateContentText();
                this.UpdateExpandedInformation();
                return S.S_OK;
        }

        return S.S_OK;
    }

    private void ThreadStart()
    {
        // We don't have access to the asset service here.
        var workingDirectory = Service<Dalamud>.Get().StartInfo.WorkingDirectory;
        using var extractedIcon =
            string.IsNullOrEmpty(workingDirectory)
                ? null
                : Icon.ExtractAssociatedIcon(Path.Combine(workingDirectory, "Dalamud.Injector.exe"));

        fixed (void* pszEmpty = "-")
        fixed (void* pszWindowTitle = "Dalamud")
        fixed (void* pszDalamudBoot = "Dalamud.Boot.dll")
        fixed (void* pszThemesManifestResourceName = "RT_MANIFEST_THEMES")
        fixed (void* pszHide = Loc.Localize("LoadingDialogHide", "Hide"))
        fixed (void* pszShowLatestLogs = Loc.Localize("LoadingDialogShowLatestLogs", "Show Latest Logs"))
        fixed (void* pszHideLatestLogs = Loc.Localize("LoadingDialogHideLatestLogs", "Hide Latest Logs"))
        {
            var taskDialogButton = new TASKDIALOG_BUTTON
            {
                nButtonID = IDOK,
                pszButtonText = (ushort*)pszHide,
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
                pszWindowTitle = (ushort*)pszWindowTitle,
                pszMainIcon = extractedIcon is null ? TD.TD_INFORMATION_ICON : (ushort*)extractedIcon.Handle,
                pszMainInstruction = null,
                pszContent = null,
                cButtons = 1,
                pButtons = &taskDialogButton,
                nDefaultButton = IDOK,
                cRadioButtons = 0,
                pRadioButtons = null,
                nDefaultRadioButton = 0,
                pszVerificationText = null,
                pszExpandedInformation = (ushort*)pszEmpty,
                pszExpandedControlText = (ushort*)pszShowLatestLogs,
                pszCollapsedControlText = (ushort*)pszHideLatestLogs,
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
                    lpResourceName = (ushort*)pszThemesManifestResourceName,
                    hModule = GetModuleHandleW((ushort*)pszDalamudBoot),
                };
                hActCtx = CreateActCtxW(&actctx);
                if (hActCtx == default)
                    throw new Win32Exception("CreateActCtxW failure.");

                if (!ActivateActCtx(hActCtx, &cookie))
                    throw new Win32Exception("ActivateActCtx failure.");

                gch = GCHandle.Alloc((Func<HWND, uint, WPARAM, LPARAM, HRESULT>)this.TaskDialogCallback);
                taskDialogConfig.lpCallbackData = GCHandle.ToIntPtr(gch);
                TaskDialogIndirect(&taskDialogConfig, null, null, null).ThrowOnError();
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
