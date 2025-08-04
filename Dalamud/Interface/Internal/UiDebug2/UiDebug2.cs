using System.Collections.Generic;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui;
using Dalamud.Interface.Internal.UiDebug2.Browsing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Logging.Internal;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Bindings.ImGui.ImGuiWindowFlags;

namespace Dalamud.Interface.Internal.UiDebug2;

// Original version by aers https://github.com/aers/FFXIVUIDebug
// Also incorporates features from Caraxi's fork https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Debugging/UIDebug.cs

/// <summary>
/// A tool for browsing the contents and structure of UI elements.
/// </summary>
internal partial class UiDebug2 : IDisposable
{
    private readonly ElementSelector elementSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="UiDebug2"/> class.
    /// </summary>
    internal UiDebug2()
    {
        this.elementSelector = new(this);
    }

    /// <inheritdoc cref="ModuleLog"/>
    internal static ModuleLog Log { get; set; } = new("UiDebug2");

    /// <inheritdoc cref="IGameGui"/>
    internal static IGameGui GameGui { get; set; } = Service<GameGui>.Get();

    /// <summary>
    /// Gets a collection of <see cref="AddonTree"/> instances, each representing an <see cref="FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase"/>.
    /// </summary>
    internal static Dictionary<string, AddonTree> AddonTrees { get; } = [];

    /// <summary>
    /// Gets or sets a window system to handle any popout windows for addons or nodes.
    /// </summary>
    internal static WindowSystem PopoutWindows { get; set; } = new("UiDebugPopouts");

    /// <summary>
    /// Gets or sets the name of the currently-selected <see cref="AtkUnitBase"/>.
    /// </summary>
    internal string? SelectedAddonName { get; set; }

    /// <summary>
    /// Clears all windows and <see cref="AddonTree"/>s.
    /// </summary>
    public void Dispose()
    {
        foreach (var a in AddonTrees)
        {
            a.Value.Dispose();
        }

        AddonTrees.Clear();
        PopoutWindows.RemoveAllWindows();
        this.elementSelector.Dispose();
    }

    /// <summary>
    /// Draws the UiDebug tool's interface and contents.
    /// </summary>
    internal void Draw()
    {
        PopoutWindows.Draw();
        this.DrawSidebar();
        this.DrawMainPanel();
    }

    private void DrawMainPanel()
    {
        ImGui.SameLine();

        using var ch = ImRaii.Child("###uiDebugMainPanel", new(-1, -1), true, HorizontalScrollbar);

        if (ch.Success)
        {
            if (this.elementSelector.Active)
            {
                this.elementSelector.DrawSelectorOutput();
            }
            else
            {
                if (this.SelectedAddonName != null)
                {
                    var addonTree = AddonTree.GetOrCreate(this.SelectedAddonName);

                    if (addonTree == null)
                    {
                        this.SelectedAddonName = null;
                        return;
                    }

                    addonTree.Draw();
                }
            }
        }
    }
}
