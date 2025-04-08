using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class DevPluginsSettingsEntry : SettingsEntry
{
    private List<DevPluginLocationSettings> devPluginLocations = new();
    private bool devPluginLocationsChanged;
    private string devPluginTempLocation = string.Empty;
    private string devPluginLocationAddError = string.Empty;
    private FileDialogManager fileDialogManager = new();

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
        using var id = ImRaii.PushId("devPluginLocation");
        ImGui.TextUnformatted(this.Name);
        if (this.devPluginLocationsChanged)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen))
            {
                ImGui.SameLine();
                ImGui.TextUnformatted(Loc.Localize("DalamudSettingsChanged", "(Changed)"));
            }
        }

        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsDevPluginLocationsHint", "Add dev plugin load locations.\nThis must be a path to the plugin DLL."));

        var locationSelect = Loc.Localize("DalamudDevPluginLocationSelect", "Select Dev Plugin DLL");
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Folder, locationSelect))
        {
            this.fileDialogManager.OpenFileDialog(
                locationSelect,
                ".dll",
                (result, path) =>
                {
                    if (result)
                    {
                        this.devPluginTempLocation = path;
                        this.AddDevPlugin();
                    }
                });
        }

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Columns(4);
        ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

        ImGui.Separator();

        ImGui.TextUnformatted("#");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Path");
        ImGui.NextColumn();
        ImGui.TextUnformatted("Enabled");
        ImGui.NextColumn();
        ImGui.TextUnformatted(string.Empty);
        ImGui.NextColumn();

        ImGui.Separator();

        DevPluginLocationSettings locationToRemove = null;

        var locNumber = 1;
        foreach (var devPluginLocationSetting in this.devPluginLocations)
        {
            var isEnabled = devPluginLocationSetting.IsEnabled;

            id.Push(devPluginLocationSetting.Path);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(locNumber.ToString()).X / 2));
            ImGui.TextUnformatted(locNumber.ToString());
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
                else if (!ValidDevPluginPath(path))
                {
                    this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginInvalid", "The entered value is not a valid path to a potential Dev Plugin.\nDid you mean to enter it as a custom plugin repository in the fields below instead?");
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

            id.Pop();

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
        ImGui.TextUnformatted(locNumber.ToString());
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##devPluginLocationInput", ref this.devPluginTempLocation, 300);
        ImGui.NextColumn();
        // Enabled button
        ImGui.NextColumn();
        if (!string.IsNullOrEmpty(this.devPluginTempLocation) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            this.AddDevPlugin();
        }

        ImGui.Columns(1);

        if (!string.IsNullOrEmpty(this.devPluginLocationAddError))
        {
            ImGuiHelpers.SafeTextColoredWrapped(new Vector4(1, 0, 0, 1), this.devPluginLocationAddError);
        }
    }

    public override void PostDraw()
    {
        this.fileDialogManager.Draw();
    }

    private static bool ValidDevPluginPath(string path)
        => Path.IsPathRooted(path) && Path.GetExtension(path) == ".dll";

    private void AddDevPlugin()
    {
        this.devPluginTempLocation = this.devPluginTempLocation.Trim('"');
        if (this.devPluginLocations.Any(
                r => string.Equals(r.Path, this.devPluginTempLocation, StringComparison.InvariantCultureIgnoreCase)))
        {
            this.devPluginLocationAddError = Loc.Localize("DalamudDevPluginLocationExists", "Location already exists.");
            Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
        }
        else if (!ValidDevPluginPath(this.devPluginTempLocation))
        {
            this.devPluginLocationAddError = Loc.Localize(
                "DalamudDevPluginInvalid",
                "The entered value is not a valid path to a potential Dev Plugin.\nDid you mean to enter it as a custom plugin repository in the fields below instead?");
            Task.Delay(5000).ContinueWith(t => this.devPluginLocationAddError = string.Empty);
            return;
        }
        else
        {
            this.devPluginLocations.Add(
                new DevPluginLocationSettings
                {
                    Path = this.devPluginTempLocation,
                    IsEnabled = true,
                });
            this.devPluginLocationsChanged = true;
            this.devPluginTempLocation = string.Empty;
        }

        // Enable ImGui asserts if a dev plugin is added, if no choice was made prior
        Service<DalamudConfiguration>.Get().ImGuiAssertsEnabledAtStartup ??= true;
    }
}
