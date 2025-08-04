using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for the Fate Table.
/// </summary>
internal class FateTableSelfTestStep : ISelfTestStep
{
    private byte index = 0;

    /// <inheritdoc/>
    public string Name => "Test FateTable";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var fateTable = Service<FateTable>.Get();

        ImGui.Text("Checking fate table..."u8);

        if (fateTable.Length == 0)
        {
            ImGui.Text("Go to a zone that has FATEs currently up."u8);
            return SelfTestStepResult.Waiting;
        }

        if (this.index == fateTable.Length - 1)
        {
            return SelfTestStepResult.Pass;
        }

        var actor = fateTable[this.index];
        this.index++;

        if (actor == null)
        {
            return SelfTestStepResult.Waiting;
        }

        Util.ShowObject(actor);

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }
}
