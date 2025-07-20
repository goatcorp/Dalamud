using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for the Key State.
/// </summary>
internal class KeyStateSelfTestStep : ISelfTestStep
{
    /// <inheritdoc/>
    public string Name => "Test KeyState";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var keyState = Service<KeyState>.Get();

        ImGui.TextUnformatted("Hold down D,A,L,M,U"u8);

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
