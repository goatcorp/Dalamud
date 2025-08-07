using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for the login events.
/// </summary>
internal class LogoutEventSelfTestStep : ISelfTestStep
{
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <inheritdoc/>
    public string Name => "Test Log-Out";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var clientState = Service<ClientState>.Get();

        ImGui.Text("Log out now..."u8);

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

    private void ClientStateOnOnLogout(int type, int code)
    {
        this.hasPassed = true;
    }
}
