using System.Linq;

using Dalamud.Game.ClientState.GamePad;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.SelfTest;
using Dalamud.Utility;

using Lumina.Text.Payloads;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for the Gamepad State.
/// </summary>
internal class GamepadStateSelfTestStep : ISelfTestStep
{
    /// <inheritdoc/>
    public string Name => "Test GamePadState";

    /// <inheritdoc/>
    public SelfTestStepResult RunStep()
    {
        var gamepadState = Service<GamepadState>.Get();

        var buttons = new (GamepadButtons Button, uint IconId)[]
        {
            (GamepadButtons.North, 11),
            (GamepadButtons.East, 8),
            (GamepadButtons.L1, 12),
        };

        using var rssb = new RentedSeStringBuilder();

        rssb.Builder.Append("Hold down ");

        for (var i = 0; i < buttons.Length; i++)
        {
            var (button, iconId) = buttons[i];

            rssb.Builder
                .BeginMacro(MacroCode.Icon)
                .AppendUIntExpression(iconId)
                .EndMacro()
                .PushColorRgba(gamepadState.Raw(button) == 1 ? 0x0000FF00u : 0x000000FF)
                .Append(button.ToString())
                .PopColor()
                .Append(i < buttons.Length - 1 ? ", " : ".");
        }

        ImGuiHelpers.SeStringWrapped(rssb.Builder.ToReadOnlySeString());

        if (buttons.All(tuple => gamepadState.Raw(tuple.Button) == 1))
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
