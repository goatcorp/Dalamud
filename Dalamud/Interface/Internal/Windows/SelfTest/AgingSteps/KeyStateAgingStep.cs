using Dalamud.Game.ClientState.Keys;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the Key State.
/// </summary>
internal class KeyStateAgingStep : IAgingStep
{
    /// <inheritdoc/>
    public string Name => "Test KeyState";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var keyState = Service<KeyState>.Get();

        ImGui.Text("Hold down D,A,L,M,U");

        if (keyState[VirtualKey.D]
            && keyState[VirtualKey.A]
            && keyState[VirtualKey.L]
            && keyState[VirtualKey.M]
            && keyState[VirtualKey.U])
        {
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
