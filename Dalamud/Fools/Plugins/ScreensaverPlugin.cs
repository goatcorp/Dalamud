using System;
using System.IO;
using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using ImGuiNET;
using ImGuiScene;
using static ImGuiNET.ImGuiWindowFlags;

namespace Dalamud.Fools.Plugins;

public class ScreensaverPlugin : IFoolsPlugin
{
    private readonly TextureWrap logoTexture;
    private readonly Condition condition;

    private int x;
    private int y;

    private bool xDir = true;
    private bool yDir = true;

    private double lastTime;

    public ScreensaverPlugin()
    {
        var interfaceManager = Service<InterfaceManager>.Get();
        var dalamud = Service<Dalamud>.Get();
        this.condition = Service<Condition>.Get();

        this.logoTexture =
            interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"))!;
    }

    public void DrawUi()
    {
        var time = Environment.TickCount64 / 1000.0;
        var diff = time - this.lastTime;
        diff = diff > 1 ? 1 : diff;
        this.lastTime = time;

        if (!this.condition[ConditionFlag.BetweenAreas])
        {
            return;
        }

        var textureSize = new Vector2(100);
        var maxSize = ImGui.GetMainViewport().Size - textureSize;
        this.xDir = this.xDir ? this.x < maxSize.X : this.x > 0;
        this.yDir = this.yDir ? this.y < maxSize.Y : this.y > 0;

        this.x += (int)(diff * (this.xDir ? 1 : -1) * 100);
        this.y += (int)(diff * (this.yDir ? 1 : -1) * 100);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(this.x, this.y));
        ImGui.Begin("Screensaver", NoInputs | NoNav | NoTitleBar | NoScrollbar | NoBackground);
        ImGui.SetWindowSize(textureSize);
        ImGui.Image(this.logoTexture.ImGuiHandle, textureSize);

        ImGui.End();
        ImGui.PopStyleVar();
    }

    public void Dispose()
    {
        this.logoTexture.Dispose();
    }
}
