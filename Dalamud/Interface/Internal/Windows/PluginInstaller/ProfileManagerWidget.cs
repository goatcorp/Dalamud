using System;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Interface.Components;
using Dalamud.Interface.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

public class ProfileManagerWidget
{
    private Mode mode = Mode.Overview;
    private Guid? editingProfileGuid;

    private Task? doingStuffTask = null;

    private string? pickerSelectedPluginInternalName = null;
    private string profileNameEdit = string.Empty;

    public ProfileManagerWidget()
    {
    }

    public void Draw()
    {
        switch (this.mode)
        {
            case Mode.Overview:
                this.DrawOverview();
                break;

            case Mode.EditSingleProfile:
                this.DrawEdit();
                break;
        }
    }

    public void Reset()
    {
        this.mode = Mode.Overview;
        this.editingProfileGuid = null;
        this.pickerSelectedPluginInternalName = null;
    }

    private void DrawOverview()
    {
        var didAny = false;
        var profman = Service<ProfileManager>.Get();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            profman.AddNewProfile();
        }

        foreach (var profile in profman.Profiles)
        {
            if (profile.IsDefaultProfile)
                continue;

            var isEnabled = profile.IsEnabled;
            if (ImGuiComponents.ToggleButton($"###toggleButton{profile.Guid}", ref isEnabled))
            {
                this.doingStuffTask = Task.Run(() => profile.SetState(isEnabled));
            }

            ImGui.SameLine();

            ImGui.Text(profile.Name);
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text(profile.Guid.ToString());
                ImGui.EndTooltip();
            }

            ImGui.SameLine();

            if (ImGui.Button($"Edit###editButton{profile.Guid}"))
            {
                this.mode = Mode.EditSingleProfile;
                this.editingProfileGuid = profile.Guid;
                this.profileNameEdit = profile.Name;
            }

            ImGui.SameLine();

            if (ImGui.Button("Clone"))
            {
                profman.CloneProfile(profile);
            }

            didAny = true;
        }

        if (!didAny)
        {
            ImGui.Text("No profiles! Add one!");
        }
    }

    private void DrawEdit()
    {
        if (this.editingProfileGuid == null)
        {
            Log.Error("Editing profile guid was null");
            this.Reset();
            return;
        }

        var profman = Service<ProfileManager>.Get();
        var pm = Service<PluginManager>.Get();
        var profile = profman.Profiles.FirstOrDefault(x => x.Guid == this.editingProfileGuid);

        if (profile == null)
        {
            Log.Error("Could not find profile {Guid} for edit", this.editingProfileGuid);
            this.Reset();
            return;
        }

        var didAny = false;

        const string addPluginToProfilePopup = "###addPluginToProfile";
        if (ImGui.BeginPopup(addPluginToProfilePopup))
        {
            var selected =
                pm.InstalledPlugins.FirstOrDefault(
                    x => x.Manifest.InternalName == this.pickerSelectedPluginInternalName);

            if (ImGui.BeginCombo("###pluginPicker", selected == null ? "Pick one" : selected.Manifest.Name))
            {
                foreach (var plugin in pm.InstalledPlugins.Where(x => !x.Manifest.IsThirdParty))
                {
                    if (ImGui.Selectable($"{plugin.Manifest.Name}###selector{plugin.Manifest.InternalName}"))
                    {
                        this.pickerSelectedPluginInternalName = plugin.Manifest.InternalName;
                    }
                }

                ImGui.EndCombo();
            }


            using (ImRaii.Disabled(this.pickerSelectedPluginInternalName == null))
            {
                if (ImGui.Button("Do it") && selected != null)
                {
                    Task.Run(() => profile.AddOrUpdate(selected.Manifest.InternalName, selected.IsLoaded));
                }
            }

            ImGui.EndPopup();
        }

        var isEnabled = profile.IsEnabled;
        if (ImGuiComponents.ToggleButton($"###toggleButton{profile.Guid}", ref isEnabled))
        {
            this.doingStuffTask = Task.Run(() => profile.SetState(isEnabled));
        }

        ImGui.SameLine();

        ImGui.Text("Enable/Disable");

        ImGui.Separator();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            ImGui.OpenPopup(addPluginToProfilePopup);
        }

        foreach (var plugin in profile.Plugins)
        {
            didAny = true;

            var enabled = plugin.IsEnabled;
            if (ImGui.Checkbox($"###{this.editingProfileGuid}-{plugin.InternalName}", ref enabled))
            {
                this.doingStuffTask = Task.Run(() => profile.AddOrUpdate(plugin.InternalName, enabled));
            }

            ImGui.SameLine();

            var pmPlugin = pm.InstalledPlugins.FirstOrDefault(x => x.Manifest.InternalName == plugin.InternalName);

            if (pmPlugin != null)
            {
                ImGui.TextUnformatted($"{pmPlugin.Name}");
            }
            else
            {
                ImGui.Text($"{plugin.InternalName} (Unknown/Not Installed)");
            }
        }

        if (!didAny)
        {
            ImGui.Text("Profile has no plugins!");
        }

        ImGui.Separator();

        if (ImGui.InputText("Profile Name", ref this.profileNameEdit, 255))
        {
            profile.Name = this.profileNameEdit;
        }

        var enableAtBoot = profile.AlwaysEnableAtBoot;
        if (ImGui.Checkbox("Always enable when game starts", ref enableAtBoot))
        {
            profile.AlwaysEnableAtBoot = enableAtBoot;
        }

        ImGui.Separator();
        if (ImGui.Button("Back"))
        {
            this.Reset();
        }
    }

    private enum Mode
    {
        Overview,
        EditSingleProfile,
    }
}
