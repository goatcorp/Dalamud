using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Hooking;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Serilog;

using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying hook information.
/// </summary>
internal unsafe class HookWidget : IDataWindowWidget
{
    private readonly List<IDalamudHook> hookStressTestList = [];

    private Hook<MessageBoxWDelegate>? messageBoxMinHook;
    private bool hookUseMinHook;

    private int hookStressTestCount = 0;
    private int hookStressTestMax = 1000;
    private int hookStressTestWait = 100;
    private int hookStressTestMaxDegreeOfParallelism = 10;
    private StressTestHookTarget hookStressTestHookTarget = StressTestHookTarget.Random;
    private bool hookStressTestRunning = false;

    private MessageBoxWDelegate? messageBoxWOriginal;
    private AddonFinalizeDelegate? addonFinalizeOriginal;

    private nint address;

    private delegate int MessageBoxWDelegate(
        IntPtr hWnd,
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        [MarshalAs(UnmanagedType.LPWStr)] string caption,
        MESSAGEBOX_STYLE type);

    private delegate void AddonFinalizeDelegate(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase);

    private enum StressTestHookTarget
    {
        MessageBoxW,
        AddonFinalize,
        Random,
    }

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Hook";

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["hook"];

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;

        var sigScanner = Service<TargetSigScanner>.Get();
        this.address = sigScanner.ScanText("E8 ?? ?? ?? ?? 48 83 EF 01 75 D5");
    }

    /// <inheritdoc/>
    public void Draw()
    {
        try
        {
            ImGui.Checkbox("Use MinHook (only for regular hooks, AsmHook is Reloaded-only)"u8, ref this.hookUseMinHook);

            ImGui.Separator();

            if (ImGui.Button("Create"u8))
                this.messageBoxMinHook = Hook<MessageBoxWDelegate>.FromSymbol("User32", "MessageBoxW", this.MessageBoxWDetour, this.hookUseMinHook);

            if (ImGui.Button("Enable"u8))
                this.messageBoxMinHook?.Enable();

            if (ImGui.Button("Disable"u8))
                this.messageBoxMinHook?.Disable();

            if (ImGui.Button("Call Original"u8))
                this.messageBoxMinHook?.Original(IntPtr.Zero, "Hello from .Original", "Hook Test", MESSAGEBOX_STYLE.MB_OK);

            if (ImGui.Button("Dispose"u8))
            {
                this.messageBoxMinHook?.Dispose();
                this.messageBoxMinHook = null;
            }

            if (ImGui.Button("Test"u8))
                _ = global::Windows.Win32.PInvoke.MessageBox(HWND.Null, "Hi", "Hello", MESSAGEBOX_STYLE.MB_OK);

            if (this.messageBoxMinHook != null)
                ImGui.Text("Enabled: " + this.messageBoxMinHook?.IsEnabled);

            ImGui.Separator();

            ImGui.BeginDisabled(this.hookStressTestRunning);
            ImGui.Text("Stress Test"u8);

            if (ImGui.InputInt("Max"u8, ref this.hookStressTestMax))
                this.hookStressTestCount = 0;

            ImGui.InputInt("Wait (ms)"u8, ref this.hookStressTestWait);
            ImGui.InputInt("Max Degree of Parallelism"u8, ref this.hookStressTestMaxDegreeOfParallelism);

            if (ImGui.BeginCombo("Target"u8, HookTargetToString(this.hookStressTestHookTarget)))
            {
                foreach (var target in Enum.GetValues<StressTestHookTarget>())
                {
                    if (ImGui.Selectable(HookTargetToString(target), this.hookStressTestHookTarget == target))
                        this.hookStressTestHookTarget = target;
                }

                ImGui.EndCombo();
            }

            if (ImGui.Button("Stress Test"u8))
            {
                Task.Run(() =>
                {
                    this.hookStressTestRunning = true;
                    this.hookStressTestCount = 0;
                    Parallel.For(
                        0,
                        this.hookStressTestMax,
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = this.hookStressTestMaxDegreeOfParallelism,
                        },
                        _ =>
                        {
                            this.hookStressTestList.Add(this.HookTarget(this.hookStressTestHookTarget));
                            this.hookStressTestCount++;
                            Thread.Sleep(this.hookStressTestWait);
                        });
                }).ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Log.Error(t.Exception, "Stress test failed");
                    }
                    else
                    {
                        Log.Information("Stress test completed");
                    }

                    this.hookStressTestRunning = false;
                    this.hookStressTestList.ForEach(hook =>
                    {
                        hook.Dispose();
                    });
                    this.hookStressTestList.Clear();
                });
            }

            ImGui.EndDisabled();

            ImGui.Text("Status: " + (this.hookStressTestRunning ? "Running" : "Idle"));
            ImGui.ProgressBar(this.hookStressTestCount / (float)this.hookStressTestMax, new System.Numerics.Vector2(0, 0), $"{this.hookStressTestCount}/{this.hookStressTestMax}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Hook error caught");
        }
    }

    private static string HookTargetToString(StressTestHookTarget target)
    {
        return target switch
        {
            StressTestHookTarget.MessageBoxW => "MessageBoxW (Hook)",
            StressTestHookTarget.AddonFinalize => "AddonFinalize (Hook)",
            _ => target.ToString(),
        };
    }

    private int MessageBoxWDetour(IntPtr hwnd, string text, string caption, MESSAGEBOX_STYLE type)
    {
        Log.Information("[DATAHOOK] {Hwnd} {Text} {Caption} {Type}", hwnd, text, caption, type);

        var result = this.messageBoxWOriginal!(hwnd, "Cause Access Violation?", caption, MESSAGEBOX_STYLE.MB_YESNO);

        if (result == (int)MESSAGEBOX_RESULT.IDYES)
        {
            Marshal.ReadByte(IntPtr.Zero);
        }

        return result;
    }

    private void OnAddonFinalize(AtkUnitManager* unitManager, AtkUnitBase** atkUnitBase)
    {
        Log.Information("OnAddonFinalize");
        this.addonFinalizeOriginal!(unitManager, atkUnitBase);
    }

    private void OnAddonUpdate(AtkUnitBase* thisPtr, float delta)
    {
        Log.Information("OnAddonUpdate");
    }

    private IDalamudHook HookMessageBoxW()
    {
        var hook = Hook<MessageBoxWDelegate>.FromSymbol(
            "User32",
            "MessageBoxW",
            this.MessageBoxWDetour,
            this.hookUseMinHook);

        this.messageBoxWOriginal = hook.Original;
        hook.Enable();
        return hook;
    }

    private IDalamudHook HookAddonFinalize()
    {
        var hook = Hook<AddonFinalizeDelegate>.FromAddress(this.address, this.OnAddonFinalize);

        this.addonFinalizeOriginal = hook.Original;
        hook.Enable();
        return hook;
    }

    private IDalamudHook HookTarget(StressTestHookTarget target)
    {
        if (target == StressTestHookTarget.Random)
        {
            target = (StressTestHookTarget)Random.Shared.Next(0, 2);
        }

        return target switch
        {
            StressTestHookTarget.MessageBoxW => this.HookMessageBoxW(),
            StressTestHookTarget.AddonFinalize => this.HookAddonFinalize(),
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null),
        };
    }
}
