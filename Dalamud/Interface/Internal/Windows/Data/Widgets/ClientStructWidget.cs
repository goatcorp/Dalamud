using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Info;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget to display resolved CS addresses
/// </summary>
internal class ClientStructWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["clientstruct"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "ClientStruct";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        ImGui.TextColored(ImGuiColors.DalamudOrange, "InfoProxies");
        var uiModule = UIModule.Instance();
        if (uiModule == null)
        {
            ImGui.TextUnformatted("Unable to load UiModule");
            return;
        }

        var infoModule = uiModule->GetInfoModule();
        if (infoModule == null)
        {
            ImGui.TextUnformatted("Unable to load InfoModule");
            return;
        }

        using var table = ImRaii.Table("##addressTable", 2, ImGuiTableFlags.BordersInner, new Vector2(ImGui.GetWindowContentRegionMax().X / 2.5f, 0));
        if (!table.Success)
            return;

        ImGui.TableSetupColumn("Name");
        ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed);

        ImGui.TableHeadersRow();
        foreach (var proxy in Enum.GetValues<InfoProxyId>())
        {
            var resolvedProxy = infoModule->GetInfoProxyById(proxy);

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{Enum.GetName(proxy)}");

            var address = $"{(nint)resolvedProxy:X}";
            ImGui.TableNextColumn();
            if (ImGui.Selectable(address))
                ImGui.SetClipboardText(address);
        }
    }
}
