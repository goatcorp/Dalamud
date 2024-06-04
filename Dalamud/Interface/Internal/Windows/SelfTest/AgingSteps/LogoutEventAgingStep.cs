using Dalamud.Game.ClientState;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.SelfTest.AgingSteps;

/// <summary>
/// Test setup for the login events.
/// </summary>
internal class LogoutEventAgingStep : IAgingStep
{
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <inheritdoc/>
    public string Name => "Test Log-Out";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var clientState = Service<ClientState>.Get();

        ImGui.Text("Log out now...");

        if (!this.subscribed)
        {
            clientState.Logout += this.ClientStateOnOnLogout;
            this.subscribed = true;
        }

        if (this.hasPassed)
        {
            clientState.Logout -= this.ClientStateOnOnLogout;
            this.subscribed = false;
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var clientState = Service<ClientState>.Get();

        if (this.subscribed)
        {
            clientState.Logout -= this.ClientStateOnOnLogout;
            this.subscribed = false;
        }
    }

    private void ClientStateOnOnLogout()
    {
        this.hasPassed = true;
    }
}
