using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static System.StringComparison;
using static Dalamud.Interface.FontAwesomeIcon;

namespace Dalamud.Interface.Internal.UiDebug2;

/// <inheritdoc cref="UiDebug2"/>
internal unsafe partial class UiDebug2
{
    /// <summary>
    /// All unit lists to check for addons.
    /// </summary>
    internal static readonly List<UnitListOption> UnitListOptions =
    [
        new(13, "Loaded"),
        new(14, "Focused"),
        new(0, "Depth Layer 1"),
        new(1, "Depth Layer 2"),
        new(2, "Depth Layer 3"),
        new(3, "Depth Layer 4"),
        new(4, "Depth Layer 5"),
        new(5, "Depth Layer 6"),
        new(6, "Depth Layer 7"),
        new(7, "Depth Layer 8"),
        new(8, "Depth Layer 9"),
        new(9, "Depth Layer 10"),
        new(10, "Depth Layer 11"),
        new(11, "Depth Layer 12"),
        new(12, "Depth Layer 13"),
        new(15, "Units 16"),
        new(16, "Units 17"),
        new(17, "Units 18")
    ];

    private string addonNameSearch = string.Empty;

    private bool visFilter;

    /// <summary>
    /// Gets the base address for all unit lists.
    /// </summary>
    /// <returns>The address, if found.</returns>
    internal static AtkUnitList* GetUnitListBaseAddr() => &((UIModule*)GameGui.GetUIModule())->GetRaptureAtkModule()->RaptureAtkUnitManager.AtkUnitManager.DepthLayerOneList;

    private void DrawSidebar()
    {
        using (ImRaii.Group())
        {
            this.DrawNameSearch();
            this.DrawAddonSelectionList();
            this.elementSelector.DrawInterface();
        }
    }

    private void DrawNameSearch()
    {
        using (ImRaii.Child("###sidebar_nameSearch", new(250, 40), true))
        {
            var atkUnitBaseSearch = this.addonNameSearch;

            Vector4? defaultColor = this.visFilter ? new(0.0f, 0.8f, 0.2f, 1f) : new Vector4(0.6f, 0.6f, 0.6f, 1);
            if (ImGuiComponents.IconButton("filter", LowVision, defaultColor))
            {
                this.visFilter = !this.visFilter;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Filter by visibility");
            }

            ImGui.SameLine();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.InputTextWithHint("###atkUnitBaseSearch", "Filter by name", ref atkUnitBaseSearch, 0x20))
            {
                this.addonNameSearch = atkUnitBaseSearch;
            }
        }
    }

    private void DrawAddonSelectionList()
    {
        using (ImRaii.Child("###sideBar_addonList", new(250, -44), true, ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            var unitListBaseAddr = GetUnitListBaseAddr();

            foreach (var unit in UnitListOptions)
            {
                this.DrawUnitListOption(unitListBaseAddr, unit);
            }

            ;
        }
    }

    private void DrawUnitListOption(AtkUnitList* unitListBaseAddr, UnitListOption unit)
    {
        var atkUnitList = &unitListBaseAddr[unit.Index];
        var safeLength = Math.Min(atkUnitList->Count, atkUnitList->Entries.Length);

        var options = new List<AddonOption>();
        var totalCount = 0;
        var matchCount = 0;
        var anyVisible = false;

        var usingFilter = this.visFilter || !string.IsNullOrEmpty(this.addonNameSearch);

        for (var i = 0; i < safeLength; i++)
        {
            var addon = atkUnitList->Entries[i].Value;

            if (addon == null)
            {
                continue;
            }

            totalCount++;

            if (this.visFilter && !addon->IsVisible)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(this.addonNameSearch) && !addon->NameString.Contains(this.addonNameSearch, InvariantCultureIgnoreCase))
            {
                continue;
            }

            matchCount++;
            anyVisible |= addon->IsVisible;
            options.Add(new AddonOption(addon->NameString, addon->IsVisible));
        }

        if (matchCount == 0)
        {
            return;
        }

        var countStr = $"{(usingFilter ? $"{matchCount}/" : string.Empty)}{totalCount}";

        using var col1 = ImRaii.PushColor(ImGuiCol.Text, anyVisible ? new Vector4(1) : new Vector4(0.6f, 0.6f, 0.6f, 1));
        using var tree = ImRaii.TreeNode($"{unit.Name} [{countStr}]###unitListTree{unit.Index}");
        col1.Pop();

        if (tree)
        {
            foreach (var option in options)
            {
                using (ImRaii.PushColor(ImGuiCol.Text, option.Visible ? new Vector4(0.1f, 1f, 0.1f, 1f) : new Vector4(0.6f, 0.6f, 0.6f, 1)))
                {
                    if (ImGui.Selectable($"{option.Name}##select{option.Name}", this.SelectedAddonName == option.Name))
                    {
                        this.SelectedAddonName = option.Name;
                    }
                }
            }
        }
    }

    /// <summary>
    /// A struct representing a unit list that can be browed in the sidebar.
    /// </summary>
    internal struct UnitListOption
    {
        /// <summary>The index of the unit list.</summary>
        internal uint Index;

        /// <summary>The name of the unit list.</summary>
        internal string Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitListOption"/> struct.
        /// </summary>
        /// <param name="i">The index of the unit list.</param>
        /// <param name="name">The name of the unit list.</param>
        internal UnitListOption(uint i, string name)
        {
            this.Index = i;
            this.Name = name;
        }
    }

    /// <summary>
    /// A struct representing an addon that can be selected in the sidebar.
    /// </summary>
    internal struct AddonOption
    {
        /// <summary>The name of the addon.</summary>
        internal string Name;

        /// <summary>Whether the addon is visible.</summary>
        internal bool Visible;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddonOption"/> struct.
        /// </summary>
        /// <param name="name">The name of the addon.</param>
        /// <param name="visible">Whether the addon is visible.</param>
        internal AddonOption(string name, bool visible)
        {
            this.Name = name;
            this.Visible = visible;
        }
    }
}
