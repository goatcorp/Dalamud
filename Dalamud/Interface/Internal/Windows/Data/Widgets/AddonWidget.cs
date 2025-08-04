using Dalamud.Bindings.ImGui;
using Dalamud.Memory;
using Dalamud.Game.NativeWrapper;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying Addon Data.
/// </summary>
internal unsafe class AddonWidget : IDataWindowWidget
{
    private string inputAddonName = string.Empty;
    private int inputAddonIndex;
    private AgentInterfacePtr agentInterfacePtr;

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

        var addon = gameGui.GetAddonByName(this.inputAddonName, this.inputAddonIndex);
        if (addon.IsNull)
        {
            ImGui.Text("Null");
            return;
        }

        ImGui.TextUnformatted($"{addon.Name} - {Util.DescribeAddress(addon)}\n    v:{addon.IsVisible} x:{addon.X} y:{addon.Y} s:{addon.Scale}, w:{addon.Width}, h:{addon.Height}");

        if (ImGui.Button("Find Agent"))
        {
            this.agentInterfacePtr = gameGui.FindAgentInterface(addon);
        }

        if (!this.agentInterfacePtr.IsNull)
        {
            ImGui.TextUnformatted($"Agent: {Util.DescribeAddress(this.agentInterfacePtr)}");
            ImGui.SameLine();

            if (ImGui.Button("C"))
                ImGui.SetClipboardText(this.agentInterfacePtr.Address.ToString("X"));
        }
    }
}
