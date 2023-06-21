using System;

using Dalamud.Game.ClientState.GamePad;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Widget for displaying gamepad info.
/// </summary>
internal class GamepadWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.Gamepad;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var gamepadState = Service<GamepadState>.Get();

        static void DrawHelper(string text, uint mask, Func<GamepadButtons, float> resolve)
        {
            ImGui.Text($"{text} {mask:X4}");
            ImGui.Text($"DPadLeft {resolve(GamepadButtons.DpadLeft)} " +
                       $"DPadUp {resolve(GamepadButtons.DpadUp)} " +
                       $"DPadRight {resolve(GamepadButtons.DpadRight)} " +
                       $"DPadDown {resolve(GamepadButtons.DpadDown)} ");
            ImGui.Text($"West {resolve(GamepadButtons.West)} " +
                       $"North {resolve(GamepadButtons.North)} " +
                       $"East {resolve(GamepadButtons.East)} " +
                       $"South {resolve(GamepadButtons.South)} ");
            ImGui.Text($"L1 {resolve(GamepadButtons.L1)} " +
                       $"L2 {resolve(GamepadButtons.L2)} " +
                       $"R1 {resolve(GamepadButtons.R1)} " +
                       $"R2 {resolve(GamepadButtons.R2)} ");
            ImGui.Text($"Select {resolve(GamepadButtons.Select)} " +
                       $"Start {resolve(GamepadButtons.Start)} " +
                       $"L3 {resolve(GamepadButtons.L3)} " +
                       $"R3 {resolve(GamepadButtons.R3)} ");
        }

        ImGui.Text($"GamepadInput 0x{gamepadState.GamepadInputAddress.ToInt64():X}");

#if DEBUG
        if (ImGui.IsItemHovered())
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        if (ImGui.IsItemClicked())
            ImGui.SetClipboardText($"0x{gamepadState.GamepadInputAddress.ToInt64():X}");
#endif

        DrawHelper(
            "Buttons Raw",
            gamepadState.ButtonsRaw,
            gamepadState.Raw);
        DrawHelper(
            "Buttons Pressed",
            gamepadState.ButtonsPressed,
            gamepadState.Pressed);
        DrawHelper(
            "Buttons Repeat",
            gamepadState.ButtonsRepeat,
            gamepadState.Repeat);
        DrawHelper(
            "Buttons Released",
            gamepadState.ButtonsReleased,
            gamepadState.Released);
        ImGui.Text($"LeftStickLeft {gamepadState.LeftStickLeft:0.00} " +
                   $"LeftStickUp {gamepadState.LeftStickUp:0.00} " +
                   $"LeftStickRight {gamepadState.LeftStickRight:0.00} " +
                   $"LeftStickDown {gamepadState.LeftStickDown:0.00} ");
        ImGui.Text($"RightStickLeft {gamepadState.RightStickLeft:0.00} " +
                   $"RightStickUp {gamepadState.RightStickUp:0.00} " +
                   $"RightStickRight {gamepadState.RightStickRight:0.00} " +
                   $"RightStickDown {gamepadState.RightStickDown:0.00} ");
    }
}
