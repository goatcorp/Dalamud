using Dalamud.Game.ClientState;

using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for the login events.
/// </summary>
internal class LoginEventSelfTestStep : ISelfTestStep
{
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <inheritdoc/>
    public string Name => "Test Log-In";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var clientState = Service<ClientState>.Get();

        ImGui.Text("Log in now...");

        if (!this.subscribed)
        {
            clientState.Login += this.ClientStateOnOnLogin;
            this.subscribed = true;
        }

        if (this.hasPassed)
        {
            clientState.Login -= this.ClientStateOnOnLogin;
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
            clientState.Login -= this.ClientStateOnOnLogin;
            this.subscribed = false;
        }
    }

    private void ClientStateOnOnLogin()
    {
        this.hasPassed = true;
    }
}
