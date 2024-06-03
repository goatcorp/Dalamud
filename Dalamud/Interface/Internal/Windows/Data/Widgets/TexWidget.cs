using System.Collections.Generic;
using System.IO;
using System.Numerics;

using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying texture test.
/// </summary>
internal class TexWidget : IDataWindowWidget
{
    private readonly List<IDalamudTextureWrap> addedTextures = new();
    
    private string iconId = "18";
    private bool hiRes = true;
    private bool hq = false;
    private bool keepAlive = false;
    private string inputTexPath = string.Empty;
    private Vector2 inputTexUv0 = Vector2.Zero;
    private Vector2 inputTexUv1 = Vector2.One;
    private Vector4 inputTintCol = Vector4.One;
    private Vector2 inputTexScale = Vector2.Zero;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "tex", "texture" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Tex"; 

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
        var texManager = Service<TextureManager>.Get();

        ImGui.InputText("Icon ID", ref this.iconId, 32);
        ImGui.Checkbox("HQ Item", ref this.hq);
        ImGui.Checkbox("Hi-Res", ref this.hiRes);
        ImGui.Checkbox("Keep alive", ref this.keepAlive);
        if (ImGui.Button("Load Icon"))
        {
            try
            {
                var flags = ITextureProvider.IconFlags.None;
                if (this.hq)
                    flags |= ITextureProvider.IconFlags.ItemHighQuality;

                if (this.hiRes)
                    flags |= ITextureProvider.IconFlags.HiRes;
                
                this.addedTextures.Add(texManager.GetIcon(uint.Parse(this.iconId), flags, keepAlive: this.keepAlive));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load tex");
            }
        }
        
        ImGui.Separator();
        ImGui.InputText("Tex Path", ref this.inputTexPath, 255);
        if (ImGui.Button("Load Tex"))
        {
            try
            {
                this.addedTextures.Add(texManager.GetTextureFromGame(this.inputTexPath, this.keepAlive));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load tex");
            }
        }
        
        if (ImGui.Button("Load File"))
        {
            try
            {
                this.addedTextures.Add(texManager.GetTextureFromFile(new FileInfo(this.inputTexPath), this.keepAlive));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load tex");
            }
        }
        
        ImGui.Separator();
        ImGui.InputFloat2("UV0", ref this.inputTexUv0);
        ImGui.InputFloat2("UV1", ref this.inputTexUv1);
        ImGui.InputFloat4("Tint", ref this.inputTintCol);
        ImGui.InputFloat2("Scale", ref this.inputTexScale);

        ImGuiHelpers.ScaledDummy(10);

        IDalamudTextureWrap? toRemove = null;
        for (var i = 0; i < this.addedTextures.Count; i++)
        {
            if (ImGui.CollapsingHeader($"Tex #{i}"))
            {
                var tex = this.addedTextures[i];

                var scale = new Vector2(tex.Width, tex.Height);
                if (this.inputTexScale != Vector2.Zero)
                    scale = this.inputTexScale;
                    
                ImGui.Image(tex.ImGuiHandle, scale, this.inputTexUv0, this.inputTexUv1, this.inputTintCol);

                if (ImGui.Button($"X##{i}"))
                    toRemove = tex;

                ImGui.SameLine();
                if (ImGui.Button($"Clone##{i}"))
                    this.addedTextures.Add(tex.CreateWrapSharingLowLevelResource());
            }
        }

        if (toRemove != null)
        {
            toRemove.Dispose();
            this.addedTextures.Remove(toRemove);
        }
    }
}
