using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.SelfTest.Steps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;

using ImGuiNET;

using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest;

/// <summary>
/// Window for the Self-Test logic.
/// </summary>
internal class SelfTestWindow : Window
{
    private static readonly ModuleLog Log = new("AGING");

    private readonly List<ISelfTestStep> steps =
    [
        new LoginEventSelfTestStep(),
        new WaitFramesSelfTestStep(1000),
        new FrameworkTaskSchedulerSelfTestStep(),
        new EnterTerritorySelfTestStep(148, "Central Shroud"),
        new ItemPayloadSelfTestStep(),
        new ContextMenuSelfTestStep(),
        new NamePlateSelfTestStep(),
        new ActorTableSelfTestStep(),
        new FateTableSelfTestStep(),
        new AetheryteListSelfTestStep(),
        new ConditionSelfTestStep(),
        new ToastSelfTestStep(),
        new TargetSelfTestStep(),
        new KeyStateSelfTestStep(),
        new GamepadStateSelfTestStep(),
        new ChatSelfTestStep(),
        new HoverSelfTestStep(),
        new LuminaSelfTestStep<Item>(true),
        new LuminaSelfTestStep<Level>(true),
        new LuminaSelfTestStep<Lumina.Excel.Sheets.Action>(true),
        new LuminaSelfTestStep<Quest>(true),
        new LuminaSelfTestStep<TerritoryType>(false),
        new AddonLifecycleSelfTestStep(),
        new PartyFinderSelfTestStep(),
        new HandledExceptionSelfTestStep(),
        new DutyStateSelfTestStep(),
        new GameConfigSelfTestStep(),
        new MarketBoardSelfTestStep(),
        new SheetRedirectResolverSelfTestStep(),
        new NounProcessorSelfTestStep(),
        new SeStringEvaluatorSelfTestStep(),
        new CompletionSelfTestStep(),
        new LogoutEventSelfTestStep()
    ];

    private readonly Dictionary<int, (SelfTestStepResult Result, TimeSpan? Duration)> testIndexToResult = new();

    private bool selfTestRunning = false;
    private int currentStep = 0;
    private int scrollToStep = -1;
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
                this.testIndexToResult[this.currentStep] = (SelfTestStepResult.NotRan, null);
                this.steps[this.currentStep].CleanUp();
                this.currentStep++;
                this.scrollToStep = this.currentStep;
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
                this.scrollToStep = this.currentStep;
                this.testIndexToResult.Clear();
                this.lastTestStart = DateTimeOffset.Now;
            }
        }

        ImGui.SameLine();

        ImGui.TextUnformatted($"Step: {this.currentStep} / {this.steps.Count}");

        ImGui.Spacing();

        this.DrawResultTable();

        ImGui.Spacing();

        if (this.currentStep >= this.steps.Count)
        {
            if (this.selfTestRunning)
            {
                this.StopTests();
            }

            if (this.testIndexToResult.Any(x => x.Value.Result == SelfTestStepResult.Fail))
            {
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudRed, "One or more checks failed!");
            }
            else
            {
                ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.HealerGreen, "All checks passed!");
            }

            return;
        }

        if (!this.selfTestRunning)
        {
            return;
        }

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

        if (result != SelfTestStepResult.Waiting)
        {
            var duration = DateTimeOffset.Now - this.lastTestStart;
            this.testIndexToResult[this.currentStep] = (result, duration);
            this.currentStep++;
            this.scrollToStep = this.currentStep;

            this.lastTestStart = DateTimeOffset.Now;
        }
    }

    private void DrawResultTable()
    {
        var tableSize = ImGui.GetContentRegionAvail();

        if (this.selfTestRunning)
            tableSize -= new Vector2(0, 150);

        tableSize.Y = Math.Min(tableSize.Y, ImGui.GetWindowViewport().Size.Y * 0.5f);

        using var table = ImRaii.Table("agingResultTable", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY, tableSize);
        if (!table)
            return;

        ImGui.TableSetupColumn("###index", ImGuiTableColumnFlags.WidthFixed, 12f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Result", ImGuiTableColumnFlags.WidthFixed, 40f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 90f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        for (var i = 0; i < this.steps.Count; i++)
        {
            var step = this.steps[i];
            ImGui.TableNextRow();

            if (this.selfTestRunning && this.currentStep == i)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(ImGuiCol.TableRowBgAlt));
            }

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(i.ToString());

            if (this.selfTestRunning && this.scrollToStep == i)
            {
                ImGui.SetScrollHereY();
                this.scrollToStep = -1;
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(step.Name);

            if (this.testIndexToResult.TryGetValue(i, out var result))
            {
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();

                switch (result.Result)
                {
                    case SelfTestStepResult.Pass:
                        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.HealerGreen, "PASS");
                        break;
                    case SelfTestStepResult.Fail:
                        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudRed, "FAIL");
                        break;
                    default:
                        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "NR");
                        break;
                }

                ImGui.TableSetColumnIndex(3);
                if (result.Duration.HasValue)
                {
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextUnformatted(this.FormatTimeSpan(result.Duration.Value));
                }
            }
            else
            {
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                if (this.selfTestRunning && this.currentStep == i)
                {
                    ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "WAIT");
                }
                else
                {
                    ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, "NR");
                }

                ImGui.TableSetColumnIndex(3);
                ImGui.AlignTextToFramePadding();
                if (this.selfTestRunning && this.currentStep == i)
                {
                    ImGui.TextUnformatted(this.FormatTimeSpan(DateTimeOffset.Now - this.lastTestStart));
                }
            }

            ImGui.TableSetColumnIndex(4);
            using var id = ImRaii.PushId($"selfTest{i}");
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FastForward))
            {
                this.StopTests();
                this.testIndexToResult.Remove(i);
                this.currentStep = i;
                this.selfTestRunning = true;
                this.lastTestStart = DateTimeOffset.Now;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Jump to this test");
            }
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

    private string FormatTimeSpan(TimeSpan ts)
    {
        var str = ts.ToString("g", CultureInfo.InvariantCulture);
        var commaPos = str.LastIndexOf('.');
        return commaPos > -1 && commaPos + 5 < str.Length ? str[..(commaPos + 5)] : str;
    }
}
