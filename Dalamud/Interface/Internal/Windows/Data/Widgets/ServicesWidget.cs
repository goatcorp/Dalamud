using System.Linq;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.IoC.Internal;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying start info.
/// </summary>
internal class ServicesWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "services" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Service Container"; 

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
        var container = Service<ServiceContainer>.Get();

        foreach (var instance in container.Instances)
        {
            var hasInterface = container.InterfaceToTypeMap.Values.Any(x => x == instance.Key);
            var isPublic = instance.Key.IsPublic;
            
            ImGui.BulletText($"{instance.Key.FullName} ({instance.Key.GetServiceKind()})");
            
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, !hasInterface))
            {
                ImGui.Text(hasInterface
                               ? $"\t => Provided via interface: {container.InterfaceToTypeMap.First(x => x.Value == instance.Key).Key.FullName}"
                               : "\t => NO INTERFACE!!!");
            }

            if (isPublic)
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.Text("\t => PUBLIC!!!");
            }
            
            ImGuiHelpers.ScaledDummy(2);
        }
    }
}
