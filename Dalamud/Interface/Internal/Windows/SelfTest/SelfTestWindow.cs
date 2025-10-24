using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.SelfTest;
using Dalamud.Plugin.SelfTest.Internal;
using Dalamud.Utility;
using Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest;

/// <summary>
/// Window for the Self-Test logic.
/// </summary>
internal class SelfTestWindow : Window
{
    private static readonly ModuleLog Log = new("AGING");

    private readonly SelfTestRegistry selfTestRegistry;

    private List<SelfTestWithResults> visibleSteps = new();

    private bool selfTestRunning = false;
    private SelfTestGroup? currentTestGroup = null;
    private SelfTestWithResults? currentStep = null;
    private SelfTestWithResults? scrollToStep = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestWindow"/> class.
    /// </summary>
    public SelfTestWindow()
        : base("Dalamud Self-Test", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.selfTestRegistry = Service<SelfTestRegistry>.Get();
        this.Size = new Vector2(800, 800);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        // Initialize to first group if not set (first time drawing)
        if (this.currentTestGroup == null)
        {
            this.currentTestGroup = this.selfTestRegistry.SelfTestGroups.FirstOrDefault(); // Should always be "Dalamud"
            if (this.currentTestGroup != null)
            {
                this.SelectTestGroup(this.currentTestGroup);
            }
        }

        // Update visible steps based on current group
        if (this.currentTestGroup != null)
        {
            this.visibleSteps = this.selfTestRegistry.SelfTests
                .Where(test => test.Group == this.currentTestGroup.Name).ToList();

            // Stop tests if no steps available or if current step is no longer valid
            if (this.visibleSteps.Count == 0 || (this.currentStep != null && !this.visibleSteps.Contains(this.currentStep)))
            {
                this.StopTests();
            }
        }

        using (var dropdown = ImRaii.Combo("###SelfTestGroupCombo"u8, this.currentTestGroup?.Name ?? string.Empty))
        {
            if (dropdown)
            {
                foreach (var testGroup in this.selfTestRegistry.SelfTestGroups)
                {
                    if (ImGui.Selectable(testGroup.Name))
                    {
                        this.SelectTestGroup(testGroup);
                    }

                    if (!testGroup.Loaded)
                    {
                        ImGui.SameLine();
                        this.DrawUnloadedIcon();
                    }
                }
            }
        }

        if (this.currentTestGroup?.Loaded == false)
        {
            ImGui.SameLine();
            this.DrawUnloadedIcon();
        }

        if (this.selfTestRunning)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Stop))
            {
                this.StopTests();
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.StepForward))
            {
                this.currentStep.Reset();
                this.MoveToNextTest();
                this.scrollToStep = this.currentStep;
                if (this.currentStep == null)
                {
                    this.StopTests();
                }
            }
        }
        else
        {
            var canRunTests = this.currentTestGroup?.Loaded == true && this.visibleSteps.Count > 0;

            using var disabled = ImRaii.Disabled(!canRunTests);
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            {
                this.selfTestRunning = true;
                this.currentStep = this.visibleSteps.FirstOrDefault();
                this.scrollToStep = this.currentStep;
                foreach (var test in this.visibleSteps)
                {
                    test.Reset();
                }
            }
        }

        ImGui.SameLine();

        var stepNumber = this.currentStep != null ? this.visibleSteps.IndexOf(this.currentStep) : 0;
        ImGui.Text($"Step: {stepNumber} / {this.visibleSteps.Count}"); 

        ImGui.Spacing();

        if (this.currentTestGroup?.Loaded == false)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, $"Plugin '{this.currentTestGroup.Name}' is unloaded. No tests available.");
            ImGui.Spacing();
        }

        this.DrawResultTable();

        ImGui.Spacing();

        if (this.currentStep == null)
        {
            if (this.selfTestRunning)
            {
                this.StopTests();
            }

            if (this.visibleSteps.Any(test => test.Result == SelfTestStepResult.Fail))
            {
                ImGui.TextColoredWrapped(ImGuiColors.DalamudRed, "One or more checks failed!"u8);
            }
            else if (this.visibleSteps.All(test => test.Result == SelfTestStepResult.Pass))
            {
                ImGui.TextColoredWrapped(ImGuiColors.HealerGreen, "All checks passed!"u8);
            }

            return;
        }

        if (!this.selfTestRunning)
        {
            return;
        }

        using var resultChild = ImRaii.Child("SelfTestResultChild"u8, ImGui.GetContentRegionAvail());
        if (!resultChild) return;

        ImGui.Text($"Current: {this.currentStep.Name}");

        ImGuiHelpers.ScaledDummy(10);

        this.currentStep.DrawAndStep();
        if (this.currentStep.Result != SelfTestStepResult.Waiting)
        {
            this.MoveToNextTest();
        }
    }

    private void DrawResultTable()
    {
        var tableSize = ImGui.GetContentRegionAvail();

        if (this.selfTestRunning)
            tableSize -= new Vector2(0, 200);

        tableSize.Y = Math.Min(tableSize.Y, ImGui.GetWindowViewport().Size.Y * 0.5f);

        using var table = ImRaii.Table("agingResultTable"u8, 5, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollY, tableSize);
        if (!table)
            return;

        ImGui.TableSetupColumn("###index"u8, ImGuiTableColumnFlags.WidthFixed, 12f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Name"u8);
        ImGui.TableSetupColumn("Result"u8, ImGuiTableColumnFlags.WidthFixed, 40f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn("Duration"u8, ImGuiTableColumnFlags.WidthFixed, 90f * ImGuiHelpers.GlobalScale);
        ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 30f * ImGuiHelpers.GlobalScale);

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableHeadersRow();

        foreach (var (step, index) in this.visibleSteps.WithIndex())
        {
            ImGui.TableNextRow();

            if (this.selfTestRunning && this.currentStep == step)
            {
                ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(ImGuiCol.TableRowBgAlt));
            }

            ImGui.TableSetColumnIndex(0);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(index.ToString());

            if (this.selfTestRunning && this.scrollToStep == step)
            {
                ImGui.SetScrollHereY();
                this.scrollToStep = null;
            }

            ImGui.TableSetColumnIndex(1);
            ImGui.AlignTextToFramePadding();
            ImGui.Text(step.Name);

            ImGui.TableSetColumnIndex(2);
            ImGui.AlignTextToFramePadding();
            switch (step.Result)
            {
                case SelfTestStepResult.Pass:
                    ImGui.TextColored(ImGuiColors.HealerGreen, "PASS"u8);
                    break;
                case SelfTestStepResult.Fail:
                    ImGui.TextColored(ImGuiColors.DalamudRed, "FAIL"u8);
                    break;
                case SelfTestStepResult.Waiting:
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "WAIT"u8);
                    break;
                default:
                    ImGui.TextColored(ImGuiColors.DalamudGrey, "NR"u8);
                    break;
            }

            ImGui.TableSetColumnIndex(3);
            if (step.Duration.HasValue)
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text(this.FormatTimeSpan(step.Duration.Value));
            }

            ImGui.TableSetColumnIndex(4);
            using var id = ImRaii.PushId($"selfTest{index}");
            if (ImGuiComponents.IconButton(FontAwesomeIcon.FastForward))
            {
                this.StopTests();
                this.currentStep = step;
                this.currentStep.Reset();
                this.selfTestRunning = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Jump to this test"u8);
            }
        }
    }

    private void StopTests()
    {
        this.selfTestRunning = false;
        this.currentStep = null;
        this.scrollToStep = null;

        foreach (var agingStep in this.visibleSteps)
        {
            try
            {
                agingStep.Finish();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Could not clean up AgingStep: {agingStep.Name}");
            }
        }
    }

    /// <summary>
    /// Makes <paramref name="testGroup"/> the active test group.
    /// </summary>
    /// <param name="testGroup">The test group to make active.</param>
    private void SelectTestGroup(SelfTestGroup testGroup)
    {
        this.currentTestGroup = testGroup;
        this.StopTests();
    }

    /// <summary>
    /// Move `currentTest` to the next test. If there are no tests left, set `currentTest` to null.
    /// </summary>
    private void MoveToNextTest()
    {
        if (this.currentStep == null)
        {
            this.currentStep = this.visibleSteps.FirstOrDefault();
            return;
        }

        var currentIndex = this.visibleSteps.IndexOf(this.currentStep);
        this.currentStep = this.visibleSteps.ElementAtOrDefault(currentIndex + 1);
    }

    /// <summary>
    /// Draws the unloaded plugin icon with tooltip.
    /// </summary>
    private void DrawUnloadedIcon()
    {
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.Unlink.ToIconString());
        ImGui.PopFont();

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Plugin is unloaded");
        }
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        var str = ts.ToString("g", CultureInfo.InvariantCulture);
        var commaPos = str.LastIndexOf('.');
        return commaPos > -1 && commaPos + 5 < str.Length ? str[..(commaPos + 5)] : str;
    }
}
