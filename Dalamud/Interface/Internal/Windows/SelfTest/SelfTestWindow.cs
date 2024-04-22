using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest;

/// <summary>
/// Window for the Self-Test logic.
/// </summary>
internal class SelfTestWindow : Window
{
    private static readonly ModuleLog Log = new("AGING");

    private readonly List<IAgingStep> steps =
        new()
        {
            new LoginEventAgingStep(),
            new WaitFramesAgingStep(1000),
            new EnterTerritoryAgingStep(148, "Central Shroud"),
            new ItemPayloadAgingStep(),
            new ContextMenuAgingStep(),
            new ActorTableAgingStep(),
            new FateTableAgingStep(),
            new AetheryteListAgingStep(),
            new ConditionAgingStep(),
            new ToastAgingStep(),
            new TargetAgingStep(),
            new KeyStateAgingStep(),
            new GamepadStateAgingStep(),
            new ChatAgingStep(),
            new HoverAgingStep(),
            new LuminaAgingStep<TerritoryType>(),
            new AddonLifecycleAgingStep(),
            new PartyFinderAgingStep(),
            new HandledExceptionAgingStep(),
            new DutyStateAgingStep(),
            new GameConfigAgingStep(),
            new LogoutEventAgingStep(),
        };

    private readonly List<(SelfTestStepResult Result, TimeSpan? Duration)> stepResults = new();

    private bool selfTestRunning = false;
    private int currentStep = 0;

    private DateTimeOffset lastTestStart;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestWindow"/> class.
    /// </summary>
    public SelfTestWindow()
        : base("Dalamud Self-Test", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(800, 800);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.selfTestRunning)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Stop))
            {
                this.StopTests();
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.StepForward))
            {
                this.stepResults.Add((SelfTestStepResult.NotRan, null));
                this.currentStep++;
                this.lastTestStart = DateTimeOffset.Now;

                if (this.currentStep >= this.steps.Count)
                {
                    this.StopTests();
                }
            }
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            {
                this.selfTestRunning = true;
                this.currentStep = 0;
                this.stepResults.Clear();
                this.lastTestStart = DateTimeOffset.Now;
            }
        }

        ImGui.SameLine();

        ImGui.TextUnformatted($"Step: {this.currentStep} / {this.steps.Count}");

        ImGuiHelpers.ScaledDummy(10);

        this.DrawResultTable();

        ImGuiHelpers.ScaledDummy(10);

        if (this.currentStep >= this.steps.Count)
        {
            if (this.selfTestRunning)
            {
                this.StopTests();
            }

            if (this.stepResults.Any(x => x.Result == SelfTestStepResult.Fail))
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "One or more checks failed!");
            }
            else
            {
                ImGui.TextColored(ImGuiColors.HealerGreen, "All checks passed!");
            }

            return;
        }

        if (!this.selfTestRunning)
        {
            return;
        }

        ImGui.Separator();

        var step = this.steps[this.currentStep];
        ImGui.TextUnformatted($"Current: {step.Name}");

        ImGuiHelpers.ScaledDummy(10);

        SelfTestStepResult result;
        try
        {
            result = step.RunStep();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Step failed: {step.Name}");
            result = SelfTestStepResult.Fail;
        }

        ImGui.Separator();

        if (result != SelfTestStepResult.Waiting)
        {
            var duration = DateTimeOffset.Now - this.lastTestStart;
            this.currentStep++;
            this.stepResults.Add((result, duration));

            this.lastTestStart = DateTimeOffset.Now;
        }
    }

    private void DrawResultTable()
    {
        if (ImGui.BeginTable("agingResultTable", 4, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("###index", ImGuiTableColumnFlags.WidthFixed, 12f);
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, 40f);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 90f);

            ImGui.TableHeadersRow();

            for (var i = 0; i < this.steps.Count; i++)
            {
                var step = this.steps[i];
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text(i.ToString());

                ImGui.TableSetColumnIndex(1);
                ImGui.Text(step.Name);

                ImGui.TableSetColumnIndex(2);
                ImGui.PushFont(Interface.Internal.InterfaceManager.MonoFont);
                if (this.stepResults.Count > i)
                {
                    var result = this.stepResults[i];

                    switch (result.Result)
                    {
                        case SelfTestStepResult.Pass:
                            ImGui.TextColored(ImGuiColors.HealerGreen, "PASS");
                            break;
                        case SelfTestStepResult.Fail:
                            ImGui.TextColored(ImGuiColors.DalamudRed, "FAIL");
                            break;
                        default:
                            ImGui.TextColored(ImGuiColors.DalamudGrey, "NR");
                            break;
                    }
                }
                else
                {
                    if (this.selfTestRunning && this.currentStep == i)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "WAIT");
                    }
                    else
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "NR");
                    }
                }

                ImGui.PopFont();

                ImGui.TableSetColumnIndex(3);
                if (this.stepResults.Count > i)
                {
                    var (_, duration) = this.stepResults[i];

                    if (duration.HasValue)
                    {
                        ImGui.TextUnformatted(duration.Value.ToString("g"));
                    }
                }
                else
                {
                    if (this.selfTestRunning && this.currentStep == i)
                    {
                        ImGui.TextUnformatted((DateTimeOffset.Now - this.lastTestStart).ToString("g"));
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    private void StopTests()
    {
        this.selfTestRunning = false;

        foreach (var agingStep in this.steps)
        {
            try
            {
                agingStep.CleanUp();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Could not clean up AgingStep: {agingStep.Name}");
            }
        }
    }
}
