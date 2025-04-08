using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for Territory Change.
/// </summary>
internal class EnterTerritorySelfTestStep : ISelfTestStep
{
    private readonly ushort territory;
    private readonly string terriName;
    private bool subscribed = false;
    private bool hasPassed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnterTerritorySelfTestStep"/> class.
    /// </summary>
    /// <param name="terri">The territory to check for.</param>
    /// <param name="name">Name to show.</param>
    public EnterTerritorySelfTestStep(ushort terri, string name)
    {
        this.terriName = name;
        this.territory = terri;
    }

    /// <inheritdoc/>
    public string Name => $"Enter Terri: {this.terriName}";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var clientState = Service<ClientState>.Get();

        ImGui.TextUnformatted(this.Name);

        if (!this.subscribed)
        {
            clientState.TerritoryChanged += this.ClientStateOnTerritoryChanged;
            this.subscribed = true;
        }

        if (this.hasPassed)
        {
            clientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
            this.subscribed = false;
            return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        var clientState = Service<ClientState>.Get();

        clientState.TerritoryChanged -= this.ClientStateOnTerritoryChanged;
        this.subscribed = false;
    }

    private void ClientStateOnTerritoryChanged(ushort territoryId)
    {
        if (territoryId == this.territory)
        {
            this.hasPassed = true;
        }
    }
}
