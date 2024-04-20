using Dalamud.Configuration.Internal;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Window responsible for hitch settings.
/// </summary>
public class HitchSettingsWindow : Window
{
    private const float MinHitch = 1;
    private const float MaxHitch = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="HitchSettingsWindow"/> class.
    /// </summary>
    public HitchSettingsWindow()
        : base("Hitch Settings", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.ShowCloseButton = true;
        this.RespectCloseHotkey = true;
    }
    
    /// <inheritdoc/>
    public override void Draw()
    {
        var config = Service<DalamudConfiguration>.Get();

        var uiBuilderHitch = (float)config.UiBuilderHitch;
        if (ImGui.SliderFloat("UiBuilderHitch", ref uiBuilderHitch, MinHitch, MaxHitch))
        {
            config.UiBuilderHitch = uiBuilderHitch;
            config.QueueSave();
        }

        var frameworkUpdateHitch = (float)config.FrameworkUpdateHitch;
        if (ImGui.SliderFloat("FrameworkUpdateHitch", ref frameworkUpdateHitch, MinHitch, MaxHitch))
        {
            config.FrameworkUpdateHitch = frameworkUpdateHitch;
            config.QueueSave();
        }
        
        var gameNetworkUpHitch = (float)config.GameNetworkUpHitch;
        if (ImGui.SliderFloat("GameNetworkUpHitch", ref gameNetworkUpHitch, MinHitch, MaxHitch))
        {
            config.GameNetworkUpHitch = gameNetworkUpHitch;
            config.QueueSave();
        }
        
        var gameNetworkDownHitch = (float)config.GameNetworkDownHitch;
        if (ImGui.SliderFloat("GameNetworkDownHitch", ref gameNetworkDownHitch, MinHitch, MaxHitch))
        {
            config.GameNetworkDownHitch = gameNetworkDownHitch;
            config.QueueSave();
        }
    }
}
