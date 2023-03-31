using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Raii;
using Dalamud.Plugin.Internal;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class DevPluginsSettingsEntry : SettingsEntry
{
    private List<DevPluginLocationSettings> devPluginLocations = new();
    private bool devPluginLocationsChanged;
    private string devPluginTempLocation = string.Empty;
    private string devPluginLocationAddError = string.Empty;

    public DevPluginsSettingsEntry()
    {
        this.Name = Loc.Localize("DalamudSettingsDevPluginLocation", "Dev Plugin Locations");
    }

    public override void OnClose()
    {
        this.devPluginLocations =
            Service<DalamudConfiguration>.Get().DevPluginLoadLocations.Select(x => x.Clone()).ToList();
    }

    public override void Load()
    {
        this.devPluginLocations =
            Service<DalamudConfiguration>.Get().DevPluginLoadLocations.Select(x => x.Clone()).ToList();
        this.devPluginLocationsChanged = false;
    }

    public override void Save()
    {
        Service<DalamudConfiguration>.Get().DevPluginLoadLocations = this.devPluginLocations.Select(x => x.Clone()).ToList();

        if (this.devPluginLocationsChanged)
        {
            Service<PluginManager>.Get().ScanDevPlugins();
            this.devPluginLocationsChanged = false;
        }
    }

    public override void Draw()
    {
        ImGui.Text(this.Name);
        if (this.devPluginLocationsChanged)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen))
            {
                ImGui.SameLine();
                ImGui.Text(Loc.Localize("DalamudSettingsChanged", "(Changed)"));
            }
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsDevPluginLocationsHint", "Add additional dev plugin load locations.\nThese can be either the directory or DLL path."));

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Columns(4);
        ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

        ImGui.Separator();

        ImGui.Text("#");
        ImGui.NextColumn();
        ImGui.Text("Path");
        ImGui.NextColumn();
        ImGui.Text("Enabled");
        ImGui.NextColumn();
        ImGui.Text(string.Empty);
        ImGui.NextColumn();

        ImGui.Separator();

        DevPluginLocationSettings locationToRemove = null;

        var locNumber = 1;
        foreach (var devPluginLocationSetting in this.devPluginLocations)
        {
            var isEnabled = devPluginLocationSetting.IsEnabled;

            ImGui.PushID($"devPluginLocation_{devPluginLocationSetting.Path}");

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
            ImGui.Text(locNumber.ToString());
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            var path = devPluginLocationSetting.Path;
            if (ImGui.InputText($"##devPluginLocationInput", ref path, 65535, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                var contains = this.devPluginLocations.Select(loc => loc.Path).Contains(path);
                if (devPluginLocationSetting.Path == path)
                {
                    // no change.
                }
                else if (contains && devPluginLocationSetting.Path != path)
                {
                    this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginLocationExists", "Location already exists.");
                    Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
                }
                else
                {
                    devPluginLocationSetting.Path = path;
                    this.devPluginLocationsChanged = path != devPluginLocationSetting.Path;
                }
            }

            ImGui.NextColumn();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
            ImGui.Checkbox("##devPluginLocationCheck", ref isEnabled);
            ImGui.NextColumn();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                locationToRemove = devPluginLocationSetting;
            }

            ImGui.PopID();

            ImGui.NextColumn();
            ImGui.Separator();

            devPluginLocationSetting.IsEnabled = isEnabled;

            locNumber++;
        }

        if (locationToRemove != null)
        {
            this.devPluginLocations.Remove(locationToRemove);
            this.devPluginLocationsChanged = true;
        }

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
        ImGui.Text(locNumber.ToString());
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##devPluginLocationInput", ref this.devPluginTempLocation, 300);
        ImGui.NextColumn();
        // Enabled button
        ImGui.NextColumn();
        if (!string.IsNullOrEmpty(this.devPluginTempLocation) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            if (this.devPluginLocations.Any(r => string.Equals(r.Path, this.devPluginTempLocation, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginLocationExists", "Location already exists.");
                Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
            }
            else
            {
                this.devPluginLocations.Add(new DevPluginLocationSettings
                {
                    Path = this.devPluginTempLocation.Replace("\"", string.Empty),
                    IsEnabled = true,
                });
                this.devPluginLocationsChanged = true;
                this.devPluginTempLocation = string.Empty;
            }
        }

        ImGui.Columns(1);

        if (!string.IsNullOrEmpty(this.devPluginLocationAddError))
        {
            ImGuiHelpers.SafeTextColoredWrapped(new Vector4(1, 0, 0, 1), this.devPluginLocationAddError);
        }
    }
}
