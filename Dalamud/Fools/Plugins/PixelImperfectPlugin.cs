using System;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using ImGuiNET;
using static ImGuiNET.ImGuiWindowFlags;

namespace Dalamud.Fools.Plugins;

public class PixelImperfectPlugin : IFoolsPlugin
{
    private ClientState clientState;
    private GameGui gameGui;

    public PixelImperfectPlugin()
    {
        this.clientState = Service<ClientState>.Get();
        this.gameGui = Service<GameGui>.Get();
    }

    public void DrawUi()
    {
        if (this.clientState.LocalPlayer == null) return;

        // Copied directly from PixelPerfect
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(0, 0));
        ImGui.Begin("Canvas", NoInputs | NoNav | NoTitleBar | NoScrollbar | NoBackground);
        ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);

        var xOffset = Math.Sin(Environment.TickCount / 500.0);
        var yOffset = Math.Sin(Environment.TickCount / 1500.0);
        var actorPos = this.clientState.LocalPlayer.Position;

        this.gameGui.WorldToScreen(
            new Vector3(actorPos.X + (float)xOffset, actorPos.Y, actorPos.Z + (float)yOffset),
            out var pos);

        ImGui.GetWindowDrawList().AddCircle(
            new Vector2(pos.X, pos.Y),
            2,
            ImGui.GetColorU32(new Vector4(255, 255, 255, 255)),
            100,
            10);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public void Dispose() { }
}
