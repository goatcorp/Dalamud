using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Data;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Data widget for browsing in-game icons.
/// </summary>
public class IconBrowserWidget : IDataWindowWidget
{
    // Remove range 170,000 -> 180,000 by default, this specific range causes exceptions.
    private readonly HashSet<int> nullValues = Enumerable.Range(170000, 9999).ToHashSet(); 
    
    private Vector2 iconSize = new(64.0f, 64.0f);
    private Vector2 editIconSize = new(64.0f, 64.0f);
    
    private List<int> valueRange = Enumerable.Range(0, 200000).ToList();
    
    private int lastNullValueCount;
    private int startRange;
    private int stopRange = 200000;
    private bool showTooltipImage;
    
    private Vector2 mouseDragStart;
    private bool dragStarted;
    private Vector2 lastWindowSize = Vector2.Zero;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "icon", "icons" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Icon Browser";

    /// <inheritdoc/>
    public bool Ready { get; set; } = true;

    /// <inheritdoc/>
    public void Load()
    {
    }
    
    /// <inheritdoc/>
    public void Draw()
    {
        this.DrawOptions();

        if (ImGui.BeginChild("ScrollableSection", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoMove))
        {
            var itemsPerRow = (int)MathF.Floor(ImGui.GetContentRegionMax().X / (this.iconSize.X + ImGui.GetStyle().ItemSpacing.X));
            var itemHeight = this.iconSize.Y + ImGui.GetStyle().ItemSpacing.Y;

            ImGuiClip.ClippedDraw(this.valueRange, this.DrawIcon, itemsPerRow, itemHeight);
        }
        
        ImGui.EndChild();
        
        this.ProcessMouseDragging();
        
        if (this.lastNullValueCount != this.nullValues.Count)
        {
            this.RecalculateIndexRange();
            this.lastNullValueCount = this.nullValues.Count;
        }
    }
    
    // Limit the popup image to half our screen size.
    private static float GetImageScaleFactor(IDalamudTextureWrap texture)
    {
        var workArea = ImGui.GetMainViewport().Size / 2.0f;
        var scale = 1.0f;

        if (texture.Width > workArea.X || texture.Height > workArea.Y)
        {
            var widthRatio = workArea.X / texture.Width;
            var heightRatio = workArea.Y / texture.Height;

            scale = MathF.Min(widthRatio, heightRatio);
        }
        
        return scale;
    }
    
    private void DrawOptions()
    {
        ImGui.Columns(2);

        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputInt("##StartRange", ref this.startRange, 0, 0)) this.RecalculateIndexRange();

        ImGui.NextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputInt("##StopRange", ref this.stopRange, 0, 0)) this.RecalculateIndexRange();

        ImGui.NextColumn();
        ImGui.Checkbox("Show Image in Tooltip", ref this.showTooltipImage);
        
        ImGui.NextColumn();
        ImGui.InputFloat2("Icon Size", ref this.editIconSize);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            this.iconSize = this.editIconSize;
        }
        
        ImGui.Columns(1);
    }
    
    private void DrawIcon(int iconId)
    {
        try
        {
            var cursor = ImGui.GetCursorScreenPos();
            
            if (!this.IsIconValid(iconId))
            {
                this.nullValues.Add(iconId);
                return;
            }
        
            if (Service<TextureManager>.Get().GetIcon((uint)iconId) is { } texture)
            {
                ImGui.Image(texture.ImGuiHandle, this.iconSize);
            
                // If we have the option to show a tooltip image, draw the image, but make sure it's not too big.
                if (ImGui.IsItemHovered() && this.showTooltipImage)
                {
                    ImGui.BeginTooltip();
            
                    var scale = GetImageScaleFactor(texture);
                    
                    var textSize = ImGui.CalcTextSize(iconId.ToString());
                    ImGui.SetCursorPosX(texture.Size.X * scale / 2.0f - textSize.X / 2.0f + ImGui.GetStyle().FramePadding.X * 2.0f);
                    ImGui.Text(iconId.ToString());
            
                    ImGui.Image(texture.ImGuiHandle, texture.Size * scale);
                    ImGui.EndTooltip();
                }
                
                // else, just draw the iconId.
                else if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(iconId.ToString());
                }
            }
            else
            {
                // This texture was null, draw nothing, and prevent from trying to show it again.
                this.nullValues.Add(iconId);
            }

            ImGui.GetWindowDrawList().AddRect(cursor, cursor + this.iconSize, ImGui.GetColorU32(ImGuiColors.DalamudWhite));
        }
        catch (Exception)
        {
            // If something went wrong, prevent from trying to show this icon again.
            this.nullValues.Add(iconId);
        }
    }

    private void ProcessMouseDragging()
    {
        if (ImGui.IsItemHovered() || this.dragStarted)
        {
            if (ImGui.GetWindowSize() == this.lastWindowSize)
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !this.dragStarted)
                {
                    this.mouseDragStart = ImGui.GetMousePos();
                    this.dragStarted = true;
                }
            }
            else
            {
                this.lastWindowSize = ImGui.GetWindowSize();
                this.dragStarted = false;
            }
        }
        
        if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && this.dragStarted)
        {
            var delta = this.mouseDragStart - ImGui.GetMousePos();
            ImGui.GetIO().AddMouseWheelEvent(0.0f, -delta.Y / 85.0f);
            this.mouseDragStart = ImGui.GetMousePos();
        }
        else if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            this.dragStarted = false;
        }
    }
    
    // Check if the icon has a valid filepath, and exists in the game data.
    private bool IsIconValid(int iconId)
    {
        var filePath = Service<TextureManager>.Get().GetIconPath((uint)iconId);
        return !filePath.IsNullOrEmpty() && Service<DataManager>.Get().FileExists(filePath);
    }

    private void RecalculateIndexRange()
    {
        if (this.stopRange <= this.startRange || this.stopRange <= 0 || this.startRange < 0)
        {
            this.valueRange = new List<int>();
        }
        else
        {
            this.valueRange = Enumerable.Range(this.startRange, this.stopRange - this.startRange).ToList();
            this.valueRange.RemoveAll(value => this.nullValues.Contains(value));
        }
    }
}
