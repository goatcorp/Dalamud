using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

internal class ProfileManagerWidget
{
    private readonly PluginInstallerWindow installer;
    private Mode mode = Mode.Overview;
    private Guid? editingProfileGuid;

    private string? pickerSelectedPluginInternalName = null;
    private string profileNameEdit = string.Empty;

    public ProfileManagerWidget(PluginInstallerWindow installer)
    {
        this.installer = installer;
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
            profman.AddNewProfile();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Add a new profile");

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
        {
            try
            {
                profman.ImportProfile(ImGui.GetClipboardText());
                Service<NotificationManager>.Get().AddNotification("Profile successfully imported!", type: NotificationType.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not import profile");
                Service<NotificationManager>.Get().AddNotification("Could not import profile.", type: NotificationType.Error);
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Import a shared profile from your clipboard");

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        var windowSize = ImGui.GetWindowSize();

        if (ImGui.BeginChild("###profileChooserScrolling"))
        {
            Guid? toCloneGuid = null;

            foreach (var profile in profman.Profiles)
            {
                if (profile.IsDefaultProfile)
                    continue;

                var isEnabled = profile.IsEnabled;
                if (ImGuiComponents.ToggleButton($"###toggleButton{profile.Guid}", ref isEnabled))
                {
                    Task.Run(() => profile.SetState(isEnabled))
                        .ContinueWith(this.installer.DisplayErrorContinuation, "Could not change profile state.");
                }

                ImGui.SameLine();
                ImGuiHelpers.ScaledDummy(3);
                ImGui.SameLine();

                ImGui.Text(profile.Name);

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30));

                if (ImGuiComponents.IconButton($"###editButton{profile.Guid}", FontAwesomeIcon.PencilAlt))
                {
                    this.mode = Mode.EditSingleProfile;
                    this.editingProfileGuid = profile.Guid;
                    this.profileNameEdit = profile.Name;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Edit this profile");

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * 2) - 5);

                if (ImGuiComponents.IconButton($"###cloneButton{profile.Guid}", FontAwesomeIcon.Copy))
                    toCloneGuid = profile.Guid;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Clone this profile");

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * 3) - 5);

                if (ImGuiComponents.IconButton($"###exportButton{profile.Guid}", FontAwesomeIcon.FileExport))
                {
                    ImGui.SetClipboardText(profile.Model.Serialize());
                    Service<NotificationManager>.Get().AddNotification("Copied to clipboard!", type: NotificationType.Success);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Copy profile to clipboard for sharing");

                didAny = true;

                ImGuiHelpers.ScaledDummy(2);
            }

            if (toCloneGuid != null)
            {
                profman.CloneProfile(profman.Profiles.First(x => x.Guid == toCloneGuid));
            }

            if (!didAny)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGuiHelpers.CenteredText("No profiles! Add one!");
                ImGui.PopStyleColor();
            }

            ImGui.EndChild();
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
        var pic = Service<PluginImageCache>.Get();
        var profile = profman.Profiles.FirstOrDefault(x => x.Guid == this.editingProfileGuid);

        if (profile == null)
        {
            Log.Error("Could not find profile {Guid} for edit", this.editingProfileGuid);
            this.Reset();
            return;
        }

        const string addPluginToProfilePopup = "###addPluginToProfile";
        if (ImGui.BeginPopup(addPluginToProfilePopup))
        {
            var selected =
                pm.InstalledPlugins.FirstOrDefault(
                    x => x.Manifest.InternalName == this.pickerSelectedPluginInternalName);

            if (ImGui.BeginCombo("###pluginPicker", selected == null ? "Pick one" : selected.Manifest.Name))
            {
                foreach (var plugin in pm.InstalledPlugins.Where(x => x.Manifest.SupportsProfiles))
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
                if (ImGui.Button("Add plugin") && selected != null)
                {
                    // TODO: handle error
                    profile.AddOrUpdate(selected.Manifest.InternalName, true, false);
                    Task.Run(() => profman.ApplyAllWantStates())
                        .ContinueWith(this.installer.DisplayErrorContinuation, "Could not change plugin state.");
                }
            }

            ImGui.EndPopup();
        }

        var didAny = false;

        // ======== Top bar ========
        var windowSize = ImGui.GetWindowSize();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowLeft))
            this.Reset();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Back to overview");

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport))
        {
            ImGui.SetClipboardText(profile.Model.Serialize());
            Service<NotificationManager>.Get().AddNotification("Copied to clipboard!", type: NotificationType.Success);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Copy profile to clipboard for sharing");

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
        {
            // DeleteProfile() is sync, it doesn't apply and we are modifying the plugins collection. Will throw below when iterating
            profman.DeleteProfile(profile);
            Task.Run(() => profman.ApplyAllWantStates())
                .ContinueWith(t =>
                {
                    this.Reset();
                    this.installer.DisplayErrorContinuation(t, "Could not refresh profiles.");
                });
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Delete this profile");

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        ImGui.SetNextItemWidth(windowSize.X / 3);
        if (ImGui.InputText("###profileNameInput", ref this.profileNameEdit, 255))
        {
            profile.Name = this.profileNameEdit;
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(windowSize.X - (ImGui.GetFrameHeight() * 1.55f * ImGuiHelpers.GlobalScale));

        var isEnabled = profile.IsEnabled;
        if (ImGuiComponents.ToggleButton($"###toggleButton{profile.Guid}", ref isEnabled))
        {
            Task.Run(() => profile.SetState(isEnabled))
                .ContinueWith(this.installer.DisplayErrorContinuation, "Could not change profile state.");
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Enable/Disable this profile");

        ImGui.Separator();

        ImGuiHelpers.ScaledDummy(5);

        var enableAtBoot = profile.AlwaysEnableAtBoot;
        if (ImGui.Checkbox("Always enable when game starts", ref enableAtBoot))
        {
            profile.AlwaysEnableAtBoot = enableAtBoot;
        }

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Separator();
        var wantPluginAddPopup = false;

        if (ImGui.BeginChild("###profileEditorPluginList"))
        {
            var pluginLineHeight = 32 * ImGuiHelpers.GlobalScale;
            string? wantRemovePluginInternalName = null;

            foreach (var plugin in profile.Plugins)
            {
                didAny = true;
                var pmPlugin = pm.InstalledPlugins.FirstOrDefault(x => x.Manifest.InternalName == plugin.InternalName);
                var btnOffset = 2;

                if (pmPlugin != null)
                {
                    pic.TryGetIcon(pmPlugin, pmPlugin.Manifest, pmPlugin.Manifest.IsThirdParty, out var icon);
                    icon ??= pic.DefaultIcon;

                    ImGui.Image(icon.ImGuiHandle, new Vector2(pluginLineHeight));
                    ImGui.SameLine();

                    var text = $"{pmPlugin.Name}";
                    var textHeight = ImGui.CalcTextSize(text);
                    var before = ImGui.GetCursorPos();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (textHeight.Y / 2));
                    ImGui.TextUnformatted(text);

                    ImGui.SetCursorPos(before);
                }
                else
                {
                    ImGui.Image(pic.DefaultIcon.ImGuiHandle, new Vector2(pluginLineHeight));
                    ImGui.SameLine();

                    var text = $"{plugin.InternalName} (Not Installed)";
                    var textHeight = ImGui.CalcTextSize(text);
                    var before = ImGui.GetCursorPos();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (textHeight.Y / 2));
                    ImGui.TextUnformatted(text);

                    var available =
                        pm.AvailablePlugins.FirstOrDefault(
                            x => x.InternalName == plugin.InternalName && !x.SourceRepo.IsThirdParty);
                    if (available != null)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * 2) - 2);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));
                        btnOffset = 3;

                        if (ImGuiComponents.IconButton($"###installMissingPlugin{available.InternalName}", FontAwesomeIcon.Download))
                        {
                            this.installer.StartInstall(available, false);
                        }

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip("Install this plugin");
                    }

                    ImGui.SetCursorPos(before);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                var enabled = plugin.IsEnabled;
                if (ImGui.Checkbox($"###{this.editingProfileGuid}-{plugin.InternalName}", ref enabled))
                {
                    Task.Run(() => profile.AddOrUpdate(plugin.InternalName, enabled))
                        .ContinueWith(this.installer.DisplayErrorContinuation, "Could not change plugin state.");
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * btnOffset) - 5);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                if (ImGuiComponents.IconButton($"###removePlugin{plugin.InternalName}", FontAwesomeIcon.Trash))
                {
                    wantRemovePluginInternalName = plugin.InternalName;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Remove plugin from this profile");
            }

            if (wantRemovePluginInternalName != null)
            {
                // TODO: handle error
                profile.Remove(wantRemovePluginInternalName, false);
                Task.Run(() => profman.ApplyAllWantStates())
                    .ContinueWith(this.installer.DisplayErrorContinuation, "Could not remove plugin.");
            }

            if (!didAny)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, "Profile has no plugins!");
            }

            ImGuiHelpers.ScaledDummy(10);

            var addPluginsText = "Add a plugin!";
            ImGuiHelpers.CenterCursorFor((int)(ImGui.CalcTextSize(addPluginsText).X + 30 + (ImGuiHelpers.GlobalScale * 5)));

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                wantPluginAddPopup = true;

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(5);
            ImGui.SameLine();

            ImGui.TextUnformatted(addPluginsText);

            ImGuiHelpers.ScaledDummy(10);

            ImGui.EndChild();
        }

        if (wantPluginAddPopup)
            ImGui.OpenPopup(addPluginToProfilePopup);
    }

    private enum Mode
    {
        Overview,
        EditSingleProfile,
    }
}
