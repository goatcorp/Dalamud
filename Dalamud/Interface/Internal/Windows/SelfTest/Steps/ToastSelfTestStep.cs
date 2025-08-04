using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui.Toast;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for toasts.
/// </summary>
internal class ToastSelfTestStep : ISelfTestStep
{
    private bool sentToasts = false;

    /// <inheritdoc/>
    public string Name => "Test Toasts";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        if (!this.sentToasts)
        {
            var toastGui = Service<ToastGui>.Get();
            toastGui.ShowNormal("Normal Toast");
            toastGui.ShowError("Error Toast");
            toastGui.ShowQuest("Quest Toast");
            this.sentToasts = true;
        }

        ImGui.Text("Did you see a normal toast, a quest toast and an error toast?"u8);

        if (ImGui.Button("Yes"u8))
        {
            return SelfTestStepResult.Pass;
        }

        ImGui.SameLine();
        if (ImGui.Button("No"u8))
        {
            return SelfTestStepResult.Fail;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        this.sentToasts = false;
    }
}
