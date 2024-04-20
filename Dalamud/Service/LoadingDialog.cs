using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using Dalamud.Utility;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Dalamud;

/// <summary>
/// Class providing an early-loading dialog.
/// </summary>
internal class LoadingDialog
{
    private static int wasGloballyHidden = 0;
    
    private Thread? thread;
    private TaskDialogButton? inProgressHideButton;
    private TaskDialogPage? page;
    private bool canHide;
    private State currentState = State.LoadingDalamud;
    
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
    
    /// <summary>
    /// Gets or sets the current state of the dialog.
    /// </summary>
    public State CurrentState
    {
        get => this.currentState;
        set
        {
            this.currentState = value;
            this.UpdatePage();
        }
    }
    
    /// <summary>
    /// Gets or sets a value indicating whether or not the dialog can be hidden by the user.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if called before the dialog has been created.</exception>
    public bool CanHide
    {
        get => this.canHide;
        set
        {
            this.canHide = value;
            this.UpdatePage();
        }
    }

    /// <summary>
    /// Show the dialog.
    /// </summary>
    public void Show()
    {
        if (Volatile.Read(ref wasGloballyHidden) == 1)
            return;
        
        if (this.thread?.IsAlive == true)
            return;
        
        this.thread = new Thread(this.ThreadStart)
        {
            Name = "Dalamud Loading Dialog",
        };
        this.thread.SetApartmentState(ApartmentState.STA);
        this.thread.Start();
    }

    /// <summary>
    /// Hide the dialog.
    /// </summary>
    public void HideAndJoin()
    {
        if (this.thread == null || this.thread.IsAlive == false)
            return;
        
        this.inProgressHideButton?.PerformClick();
        this.thread!.Join();
    }

    private void UpdatePage()
    {
        if (this.page == null)
            return;

        this.page.Heading = this.currentState switch
        {
            State.LoadingDalamud => "Dalamud is loading...",
            State.LoadingPlugins => "Waiting for plugins to load...",
            State.AutoUpdatePlugins => "Updating plugins...",
            _ => throw new ArgumentOutOfRangeException(),
        };

        this.page.Text = this.currentState switch
        {
            State.LoadingDalamud => "Please wait while Dalamud loads...",
            State.LoadingPlugins => "Please wait while Dalamud loads plugins...",
            State.AutoUpdatePlugins => "Please wait while Dalamud updates your plugins...",
            _ => throw new ArgumentOutOfRangeException(),
        };

        this.inProgressHideButton!.Enabled = this.canHide;
    }

    private void ThreadStart()
    {
        Application.EnableVisualStyles();

        this.inProgressHideButton = new TaskDialogButton("Hide", this.canHide);

        // We don't have access to the asset service here.
        var workingDirectory = Service<Dalamud>.Get().StartInfo.WorkingDirectory;
        TaskDialogIcon? dialogIcon = null;
        if (!workingDirectory.IsNullOrEmpty())
        {
            var extractedIcon = Icon.ExtractAssociatedIcon(Path.Combine(workingDirectory, "Dalamud.Injector.exe"));
            if (extractedIcon != null)
            {
                dialogIcon = new TaskDialogIcon(extractedIcon);
            }
        }

        dialogIcon ??= TaskDialogIcon.Information;
        this.page = new TaskDialogPage
        {
            ProgressBar = new TaskDialogProgressBar(TaskDialogProgressBarState.Marquee),
            Caption = "Dalamud",
            Icon = dialogIcon,
            Buttons = { this.inProgressHideButton },
            AllowMinimize = false,
            AllowCancel = false,
        };
        
        this.UpdatePage();

        // Call private TaskDialog ctor
        var ctor = typeof(TaskDialog).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            Array.Empty<Type>(),
            null);

        var taskDialog = (TaskDialog)ctor!.Invoke(Array.Empty<object>())!;

        this.page.Created += (_, _) =>
        {
            // Bring to front
            var hwnd = new HWND(taskDialog.Handle);
            Windows.Win32.PInvoke.SetWindowPos(hwnd, HWND.HWND_TOPMOST, 0, 0, 0, 0, 
                                               SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
            Windows.Win32.PInvoke.SetWindowPos(hwnd, HWND.HWND_NOTOPMOST, 0, 0, 0, 0, 
                                               SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                                               SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
            Windows.Win32.PInvoke.SetForegroundWindow(hwnd);
            Windows.Win32.PInvoke.SetFocus(hwnd);
            Windows.Win32.PInvoke.SetActiveWindow(hwnd);
        };

        // Call private "ShowDialogInternal"
        var showDialogInternal = typeof(TaskDialog).GetMethod(
            "ShowDialogInternal",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(IntPtr), typeof(TaskDialogPage), typeof(TaskDialogStartupLocation)],
            null);

        showDialogInternal!.Invoke(
            taskDialog,
            [IntPtr.Zero, this.page, TaskDialogStartupLocation.CenterScreen]);
        
        Interlocked.Exchange(ref wasGloballyHidden, 1);
    }
}
