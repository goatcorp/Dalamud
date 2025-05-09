using System.Collections.Generic;

using Dalamud.Game.Gui.NamePlate;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Tests for nameplates.
/// </summary>
internal class NamePlateSelfTestStep : ISelfTestStep
{
    private SubStep currentSubStep;
    private Dictionary<ulong, int>? updateCount;

    private enum SubStep
    {
        Start,
        Confirm,
    }

    /// <inheritdoc/>
    public string Name => "Test Nameplates";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var namePlateGui = Service<NamePlateGui>.Get();

        switch (this.currentSubStep)
        {
            case SubStep.Start:
                namePlateGui.OnNamePlateUpdate += this.OnNamePlateUpdate;
                namePlateGui.OnDataUpdate += this.OnDataUpdate;
                namePlateGui.RequestRedraw();
                this.updateCount = new Dictionary<ulong, int>();
                this.currentSubStep++;
                break;

            case SubStep.Confirm:
                ImGui.Text("Click to redraw all visible nameplates");
                if (ImGui.Button("Request redraw"))
                    namePlateGui.RequestRedraw();

                ImGui.TextUnformatted("Can you see marker icons above nameplates, and does\n" +
                                      "the update count increase when using request redraw?");

                if (ImGui.Button("Yes"))
                {
                    this.CleanUp();
                    return SelfTestStepResult.Pass;
                }

                ImGui.SameLine();

                if (ImGui.Button("No"))
                {
                    this.CleanUp();
                    return SelfTestStepResult.Fail;
                }

                break;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var namePlateGui = Service<NamePlateGui>.Get();
        namePlateGui.OnNamePlateUpdate -= this.OnNamePlateUpdate;
        namePlateGui.OnDataUpdate -= this.OnDataUpdate;
        namePlateGui.RequestRedraw();
        this.updateCount = null;
        this.currentSubStep = SubStep.Start;
    }

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            // Force nameplates to be visible
            handler.VisibilityFlags |= 1;

            // Set marker icon based on nameplate kind, and flicker when updating
            if (handler.IsUpdating || context.IsFullUpdate)
            {
                handler.MarkerIconId = 66181 + (int)handler.NamePlateKind;
            }
            else
            {
                handler.MarkerIconId = 66161 + (int)handler.NamePlateKind;
            }
        }
    }

    private void OnNamePlateUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        foreach (var handler in handlers)
        {
            // Append GameObject address to name
            var gameObjectAddress = handler.GameObject?.Address ?? 0;

            handler.Name = handler.Name.Append(new SeString(new UIForegroundPayload(9)))
                                  .Append($" (0x{gameObjectAddress:X})")
                                  .Append(new SeString(UIForegroundPayload.UIForegroundOff));

            // Track update count and set it as title
            var count = this.updateCount!.GetValueOrDefault(handler.GameObjectId);
            this.updateCount[handler.GameObjectId] = count + 1;

            handler.TitleParts.Text = $"Updates: {count}";
            handler.TitleParts.TextWrap = (new SeString(new UIForegroundPayload(43)),
                                              new SeString(UIForegroundPayload.UIForegroundOff));
            handler.DisplayTitle = true;
            handler.IsPrefixTitle = false;
        }
    }
}
