using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Dalamud.Configuration.Internal;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Asserts;

/// <summary>
/// Class responsible for registering and handling ImGui asserts.
/// </summary>
internal class AssertHandler : IDisposable
{
    private readonly HashSet<string> ignoredAsserts = [];
    private readonly HashSet<string> seenAsserts = [];

    // Store callback to avoid it from being GC'd
    private readonly AssertCallbackDelegate callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertHandler"/> class.
    /// </summary>
    public unsafe AssertHandler()
    {
        this.callback = this.OnImGuiAssert;
    }

    private unsafe delegate void AssertCallbackDelegate(
        void* expr,
        void* file,
        int line);

    /// <summary>
    /// Gets or sets a value indicating whether ImGui asserts should be shown to the user.
    /// </summary>
    public bool ShowAsserts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether we want to hide asserts that occur frequently (= every update)
    /// and whether we want to log callstacks.
    /// </summary>
    public bool EnableVerboseLogging { get; set; }

    /// <summary>
    /// Register the cimgui assert handler with the native library.
    /// </summary>
    public unsafe void Setup()
    {
        var cb = Marshal.GetFunctionPointerForDelegate(this.callback).ToPointer();
        CustomNativeFunctions.igCustom_SetAssertCallback(cb);
        CustomNativeFunctionsPlot.igCustom_SetAssertCallback(cb);
        CustomNativeFunctionsGuizmo.igCustom_SetAssertCallback(cb);
    }

    /// <summary>
    /// Unregister the cimgui assert handler with the native library.
    /// </summary>
    public unsafe void Shutdown()
    {
        CustomNativeFunctions.igCustom_SetAssertCallback(null);
        CustomNativeFunctionsPlot.igCustom_SetAssertCallback(null);
        CustomNativeFunctionsGuizmo.igCustom_SetAssertCallback(null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Shutdown();
    }

    private static string? ExtractImguiFunction(StackTrace stackTrace)
    {
        var frame = stackTrace.GetFrames()
                              .FirstOrDefault(f => f.GetMethod()?.DeclaringType?.Namespace == "Dalamud.Bindings.ImGui");
        if (frame == null)
            return null;

        var method = frame.GetMethod();
        if (method == null)
            return null;

        return $"{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.Name))})";
    }

    private static StackTrace GenerateStackTrace()
    {
        var trace = DiagnosticUtil.GetUsefulTrace(new StackTrace(true));
        var frames = trace.GetFrames().ToList();

        // Remove everything that happens in the assert context.
        var lastAssertIdx = frames.FindLastIndex(f => f.GetMethod()?.DeclaringType == typeof(AssertHandler));
        if (lastAssertIdx >= 0)
        {
            frames.RemoveRange(0, lastAssertIdx + 1);
        }

        var firstInterfaceManagerIdx = frames.FindIndex(f => f.GetMethod()?.DeclaringType == typeof(InterfaceManager));
        if (firstInterfaceManagerIdx >= 0)
        {
            frames.RemoveRange(firstInterfaceManagerIdx, frames.Count - firstInterfaceManagerIdx);
        }

        return new StackTrace(frames);
    }

    private unsafe void OnImGuiAssert(void* pExpr, void* pFile, int line)
    {
        // Only show in dev mode for now
        if (!Service<DalamudConfiguration>.Get().DevMode.GetValueOrDefault(false))
            return;

        var expr = Marshal.PtrToStringAnsi(new IntPtr(pExpr));
        var file = Marshal.PtrToStringAnsi(new IntPtr(pFile));
        if (expr == null || file == null)
        {
            Log.Warning(
                "ImGui assertion failed: {Expr} at {File}:{Line} (failed to parse)",
                expr,
                file,
                line);
            return;
        }

        var key = $"{file}:{line}";
        if (this.ignoredAsserts.Contains(key))
            return;

        Lazy<StackTrace> stackTrace = new(GenerateStackTrace);

        if (!this.EnableVerboseLogging)
        {
            if (this.seenAsserts.Add(key))
            {
                WarnForPlugin();

                Log.Warning(
                    "ImGui assertion failed: {Expr} at {File}:{Line}\nFurther occurrences will be hidden. (/xldev -> GUI -> Enable verbose assert logging)\n{StackTrace:l}",
                    expr,
                    file,
                    line,
                    stackTrace);
            }
        }
        else
        {
            if (this.seenAsserts.Add(key))
            {
                WarnForPlugin();
            }

            Log.Warning(
                "ImGui assertion failed: {Expr} at {File}:{Line}\n{StackTrace:l}",
                expr,
                file,
                line,
                stackTrace.Value.ToString().TrimEnd());
        }

        if (!this.ShowAsserts)
            return;

        string? GetRepoUrl()
        {
            // TODO: implot, imguizmo?
            const string userName = "goatcorp";
            const string repoName = "gc-imgui";
            const string branch = "1.88-enhanced-abifix";

            if (!file.Contains("imgui", StringComparison.OrdinalIgnoreCase))
                return null;

            var lastSlash = file.LastIndexOf('\\');
            var fileName = file[(lastSlash + 1)..];
            return $"https://github.com/{userName}/{repoName}/blob/{branch}/{fileName}#L{line}";
        }

        // grab the stack trace now that we've decided to show UI.
        var responsiblePlugin = Service<PluginManager>.GetNullable()?.FindCallingPlugin(stackTrace.Value);
        var responsibleMethodCall = ExtractImguiFunction(stackTrace.Value);

        var gitHubUrl = GetRepoUrl();
        var showOnGitHubButton = new TaskDialogButton
        {
            Text = "Open GitHub",
            AllowCloseDialog = false,
            Enabled = !gitHubUrl.IsNullOrEmpty(),
        };
        showOnGitHubButton.Click += (_, _) =>
        {
            if (!gitHubUrl.IsNullOrEmpty())
                Util.OpenLink(gitHubUrl);
        };

        var breakButton = new TaskDialogButton
        {
            Text = "Break",
            AllowCloseDialog = true,
        };

        var disableButton = new TaskDialogButton
        {
            Text = "Disable for this session",
            AllowCloseDialog = true,
        };

        var ignoreButton = TaskDialogButton.Ignore;

        TaskDialogButton? result = null;

        void DialogThreadStart()
        {
            // TODO(goat): This is probably not gonna work if we showed the loading dialog
            // this session since it already loaded visual styles...
            Application.EnableVisualStyles();

            string text;
            if (responsiblePlugin != null)
            {
                text = $"The plugin \"{responsiblePlugin.Name}\" appears to have caused an ImGui assertion failure. " +
                       $"Please report this problem to the plugin's developer.\n\n";
            }
            else
            {
                text = "Some code in a plugin or Dalamud itself has caused an ImGui assertion failure. " +
                       "Please report this problem in the Dalamud discord.\n\n";
            }

            text += $"You may attempt to continue running the game, but Dalamud UI elements may not work " +
                    $"correctly, or the game may crash after resuming.\n\n";

            if (responsibleMethodCall != null)
            {
                text += $"Assertion failed: {expr} when performing {responsibleMethodCall}\n{file}:{line}";
            }
            else
            {
                text += $"Assertion failed: {expr}\nAt: {file}:{line}";
            }

            var page = new TaskDialogPage
            {
                Heading = "ImGui assertion failed",
                Caption = "Dalamud",
                Expander = new TaskDialogExpander
                {
                    CollapsedButtonText = "Show stack trace",
                    ExpandedButtonText = "Hide stack trace",
                    Text = stackTrace.Value.ToString(),
                },
                Text = text,
                Icon = TaskDialogIcon.Warning,
                Buttons =
                [
                    showOnGitHubButton,
                    breakButton,
                    disableButton,
                    ignoreButton,
                ],
                DefaultButton = showOnGitHubButton,
            };

            result = TaskDialog.ShowDialog(page);
        }

        // Run in a separate thread because of STA and to not mess up other stuff
        var thread = new Thread(DialogThreadStart)
        {
            Name = "Dalamud ImGui Assert Dialog",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (result == breakButton)
        {
            Debugger.Break();
        }
        else if (result == disableButton)
        {
            this.ShowAsserts = false;
        }
        else if (result == ignoreButton)
        {
            this.ignoredAsserts.Add(key);
        }

        return;

        void WarnForPlugin()
        {
            var pm = Service<PluginManager>.Get();
            var localPlugin = pm.FindCallingPlugin(stackTrace.Value);

            var errorHandler = localPlugin?
                                   .ServiceScope?
                                   .GetService(typeof(PluginErrorHandler)) as PluginErrorHandler;
            errorHandler?.NotifyError();
        }
    }

    private static unsafe class CustomNativeFunctions
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
        public static extern void igCustom_SetAssertCallback(void* cb);
#pragma warning restore SA1300
    }

    private static unsafe class CustomNativeFunctionsPlot
    {
        [DllImport("cimplot", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
        public static extern void igCustom_SetAssertCallback(void* cb);
#pragma warning restore SA1300
    }

    private static unsafe class CustomNativeFunctionsGuizmo
    {
        [DllImport("cimguizmo", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
        public static extern void igCustom_SetAssertCallback(void* cb);
#pragma warning restore SA1300
    }
}
