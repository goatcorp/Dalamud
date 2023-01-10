using Dalamud.Game.Gui.Toast;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for toasts.
/// </summary>
internal class ToastAgingStep : IAgingStep
{
    /// <inheritdoc/>
    public string Name => "Test Toasts";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var toastGui = Service<ToastGui>.Get();

        toastGui.ShowNormal("Normal Toast");
        toastGui.ShowError("Error Toast");

        return SelfTestStepResult.Pass;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
