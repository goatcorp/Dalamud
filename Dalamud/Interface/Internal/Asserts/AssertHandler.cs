using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Asserts;

/// <summary>
/// Class responsible for registering and handling ImGui asserts.
/// </summary>
internal class AssertHandler : IDisposable
{
    private const int HideThreshold = 20;
    private const int HidePrintEvery = 500;

    private readonly HashSet<string> ignoredAsserts = [];
    private readonly Dictionary<string, uint> assertCounts = new();

    // Store callback to avoid it from being GC'd
    private readonly AssertCallbackDelegate callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssertHandler"/> class.
    /// </summary>
    public AssertHandler()
    {
        this.callback = (expr, file, line) => this.OnImGuiAssert(expr, file, line);
    }

    private delegate void AssertCallbackDelegate(
        [MarshalAs(UnmanagedType.LPStr)] string expr,
        [MarshalAs(UnmanagedType.LPStr)] string file,
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
    public void Setup()
    {
        CustomNativeFunctions.igCustom_SetAssertCallback(this.callback);
    }

    /// <summary>
    /// Unregister the cimgui assert handler with the native library.
    /// </summary>
    public void Shutdown()
    {
        CustomNativeFunctions.igCustom_SetAssertCallback(null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Shutdown();
    }

    private void OnImGuiAssert(string expr, string file, int line)
    {
        var key = $"{file}:{line}";
        if (this.ignoredAsserts.Contains(key))
            return;

        Lazy<string> stackTrace = new(() => new StackTrace(3).ToString());

        if (!this.EnableVerboseLogging)
        {
            if (this.assertCounts.TryGetValue(key, out var count))
            {
                this.assertCounts[key] = count + 1;

                if (count <= HideThreshold || count % HidePrintEvery == 0)
                {
                    Log.Warning("ImGui assertion failed: {Expr} at {File}:{Line} (repeated {Count} times)",
                                expr,
                                file,
                                line,
                                count);
                }
            }
            else
            {
                this.assertCounts[key] = 1;
            }
        }
        else
        {
            Log.Warning("ImGui assertion failed: {Expr} at {File}:{Line}\n{StackTrace:l}",
                        expr,
                        file,
                        line,
                        stackTrace.Value);
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

            var page = new TaskDialogPage
            {
                Heading = "ImGui assertion failed",
                Caption = "Dalamud",
                Expander = new TaskDialogExpander
                {
                    CollapsedButtonText = "Show stack trace",
                    ExpandedButtonText = "Hide stack trace",
                    Text = stackTrace.Value,
                },
                Text = $"Some code in a plugin or Dalamud itself has caused an internal assertion in ImGui to fail. The game will most likely crash now.\n\n{expr}\nAt: {file}:{line}",
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
    }

    private static class CustomNativeFunctions
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
#pragma warning disable SA1300
        public static extern void igCustom_SetAssertCallback(AssertCallbackDelegate? callback);
#pragma warning restore SA1300
    }
}
