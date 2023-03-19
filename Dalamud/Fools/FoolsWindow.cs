using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Fools;

// Stolen from ChangelogWindow
public class FoolsWindow : Window, IDisposable {
    private readonly TextureWrap logoTexture;

    public FoolsWindow()
        : base("Introducing Alternate Reality Dalamud", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoResize)
    {
        this.Namespace = "FoolsWindow";

        this.Size = new Vector2(885, 463);
        this.SizeCondition = ImGuiCond.Appearing;

        var interfaceManager = Service<InterfaceManager>.Get();
        var dalamud = Service<Dalamud>.Get();

        this.logoTexture =
            interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"))!;
    }

    public override void Draw()
    {
        var imgCursor = ImGui.GetCursorPos();
        ImGui.TextWrapped(@"
A team of scientists and plugin developers have collaborated to create a new
version of Dalamud that includes plugins from other realities.

With our high tech systems, the plugin installer will now offer new, unique
plugins with endless possibilities. Open the ""Alternate Reality"" tab in the
plugin installer to see what's available.

We hope you enjoy this new version of Dalamud, and we look forward to your feedback.
".Trim());

        if (ImGui.Button("Open the plugin installer"))
        {
            var di = Service<DalamudInterface>.Get();
            di.OpenPluginInstallerFools();
        }

        imgCursor.X += 500;
        ImGui.SetCursorPos(imgCursor);

        ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(100));
    }

    public void Dispose()
    {
        this.logoTexture.Dispose();
    }
}
