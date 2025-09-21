using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.ClientState;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Game.Text.SeStringHandling.Payloads;

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

                this.step++;
                break;

            case 2:
                ImGui.Text("Checking AutoTranslatePayload.Text results..."u8);

                var config = Service<DalamudConfiguration>.Get();
                var originalLanguageOverride = config.LanguageOverride;

                Span<(string Language, uint Group, uint Key, string ExpectedText)> tests = [
                    ("en", 49u, 209u, " albino karakul "), // Mount
                    ("en", 62u, 116u, " /echo "), // TextCommand - testing Command
                    ("en", 62u, 143u, " /dutyfinder "), // TextCommand - testing Alias over Command
                    ("en", 65u, 67u, " Minion of Light "), // Companion - testing noun handling for the german language (special case)
                    ("en", 71u, 7u, " Phantom Geomancer "), // MKDSupportJob

                    ("de", 49u, 209u, " Albino-Karakul "), // Mount
                    ("de", 62u, 115u, " /freiegesellschaft "), // TextCommand - testing Alias over Command
                    ("de", 62u, 116u, " /echo "), // TextCommand - testing Command
                    ("de", 65u, 67u, " Begleiter des Lichts "), // Companion - testing noun handling for the german language (special case)
                    ("de", 71u, 7u, " Phantom-Geomant "), // MKDSupportJob
                ];

                try
                {
                    foreach (var (language, group, key, expectedText) in tests)
                    {
                        config.LanguageOverride = language;

                        var payload = new AutoTranslatePayload(group, key);

                        if (payload.Text != expectedText)
                        {
                            ImGui.Text($"Test failed for Group {group}, Key {key}");
                            ImGui.Text($"Expected: {expectedText}");
                            ImGui.Text($"Got: {payload.Text}");

                            if (ImGui.Button("Continue"u8))
                                return SelfTestStepResult.Fail;

                            return SelfTestStepResult.Waiting;
                        }
                    }
                }
                finally
                {
                    config.LanguageOverride = originalLanguageOverride;
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
