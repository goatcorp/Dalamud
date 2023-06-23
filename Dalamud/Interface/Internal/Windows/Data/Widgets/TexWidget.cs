using System;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying texture test.
/// </summary>
internal class TexWidget : IDataWindowWidget
{
    private string inputTexPath = string.Empty;
    private TextureWrap? debugTex;
    private Vector2 inputTexUv0 = Vector2.Zero;
    private Vector2 inputTexUv1 = Vector2.One;
    private Vector4 inputTintCol = Vector4.One;
    private Vector2 inputTexScale = Vector2.Zero;
    
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.Tex;

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
        var dataManager = Service<DataManager>.Get();

        ImGui.InputText("Tex Path", ref this.inputTexPath, 255);
        ImGui.InputFloat2("UV0", ref this.inputTexUv0);
        ImGui.InputFloat2("UV1", ref this.inputTexUv1);
        ImGui.InputFloat4("Tint", ref this.inputTintCol);
        ImGui.InputFloat2("Scale", ref this.inputTexScale);

        if (ImGui.Button("Load Tex"))
        {
            try
            {
                this.debugTex = dataManager.GetImGuiTexture(this.inputTexPath);
                this.inputTexScale = new Vector2(this.debugTex?.Width ?? 0, this.debugTex?.Height ?? 0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load tex");
            }
        }

        ImGuiHelpers.ScaledDummy(10);

        if (this.debugTex != null)
        {
            ImGui.Image(this.debugTex.ImGuiHandle, this.inputTexScale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);
            ImGuiHelpers.ScaledDummy(5);
            Util.ShowObject(this.debugTex);
        }
    }
}
