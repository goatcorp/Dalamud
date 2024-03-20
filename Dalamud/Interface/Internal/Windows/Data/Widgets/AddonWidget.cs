using Dalamud.Game.Gui;
using Dalamud.Memory;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying Addon Data.
/// </summary>
internal unsafe class AddonWidget : IDataWindowWidget
{
    private string inputAddonName = string.Empty;
    private int inputAddonIndex;
    private nint findAgentInterfacePtr;

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Addon"; 

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; }

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
        var gameGui = Service<GameGui>.Get();

        ImGui.InputText("Addon Name", ref this.inputAddonName, 256);
        ImGui.InputInt("Addon Index", ref this.inputAddonIndex);

        if (this.inputAddonName.IsNullOrEmpty())
            return;

        var address = gameGui.GetAddonByName(this.inputAddonName, this.inputAddonIndex);

        if (address == nint.Zero)
        {
            ImGui.Text("Null");
            return;
        }

        var addon = (FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase*)address;
        var name = MemoryHelper.ReadStringNullTerminated((nint)addon->Name);
        ImGui.TextUnformatted($"{name} - 0x{address.ToInt64():X}\n    v:{addon->IsVisible} x:{addon->X} y:{addon->Y} s:{addon->Scale}, w:{addon->RootNode->Width}, h:{addon->RootNode->Height}");

        if (ImGui.Button("Find Agent"))
        {
            this.findAgentInterfacePtr = gameGui.FindAgentInterface(address);
        }

        if (this.findAgentInterfacePtr != nint.Zero)
        {
            ImGui.TextUnformatted($"Agent: 0x{this.findAgentInterfacePtr.ToInt64():X}");
            ImGui.SameLine();

            if (ImGui.Button("C"))
                ImGui.SetClipboardText(this.findAgentInterfacePtr.ToInt64().ToString("X"));
        }
    }
}
