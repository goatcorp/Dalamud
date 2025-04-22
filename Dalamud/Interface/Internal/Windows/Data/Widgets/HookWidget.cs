using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Hooking;
using Dalamud.Hooking.Internal;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;
using PInvoke;
using Serilog;

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

    private AddonLifecycleAddressResolver? address;

    private delegate int MessageBoxWDelegate(
        IntPtr hWnd,
        [MarshalAs(UnmanagedType.LPWStr)] string text,
        [MarshalAs(UnmanagedType.LPWStr)] string caption,
        NativeFunctions.MessageBoxType type);

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
    public string[]? CommandShortcuts { get; init; } = { "hook" };

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;

        this.address = new AddonLifecycleAddressResolver();
        this.address.Setup(Service<TargetSigScanner>.Get());
    }

    /// <inheritdoc/>
    public void Draw()
    {
        try
        {
            ImGui.Checkbox("Use MinHook (only for regular hooks, AsmHook is Reloaded-only)", ref this.hookUseMinHook);

            ImGui.Separator();

            if (ImGui.Button("Create"))
                this.messageBoxMinHook = Hook<MessageBoxWDelegate>.FromSymbol("User32", "MessageBoxW", this.MessageBoxWDetour, this.hookUseMinHook);

            if (ImGui.Button("Enable"))
                this.messageBoxMinHook?.Enable();

            if (ImGui.Button("Disable"))
                this.messageBoxMinHook?.Disable();

            if (ImGui.Button("Call Original"))
                this.messageBoxMinHook?.Original(IntPtr.Zero, "Hello from .Original", "Hook Test", NativeFunctions.MessageBoxType.Ok);

            if (ImGui.Button("Dispose"))
            {
                this.messageBoxMinHook?.Dispose();
                this.messageBoxMinHook = null;
            }

            if (ImGui.Button("Test"))
                _ = NativeFunctions.MessageBoxW(IntPtr.Zero, "Hi", "Hello", NativeFunctions.MessageBoxType.Ok);

            if (this.messageBoxMinHook != null)
                ImGui.Text("Enabled: " + this.messageBoxMinHook?.IsEnabled);

            ImGui.Separator();

            ImGui.BeginDisabled(this.hookStressTestRunning);
            ImGui.Text("Stress Test");

            if (ImGui.InputInt("Max", ref this.hookStressTestMax))
                this.hookStressTestCount = 0;

            ImGui.InputInt("Wait (ms)", ref this.hookStressTestWait);
            ImGui.InputInt("Max Degree of Parallelism", ref this.hookStressTestMaxDegreeOfParallelism);

            if (ImGui.BeginCombo("Target", HookTargetToString(this.hookStressTestHookTarget)))
            {
                foreach (var target in Enum.GetValues<StressTestHookTarget>())
                {
                    if (ImGui.Selectable(HookTargetToString(target), this.hookStressTestHookTarget == target))
                        this.hookStressTestHookTarget = target;
                }

                ImGui.EndCombo();
            }

            if (ImGui.Button("Stress Test"))
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

            ImGui.TextUnformatted("Status: " + (this.hookStressTestRunning ? "Running" : "Idle"));
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

    private int MessageBoxWDetour(IntPtr hwnd, string text, string caption, NativeFunctions.MessageBoxType type)
    {
        Log.Information("[DATAHOOK] {Hwnd} {Text} {Caption} {Type}", hwnd, text, caption, type);

        var result = this.messageBoxWOriginal!(hwnd, "Cause Access Violation?", caption, NativeFunctions.MessageBoxType.YesNo);

        if (result == (int)User32.MessageBoxResult.IDYES)
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
        var hook = Hook<AddonFinalizeDelegate>.FromAddress(this.address!.AddonFinalize, this.OnAddonFinalize);

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
