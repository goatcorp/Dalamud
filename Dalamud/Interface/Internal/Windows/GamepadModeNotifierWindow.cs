using System.Numerics;

using CheapLoc;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Class responsible for drawing a notifier on screen that gamepad mode is active.
/// </summary>
internal class GamepadModeNotifierWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GamepadModeNotifierWindow"/> class.
    /// </summary>
    public GamepadModeNotifierWindow()
        : base(
            "###DalamudGamepadModeNotifier",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoMouseInputs
            | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings,
            true)
    {
        this.Size = Vector2.Zero;
        this.SizeCondition = ImGuiCond.Always;
        this.IsOpen = false;

        this.RespectCloseHotkey = false;
    }

    /// <summary>
    /// Draws a light grey-ish, main-viewport-big filled rect in the background draw list alongside a text indicating gamepad mode.
    /// </summary>
    public override void Draw()
    {
        var drawList = ImGui.GetBackgroundDrawList();
        drawList.PushClipRectFullScreen();
        drawList.AddRectFilled(Vector2.Zero, ImGuiHelpers.MainViewport.Size, 0x661A1A1A);
        drawList.AddText(
            Vector2.One,
            0xFFFFFFFF,
            Loc.Localize(
                "DalamudGamepadModeNotifierText",
                "Gamepad mode is ON. Press L1+L3 to deactivate, press R3 to toggle PluginInstaller."));
        drawList.PopClipRect();
    }
}
