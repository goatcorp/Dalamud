using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState;
using Dalamud.Game.Text.Evaluator;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for SeStringEvaluator.
/// </summary>
internal class SeStringEvaluatorSelfTestStep : ISelfTestStep
{
    private int step = 0;

    /// <inheritdoc/>
    public string Name => "Test SeStringEvaluator";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var seStringEvaluator = Service<SeStringEvaluator>.Get();

        switch (this.step)
        {
            case 0:
                ImGui.Text("Is this the current time, and is it ticking?"u8);

                // This checks that EvaluateFromAddon fetches the correct Addon row,
                // that MacroDecoder.GetMacroTime()->SetTime() has been called
                // and that local and global parameters have been read correctly.

                ImGui.Text(seStringEvaluator.EvaluateFromAddon(31, [(uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()]).ExtractText());

                if (ImGui.Button("Yes"u8))
                    this.step++;

                ImGui.SameLine();

                if (ImGui.Button("No"u8))
                    return SelfTestStepResult.Fail;

                break;

            case 1:
                ImGui.Text("Checking pcname macro using the local player name..."u8);

                // This makes sure that NameCache.Instance()->TryGetCharacterInfoByEntityId() has been called,
                // that it returned the local players name by using its EntityId,
                // and that it didn't include the world name by checking the HomeWorldId against AgentLobby.Instance()->LobbyData.HomeWorldId.

                var clientState = Service<ClientState>.Get();
                var localPlayer = clientState.LocalPlayer;
                if (localPlayer is null)
                {
                    ImGui.Text("You need to be logged in for this step."u8);

                    if (ImGui.Button("Skip"u8))
                        return SelfTestStepResult.NotRan;

                    return SelfTestStepResult.Waiting;
                }

                var evaluatedPlayerName = seStringEvaluator.Evaluate(ReadOnlySeString.FromMacroString("<pcname(lnum1)>"), [localPlayer.EntityId]).ExtractText();
                var localPlayerName = localPlayer.Name.TextValue;

                if (evaluatedPlayerName != localPlayerName)
                {
                    ImGui.Text("The player name doesn't match:"u8);
                    ImGui.Text($"Evaluated Player Name (got): {evaluatedPlayerName}");
                    ImGui.Text($"Local Player Name (expected): {localPlayerName}");

                    if (ImGui.Button("Continue"u8))
                        return SelfTestStepResult.Fail;

                    return SelfTestStepResult.Waiting;
                }

                return SelfTestStepResult.Pass;
        }

        return SelfTestStepResult.Waiting;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
        this.step = 0;
    }
}
