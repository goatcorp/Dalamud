using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying target info.
/// </summary>
internal class TargetWidget : IDataWindowWidget
{
    private bool resolveGameData;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "target" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Target"; 

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
        ImGui.Checkbox("Resolve GameData", ref this.resolveGameData);
        
        var clientState = Service<ClientState>.Get();
        var targetMgr = Service<TargetManager>.Get();

        if (targetMgr.Target != null)
        {
            Util.PrintGameObject(targetMgr.Target, "CurrentTarget", this.resolveGameData);

            ImGui.Text("Target");
            Util.ShowGameObjectStruct(targetMgr.Target);

            var tot = targetMgr.Target.TargetObject;
            if (tot != null)
            {
                ImGuiHelpers.ScaledDummy(10);

                ImGui.Separator();
                ImGui.Text("ToT");
                Util.ShowGameObjectStruct(tot);
            }

            ImGuiHelpers.ScaledDummy(10);
        }

        if (targetMgr.FocusTarget != null)
            Util.PrintGameObject(targetMgr.FocusTarget, "FocusTarget", this.resolveGameData);

        if (targetMgr.MouseOverTarget != null)
            Util.PrintGameObject(targetMgr.MouseOverTarget, "MouseOverTarget", this.resolveGameData);

        if (targetMgr.PreviousTarget != null)
            Util.PrintGameObject(targetMgr.PreviousTarget, "PreviousTarget", this.resolveGameData);

        if (targetMgr.SoftTarget != null)
            Util.PrintGameObject(targetMgr.SoftTarget, "SoftTarget", this.resolveGameData);
        
        if (targetMgr.GPoseTarget != null)
            Util.PrintGameObject(targetMgr.GPoseTarget, "GPoseTarget", this.resolveGameData);
        
        if (targetMgr.MouseOverNameplateTarget != null)
            Util.PrintGameObject(targetMgr.MouseOverNameplateTarget, "MouseOverNameplateTarget", this.resolveGameData);

        if (ImGui.Button("Clear CT"))
            targetMgr.Target = null;

        if (ImGui.Button("Clear FT"))
            targetMgr.FocusTarget = null;

        var localPlayer = clientState.LocalPlayer;

        if (localPlayer != null)
        {
            if (ImGui.Button("Set CT"))
                targetMgr.Target = localPlayer;

            if (ImGui.Button("Set FT"))
                targetMgr.FocusTarget = localPlayer;
        }
        else
        {
            ImGui.Text("LocalPlayer is null.");
        }
    }
}
