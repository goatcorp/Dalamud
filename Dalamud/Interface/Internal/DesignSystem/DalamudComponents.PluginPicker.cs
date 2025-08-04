using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.DesignSystem;

/// <summary>
/// Private ImGui widgets for use inside Dalamud.
/// </summary>
internal static partial class DalamudComponents
{
    /// <summary>
    /// Draw a "picker" popup to chose a plugin.
    /// </summary>
    /// <param name="id">The ID of the popup.</param>
    /// <param name="pickerSearch">String holding the search input.</param>
    /// <param name="onClicked">Action to be called if a plugin is clicked.</param>
    /// <param name="pluginDisabled">Function that should return true if a plugin should show as disabled.</param>
    /// <param name="pluginFiltered">Function that should return true if a plugin should not appear in the list.</param>
    /// <returns>An ImGuiID to open the popup.</returns>
    internal static uint DrawPluginPicker(string id, ref string pickerSearch, Action<LocalPlugin> onClicked, Func<LocalPlugin, bool> pluginDisabled, Func<LocalPlugin, bool>? pluginFiltered = null)
    {
        var pm = Service<PluginManager>.GetNullable();
        if (pm == null)
            return 0;

        var addPluginToProfilePopupId = ImGui.GetID(id);
        using var popup = ImRaii.Popup(id);

        if (popup.Success)
        {
            var width = ImGuiHelpers.GlobalScale * 300;

            ImGui.SetNextItemWidth(width);
            ImGui.InputTextWithHint("###pluginPickerSearch"u8, Locs.SearchHint, ref pickerSearch, 255);

            var currentSearchString = pickerSearch;

            using var listBox = ImRaii.ListBox("###pluginPicker"u8, new Vector2(width, width - 80));
            if (listBox.Success)
            {
                // TODO: Plugin searching should be abstracted... installer and this should use the same search
                var plugins = pm.InstalledPlugins.Where(
                                    x => x.Manifest.SupportsProfiles &&
                                         (currentSearchString.IsNullOrWhitespace() || x.Manifest.Name.Contains(
                                              currentSearchString,
                                              StringComparison.InvariantCultureIgnoreCase)))
                                .Where(pluginFiltered ?? (_ => true));

                foreach (var plugin in plugins)
                {
                    using var disabled2 = ImRaii.Disabled(pluginDisabled(plugin));
                    if (ImGui.Selectable($"{plugin.Manifest.Name}{(plugin is LocalDevPlugin ? "(dev plugin)" : string.Empty)}###selector{plugin.Manifest.InternalName}"))
                    {
                        onClicked(plugin);
                    }
                }
            }
        }

        return addPluginToProfilePopupId;
    }

    private static partial class Locs
    {
        public static string SearchHint => Loc.Localize("ProfileManagerSearchHint", "Search...");
    }
}
