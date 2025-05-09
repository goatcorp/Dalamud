using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Data widget for browsing in-game icons.
/// </summary>
public class IconBrowserWidget : IDataWindowWidget
{
    private const int MaxIconId = 250_000;

    private Vector2 iconSize = new(64.0f, 64.0f);
    private Vector2 editIconSize = new(64.0f, 64.0f);

    private List<int>? valueRange;
    private Task<List<(int ItemId, string Path)>>? iconIdsTask;

    private int startRange;
    private int stopRange = MaxIconId;
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
        this.iconIdsTask ??= Task.Run(
            () =>
            {
                var texm = Service<TextureManager>.Get();

                var result = new List<(int ItemId, string Path)>(MaxIconId);
                for (var iconId = 0; iconId < MaxIconId; iconId++)
                {
                    // // Remove range 170,000 -> 180,000 by default, this specific range causes exceptions.
                    // if (iconId is >= 170000 and < 180000)
                    //     continue;
                    if (!texm.TryGetIconPath(new((uint)iconId), out var path))
                        continue;
                    result.Add((iconId, path));
                }

                return result;
            });

        this.DrawOptions();

        if (!this.iconIdsTask.IsCompleted)
        {
            ImGui.TextUnformatted("Loading...");
        }
        else if (!this.iconIdsTask.IsCompletedSuccessfully)
        {
            ImGui.TextUnformatted(this.iconIdsTask.Exception?.ToString() ?? "Unknown error");
        }
        else
        {
            this.RecalculateIndexRange();

            if (ImGui.BeginChild("ScrollableSection", ImGui.GetContentRegionAvail(), false, ImGuiWindowFlags.NoMove))
            {
                var itemsPerRow = (int)MathF.Floor(
                    ImGui.GetContentRegionMax().X / (this.iconSize.X + ImGui.GetStyle().ItemSpacing.X));
                var itemHeight = this.iconSize.Y + ImGui.GetStyle().ItemSpacing.Y;

                ImGuiClip.ClippedDraw(this.valueRange!, this.DrawIcon, itemsPerRow, itemHeight);
            }

            ImGui.EndChild();

            this.ProcessMouseDragging();
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
        if (ImGui.InputInt("##StartRange", ref this.startRange, 0, 0))
        {
            this.startRange = Math.Clamp(this.startRange, 0, MaxIconId);
            this.valueRange = null;
        }

        ImGui.NextColumn();
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X);
        if (ImGui.InputInt("##StopRange", ref this.stopRange, 0, 0))
        {
            this.stopRange = Math.Clamp(this.stopRange, 0, MaxIconId);
            this.valueRange = null;
        }

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
        var texm = Service<TextureManager>.Get();
        var cursor = ImGui.GetCursorScreenPos();

        if (texm.Shared.GetFromGameIcon(iconId).TryGetWrap(out var texture, out var exc))
        {
            ImGui.Image(texture.ImGuiHandle, this.iconSize);

            // If we have the option to show a tooltip image, draw the image, but make sure it's not too big.
            if (ImGui.IsItemHovered() && this.showTooltipImage)
            {
                ImGui.BeginTooltip();

                var scale = GetImageScaleFactor(texture);

                var textSize = ImGui.CalcTextSize(iconId.ToString());
                ImGui.SetCursorPosX(
                    texture.Size.X * scale / 2.0f - textSize.X / 2.0f + ImGui.GetStyle().FramePadding.X * 2.0f);
                ImGui.Text(iconId.ToString());

                ImGui.Image(texture.ImGuiHandle, texture.Size * scale);
                ImGui.EndTooltip();
            }

            // else, just draw the iconId.
            else if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(iconId.ToString());
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                    this.DisplayName,
                    iconId.ToString(),
                    Task.FromResult(texture.CreateWrapSharingLowLevelResource()));
            }

            ImGui.GetWindowDrawList().AddRect(
                cursor,
                cursor + this.iconSize,
                ImGui.GetColorU32(ImGuiColors.DalamudWhite));
        }
        else if (exc is not null)
        {
            ImGui.Dummy(this.iconSize);
            using (Service<InterfaceManager>.Get().IconFontHandle?.Push())
            {
                var iconText = FontAwesomeIcon.Ban.ToIconString();
                var textSize = ImGui.CalcTextSize(iconText);
                ImGui.GetWindowDrawList().AddText(
                    cursor + ((this.iconSize - textSize) / 2),
                    ImGui.GetColorU32(ImGuiColors.DalamudRed),
                    iconText);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip($"{iconId}\n{exc}".Replace("%", "%%"));

            ImGui.GetWindowDrawList().AddRect(
                cursor,
                cursor + this.iconSize,
                ImGui.GetColorU32(ImGuiColors.DalamudRed));
        }
        else
        {
            const uint color = 0x50FFFFFFu;
            const string text = "...";

            ImGui.Dummy(this.iconSize);
            var textSize = ImGui.CalcTextSize(text);
            ImGui.GetWindowDrawList().AddText(
                    cursor + ((this.iconSize - textSize) / 2),
                    color,
                    text);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(iconId.ToString());

            ImGui.GetWindowDrawList().AddRect(
                cursor,
                cursor + this.iconSize,
                color);
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

    private void RecalculateIndexRange()
    {
        if (this.valueRange is not null)
            return;

        this.valueRange = new();
        foreach (var (id, _) in this.iconIdsTask!.Result)
        {
            if (this.startRange <= id && id < this.stopRange)
                this.valueRange.Add(id);
        }
    }
}
