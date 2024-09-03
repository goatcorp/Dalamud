using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;

using static Dalamud.Interface.FontAwesomeIcon;
using static Dalamud.Interface.Internal.UiDebug2.ElementSelector;
using static Dalamud.Interface.Internal.UiDebug2.UiDebug2;
using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A class representing an <see cref="AtkUnitBase"/>, allowing it to be browsed within an ImGui window.
/// </summary>
public unsafe partial class AddonTree : IDisposable
{
    private readonly nint initialPtr;

    private AddonPopoutWindow? window;

    private AddonTree(string name, nint ptr)
    {
        this.AddonName = name;
        this.initialPtr = ptr;
        this.PopulateFieldNames(ptr);
    }

    /// <summary>
    /// Gets the name of the addon this tree represents.
    /// </summary>
    internal string AddonName { get; init; }

    /// <summary>
    /// Gets or sets a collection of trees representing nodes within this addon.
    /// </summary>
    internal Dictionary<nint, ResNodeTree> NodeTrees { get; set; } = [];

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var nodeTree in this.NodeTrees)
        {
            nodeTree.Value.Dispose();
        }

        this.NodeTrees.Clear();
        this.FieldNames.Clear();
        AddonTrees.Remove(this.AddonName);
        if (this.window != null && PopoutWindows.Windows.Contains(this.window))
        {
            PopoutWindows.RemoveWindow(this.window);
            this.window?.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets an instance of <see cref="AddonTree"/> for the given addon name (or creates one if none are found).
    /// The tree can then be drawn within the Addon Inspector and browsed.
    /// </summary>
    /// <param name="name">The name of the addon.</param>
    /// <returns>The <see cref="AddonTree"/> for the named addon. Returns null if it does not exist, or if it is not at the expected address.</returns>
    internal static AddonTree? GetOrCreate(string? name)
    {
        if (name == null)
        {
            return null;
        }

        try
        {
            var ptr = GameGui.GetAddonByName(name);

            if ((AtkUnitBase*)ptr != null)
            {
                if (AddonTrees.TryGetValue(name, out var tree))
                {
                    if (tree.initialPtr == ptr)
                    {
                        return tree;
                    }

                    tree.Dispose();
                }

                var newTree = new AddonTree(name, ptr);
                AddonTrees.Add(name, newTree);
                return newTree;
            }
        }
        catch
        {
            Log.Warning("Couldn't get addon!");
        }

        return null;
    }

    /// <summary>
    /// Draws this AddonTree within a window.
    /// </summary>
    internal void Draw()
    {
        if (!this.ValidateAddon(out var addon))
        {
            return;
        }

        var isVisible = addon->IsVisible;

        ImGui.Text($"{this.AddonName}");
        ImGui.SameLine();

        ImGui.SameLine();
        ImGui.TextColored(isVisible ? new(0.1f, 1f, 0.1f, 1f) : new(0.6f, 0.6f, 0.6f, 1), isVisible ? "Visible" : "Not Visible");

        ImGui.SameLine(ImGui.GetWindowWidth() - 100);

        if (ImGuiComponents.IconButton($"##vis{(nint)addon:X}", isVisible ? Eye : EyeSlash, isVisible ? new(0.0f, 0.8f, 0.2f, 1f) : new Vector4(0.6f, 0.6f, 0.6f, 1)))
        {
            addon->IsVisible = !isVisible;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Visibility");
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton("pop", this.window?.IsOpen == true ? Times : ArrowUpRightFromSquare, null))
        {
            this.TogglePopout();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggle Popout Window");
        }

        ImGui.Separator();

        PrintFieldValuePair("Address", $"{(nint)addon:X}");

        var uldManager = addon->UldManager;
        PrintFieldValuePairs(
            ("X", $"{addon->X}"),
            ("Y", $"{addon->X}"),
            ("Scale", $"{addon->Scale}"),
            ("Widget Count", $"{uldManager.ObjectCount}"));

        ImGui.Separator();

        var addonObj = this.GetAddonObj(addon);
        if (addonObj != null)
        {
            ShowStruct(addonObj, (ulong)addon);
        }

        ImGui.Dummy(new(25 * ImGui.GetIO().FontGlobalScale));
        ImGui.Separator();

        ResNodeTree.PrintNodeList(uldManager.NodeList, uldManager.NodeListCount, this);

        ImGui.Dummy(new(25 * ImGui.GetIO().FontGlobalScale));
        ImGui.Separator();

        ResNodeTree.PrintNodeListAsTree(addon->CollisionNodeList, (int)addon->CollisionNodeListCount, "Collision List", this, new(0.5F, 0.7F, 1F, 1F));

        if (SearchResults.Length > 0 && Countdown <= 0)
        {
            SearchResults = [];
        }
    }

    /// <summary>
    /// Checks whether a given <see cref="AtkResNode"/> exists somewhere within this <see cref="AddonTree"/>'s associated <see cref="AtkUnitBase"/> (or any of its <see cref="AtkComponentNode"/>s).
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>true if the node was found.</returns>
    internal bool ContainsNode(AtkResNode* node) => this.ValidateAddon(out var addon) && FindNode(node, addon);

    private static bool FindNode(AtkResNode* node, AtkUnitBase* addon) => addon != null && FindNode(node, addon->UldManager);

    private static bool FindNode(AtkResNode* node, AtkComponentNode* compNode) => compNode != null && FindNode(node, compNode->Component->UldManager);

    private static bool FindNode(AtkResNode* node, AtkUldManager uldManager) => FindNode(node, uldManager.NodeList, uldManager.NodeListCount);

    private static bool FindNode(AtkResNode* node, AtkResNode** list, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var listNode = list[i];
            if (listNode == node)
            {
                return true;
            }

            if ((int)listNode->Type >= 1000 && FindNode(node, (AtkComponentNode*)listNode))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the addon exists at the expected address. If the addon is null or has a new address, disposes this instance of <see cref="AddonTree"/>.
    /// </summary>
    /// <param name="addon">The addon, if successfully found.</param>
    /// <returns>true if the addon is found.</returns>
    private bool ValidateAddon(out AtkUnitBase* addon)
    {
        addon = (AtkUnitBase*)GameGui.GetAddonByName(this.AddonName);
        if (addon == null || (nint)addon != this.initialPtr)
        {
            this.Dispose();
            return false;
        }

        return true;
    }

    private void TogglePopout()
    {
        if (this.window == null)
        {
            this.window = new AddonPopoutWindow(this, $"{this.AddonName}###addonPopout{this.initialPtr}");
            PopoutWindows.AddWindow(this.window);
        }
        else
        {
            this.window.IsOpen = !this.window.IsOpen;
        }
    }
}
