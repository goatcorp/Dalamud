using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget to display FontAwesome Symbols.
/// </summary>
internal class FontAwesomeTestWidget : IDataWindowWidget
{
    private List<FontAwesomeIcon>? icons;
    private List<string>? iconNames;
    private string[]? iconCategories;
    private int selectedIconCategory;
    private string iconSearchInput = string.Empty;
    private bool iconSearchChanged = true;
    
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.FontAwesome_Test;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "fa", "fatest", "fontawesome" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font Awesome Test"; 

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
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        
        this.iconCategories ??= FontAwesomeHelpers.GetCategories();

        if (this.iconSearchChanged)
        {
            this.icons = FontAwesomeHelpers.SearchIcons(this.iconSearchInput, this.iconCategories[this.selectedIconCategory]);
            this.iconNames = this.icons.Select(icon => Enum.GetName(icon)!).ToList();
            this.iconSearchChanged = false;
        }

        ImGui.SetNextItemWidth(160f);
        var categoryIndex = this.selectedIconCategory;
        if (ImGui.Combo("####FontAwesomeCategorySearch", ref categoryIndex, this.iconCategories, this.iconCategories.Length))
        {
            this.selectedIconCategory = categoryIndex;
            this.iconSearchChanged = true;
        }

        ImGui.SameLine(170f);
        ImGui.SetNextItemWidth(180f);
        if (ImGui.InputTextWithHint($"###FontAwesomeInputSearch", "search icons", ref this.iconSearchInput, 50))
        {
            this.iconSearchChanged = true;
        }

        ImGuiHelpers.ScaledDummy(10f);
        for (var i = 0; i < this.icons?.Count; i++)
        {
            ImGui.Text($"0x{(int)this.icons[i].ToIconChar():X}");
            ImGuiHelpers.ScaledRelativeSameLine(50f);
            ImGui.Text($"{this.iconNames?[i]}");
            ImGuiHelpers.ScaledRelativeSameLine(280f);
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(this.icons[i].ToIconString());
            ImGui.PopFont();
            ImGuiHelpers.ScaledDummy(2f);
        }
        
        ImGui.PopStyleVar();
    }
}
