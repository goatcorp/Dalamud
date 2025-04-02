using System.Linq;

using Dalamud.Game.ClientState.GamePad;
using Dalamud.Interface.Utility;

using Lumina.Text.Payloads;

using LSeStringBuilder = Lumina.Text.SeStringBuilder;

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

        var builder = LSeStringBuilder.SharedPool.Get();

        builder.Append("Hold down ");

        for (var i = 0; i < buttons.Length; i++)
        {
            var (button, iconId) = buttons[i];

            builder.BeginMacro(MacroCode.Icon).AppendUIntExpression(iconId).EndMacro();
            builder.PushColorRgba(gamepadState.Raw(button) == 1 ? 0x0000FF00u : 0x000000FF);
            builder.Append(button.ToString());
            builder.PopColor();

            builder.Append(i < buttons.Length - 1 ? ", " : ".");
        }

        ImGuiHelpers.SeStringWrapped(builder.ToReadOnlySeString());

        LSeStringBuilder.SharedPool.Return(builder);

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
