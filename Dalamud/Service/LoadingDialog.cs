using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Dalamud;

/// <summary>
/// Class providing an early-loading dialog.
/// </summary>
internal class LoadingDialog
{
    // TODO: We can't localize any of what's in here at the moment, because Localization is an EarlyLoadedService.
    
    private static int wasGloballyHidden = 0;
    
    private Thread? thread;
    private TaskDialogButton? inProgressHideButton;
    private TaskDialogPage? page;
    private bool canHide;
    private State currentState = State.LoadingDalamud;
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
        
        this.firstShowTime = DateTime.Now;
    }

    /// <summary>
    /// Hide the dialog.
    /// </summary>
    public void HideAndJoin()
    {
        if (this.thread == null || !this.thread.IsAlive)
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

        var context = string.Empty;
        if (this.currentState == State.LoadingPlugins)
        {
            context = "\nPreparing...";
            
            var tracker = Service<PluginManager>.GetNullable()?.StartupLoadTracking;
            if (tracker != null)
            {
                var nameString = tracker.GetPendingInternalNames()
                                        .Select(x => tracker.GetPublicName(x))
                                        .Where(x => x != null)
                                        .Aggregate(string.Empty, (acc, x) => acc + x + ", ");
                
                if (!nameString.IsNullOrEmpty())
                    context = $"\nWaiting for: {nameString[..^2]}";
            }
        }
        
        // Add some text if loading takes more than a few minutes
        if (DateTime.Now - this.firstShowTime > TimeSpan.FromMinutes(3))
            context += "\nIt's been a while now. Please report this issue on our Discord server.";

        this.page.Text = this.currentState switch
        {
            State.LoadingDalamud => "Please wait while Dalamud loads...",
            State.LoadingPlugins => "Please wait while Dalamud loads plugins...",
            State.AutoUpdatePlugins => "Please wait while Dalamud updates your plugins...",
            _ => throw new ArgumentOutOfRangeException(),
#pragma warning disable SA1513
        } + context;
#pragma warning restore SA1513

        this.inProgressHideButton!.Enabled = this.canHide;
    }
    
    private async Task DialogStatePeriodicUpdate(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (!token.IsCancellationRequested)
        {
            await timer.WaitForNextTickAsync(token);
            this.UpdatePage();
        }
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
            Expander = new TaskDialogExpander
            {
                CollapsedButtonText = "What does this mean?",
                ExpandedButtonText = "What does this mean?",
                Text = "Some of the plugins you have installed through Dalamud are taking a long time to load.\n" +
                       "This is likely normal, please wait a little while longer.",
            },
            SizeToContent = true,
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
            var hwnd = new HWND(taskDialog.Handle);

            // Bring to front
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

        var cts = new CancellationTokenSource();
        _ = this.DialogStatePeriodicUpdate(cts.Token);

        showDialogInternal!.Invoke(
            taskDialog,
            [IntPtr.Zero, this.page, TaskDialogStartupLocation.CenterScreen]);
        
        Interlocked.Exchange(ref wasGloballyHidden, 1);
        cts.Cancel();
    }
}
