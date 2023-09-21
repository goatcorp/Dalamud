using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Utility;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// ImGui widget used to manage profiles.
/// </summary>
internal class ProfileManagerWidget
{
    private readonly PluginInstallerWindow installer;
    private Mode mode = Mode.Overview;
    private Guid? editingProfileGuid;

    private string pickerSearch = string.Empty;
    private string profileNameEdit = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileManagerWidget"/> class.
    /// </summary>
    /// <param name="installer">The plugin installer.</param>
    public ProfileManagerWidget(PluginInstallerWindow installer)
    {
        this.installer = installer;
    }

    private enum Mode
    {
        Overview,
        EditSingleProfile,
    }

    /// <summary>
    /// Draw this widget's contents.
    /// </summary>
    public void Draw()
    {
        var tutorialTitle = Locs.TutorialTitle + "###collectionsTutorWindow";
        var tutorialId = ImGui.GetID(tutorialTitle);
        this.DrawTutorial(tutorialTitle);
        
        switch (this.mode)
        {
            case Mode.Overview:
                this.DrawOverview(tutorialId);
                break;

            case Mode.EditSingleProfile:
                this.DrawEdit();
                break;
        }
    }

    /// <summary>
    /// Reset the widget.
    /// </summary>
    public void Reset()
    {
        this.mode = Mode.Overview;
        this.editingProfileGuid = null;
        this.pickerSearch = string.Empty;
    }

    private void DrawTutorial(string modalTitle)
    {
        var open = true;
        ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.Appearing);
        using (var popup = ImRaii.PopupModal(modalTitle, ref open))
        {
            if (popup)
            {
                using var scrolling = ImRaii.Child("###scrolling", new Vector2(-1, -1));
                if (scrolling)
                {
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialParagraphOne);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialParagraphTwo);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialParagraphThree);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialParagraphFour);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialCommands);
                    
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialCommandsEnable);
                    
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialCommandsDisable);
                    
                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialCommandsToggle);

                    ImGuiHelpers.SafeTextWrapped(Locs.TutorialCommandsEnd);
                    ImGuiHelpers.ScaledDummy(5);
            
                    var buttonWidth = 120f;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
                    if (ImGui.Button("OK", new Vector2(buttonWidth, 40)))
                    {
                        ImGui.CloseCurrentPopup();
                    }
                }
            }
        }

        var config = Service<DalamudConfiguration>.Get();
        if (!config.ProfilesHasSeenTutorial)
        {
            ImGui.OpenPopup(modalTitle);
            config.ProfilesHasSeenTutorial = true;
            config.QueueSave();
        }
    }

    private void DrawOverview(uint tutorialId)
    {
        var didAny = false;
        var profman = Service<ProfileManager>.Get();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            profman.AddNewProfile();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.AddProfile);

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileImport))
        {
            try
            {
                profman.ImportProfile(ImGui.GetClipboardText());
                Service<NotificationManager>.Get().AddNotification(Locs.NotificationImportSuccess, type: NotificationType.Success);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not import profile");
                Service<NotificationManager>.Get().AddNotification(Locs.NotificationImportError, type: NotificationType.Error);
            }
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.ImportProfileHint);
        
        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();
        
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Question))
            ImGui.OpenPopup(tutorialId);
        
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.TutorialHint);

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        var windowSize = ImGui.GetWindowSize();

        using var profileChooserChild = ImRaii.Child("###profileChooserScrolling");
        if (profileChooserChild)
        {
            Guid? toCloneGuid = null;

            using var syncScope = profman.GetSyncScope();
            foreach (var profile in profman.Profiles)
            {
                if (profile.IsDefaultProfile)
                    continue;

                var isEnabled = profile.IsEnabled;
                if (ImGuiComponents.ToggleButton($"###toggleButton{profile.Guid}", ref isEnabled))
                {
                    Task.Run(() => profile.SetStateAsync(isEnabled))
                        .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
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
                    ImGui.SetTooltip(Locs.EditProfileHint);

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * 2) - 5);

                if (ImGuiComponents.IconButton($"###cloneButton{profile.Guid}", FontAwesomeIcon.Copy))
                    toCloneGuid = profile.Guid;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.CloneProfileHint);

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * 3) - 5);

                if (ImGuiComponents.IconButton($"###exportButton{profile.Guid}", FontAwesomeIcon.FileExport))
                {
                    ImGui.SetClipboardText(profile.Model.SerializeForShare());
                    Service<NotificationManager>.Get().AddNotification(Locs.CopyToClipboardNotification, type: NotificationType.Success);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.CopyToClipboardHint);

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
                ImGuiHelpers.CenteredText(Locs.AddProfileHint);
                ImGui.PopStyleColor();
            }
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
        var addPluginToProfilePopupId = ImGui.GetID(addPluginToProfilePopup);
        using (var popup = ImRaii.Popup(addPluginToProfilePopup))
        {
            if (popup.Success)
            {
                var width = ImGuiHelpers.GlobalScale * 300;

                using var disabled = ImRaii.Disabled(profman.IsBusy);

                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("###pluginPickerSearch", Locs.SearchHint, ref this.pickerSearch, 255);

                if (ImGui.BeginListBox("###pluginPicker", new Vector2(width, width - 80)))
                {
                    // TODO: Plugin searching should be abstracted... installer and this should use the same search
                    foreach (var plugin in pm.InstalledPlugins.Where(x => x.Manifest.SupportsProfiles && !x.IsDev &&
                                                                          (this.pickerSearch.IsNullOrWhitespace() || x.Manifest.Name.ToLowerInvariant().Contains(this.pickerSearch.ToLowerInvariant()))))
                    {
                        using var disabled2 =
                            ImRaii.Disabled(profile.Plugins.Any(y => y.InternalName == plugin.Manifest.InternalName));

                        if (ImGui.Selectable($"{plugin.Manifest.Name}###selector{plugin.Manifest.InternalName}"))
                        {
                            Task.Run(() => profile.AddOrUpdateAsync(plugin.Manifest.WorkingPluginId, true, false))
                                .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
                        }
                    }

                    ImGui.EndListBox();
                }
            }
        }

        var didAny = false;

        // ======== Top bar ========
        var windowSize = ImGui.GetWindowSize();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.ArrowLeft))
            this.Reset();

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.BackToOverview);

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.FileExport))
        {
            ImGui.SetClipboardText(profile.Model.SerializeForShare());
            Service<NotificationManager>.Get().AddNotification(Locs.CopyToClipboardNotification, type: NotificationType.Success);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.CopyToClipboardHint);

        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
        {
            this.Reset();

            // DeleteProfile() is sync, it doesn't apply and we are modifying the plugins collection. Will throw below when iterating
            // TODO: DeleteProfileAsync should probably apply as well
            Task.Run(async () =>
                {
                    await profman.DeleteProfileAsync(profile);
                    await profman.ApplyAllWantStatesAsync();
                })
                .ContinueWith(t =>
                {
                    this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState);
                });
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.DeleteProfileHint);

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
            Task.Run(() => profile.SetStateAsync(isEnabled))
                .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
        }

        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Locs.TooltipEnableDisable);

        ImGui.Separator();

        ImGuiHelpers.ScaledDummy(5);

        var enableAtBoot = profile.AlwaysEnableAtBoot;
        if (ImGui.Checkbox(Locs.AlwaysEnableAtBoot, ref enableAtBoot))
        {
            profile.AlwaysEnableAtBoot = enableAtBoot;
        }

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Separator();
        var wantPluginAddPopup = false;

        using var pluginListChild = ImRaii.Child("###profileEditorPluginList");
        if (pluginListChild)
        {
            var pluginLineHeight = 32 * ImGuiHelpers.GlobalScale;
            Guid? wantRemovePluginGuid = null;

            using var syncScope = profile.GetSyncScope();
            foreach (var plugin in profile.Plugins.ToArray())
            {
                didAny = true;
                var pmPlugin = pm.InstalledPlugins.FirstOrDefault(x => x.Manifest.InternalName == plugin.InternalName);
                var btnOffset = 2;

                if (pmPlugin != null)
                {
                    pic.TryGetIcon(pmPlugin, pmPlugin.Manifest, pmPlugin.IsThirdParty, out var icon);
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

                    var text = Locs.NotInstalled(plugin.InternalName);
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
                            ImGui.SetTooltip(Locs.InstallPlugin);
                    }

                    ImGui.SetCursorPos(before);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                var enabled = plugin.IsEnabled;
                if (ImGui.Checkbox($"###{this.editingProfileGuid}-{plugin.InternalName}", ref enabled))
                {
                    Task.Run(() => profile.AddOrUpdateAsync(plugin.WorkingPluginId, enabled))
                        .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * btnOffset) - 5);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                if (ImGuiComponents.IconButton($"###removePlugin{plugin.InternalName}", FontAwesomeIcon.Trash))
                {
                    wantRemovePluginGuid = plugin.WorkingPluginId;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.RemovePlugin);
            }

            if (wantRemovePluginGuid != null)
            {
                // TODO: handle error
                Task.Run(() => profile.RemoveAsync(wantRemovePluginGuid.Value, false))
                                .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotRemove);
            }

            if (!didAny)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Locs.NoPluginsInProfile);
            }

            ImGuiHelpers.ScaledDummy(10);

            var addPluginsText = Locs.AddPlugin;
            ImGuiHelpers.CenterCursorFor((int)(ImGui.CalcTextSize(addPluginsText).X + 30 + (ImGuiHelpers.GlobalScale * 5)));

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
                wantPluginAddPopup = true;

            ImGui.SameLine();
            ImGuiHelpers.ScaledDummy(5);
            ImGui.SameLine();

            ImGui.TextUnformatted(addPluginsText);

            ImGuiHelpers.ScaledDummy(10);
        }

        if (wantPluginAddPopup)
        {
            this.pickerSearch = string.Empty;
            ImGui.OpenPopup(addPluginToProfilePopupId);
        }
    }

    private static class Locs
    {
        public static string TooltipEnableDisable =>
            Loc.Localize("ProfileManagerEnableDisableHint", "Enable/Disable this collection");

        public static string InstallPlugin => Loc.Localize("ProfileManagerInstall", "Install this plugin");

        public static string RemovePlugin =>
            Loc.Localize("ProfileManagerRemoveFromProfile", "Remove plugin from this collection");

        public static string AddPlugin => Loc.Localize("ProfileManagerAddPlugin", "Add a plugin!");

        public static string NoPluginsInProfile =>
            Loc.Localize("ProfileManagerNoPluginsInProfile", "Collection has no plugins!");

        public static string AlwaysEnableAtBoot =>
            Loc.Localize("ProfileManagerAlwaysEnableAtBoot", "Always enable when game starts");

        public static string DeleteProfileHint => Loc.Localize("ProfileManagerDeleteProfile", "Delete this collection");

        public static string CopyToClipboardHint =>
            Loc.Localize("ProfileManagerCopyToClipboard", "Copy collection to clipboard for sharing");

        public static string CopyToClipboardNotification =>
            Loc.Localize("ProfileManagerCopyToClipboardHint", "Copied to clipboard!");

        public static string BackToOverview => Loc.Localize("ProfileManagerBackToOverview", "Back to overview");

        public static string SearchHint => Loc.Localize("ProfileManagerSearchHint", "Search...");

        public static string AddProfileHint => Loc.Localize("ProfileManagerAddProfileHint", "No collections! Add one!");

        public static string CloneProfileHint => Loc.Localize("ProfileManagerCloneProfile", "Clone this collection");

        public static string EditProfileHint => Loc.Localize("ProfileManagerEditProfile", "Edit this collection");

        public static string ImportProfileHint =>
            Loc.Localize("ProfileManagerImportProfile", "Import a shared collection from your clipboard");

        public static string TutorialHint =>
            Loc.Localize("ProfileManagerTutorialHint", "Learn more about collections");

        public static string AddProfile => Loc.Localize("ProfileManagerAddProfile", "Add a new collection");

        public static string NotificationImportSuccess =>
            Loc.Localize("ProfileManagerNotificationImportSuccess", "Collection successfully imported!");

        public static string NotificationImportError =>
            Loc.Localize("ProfileManagerNotificationImportError", "Could not import collection.");

        public static string ErrorCouldNotRemove =>
            Loc.Localize("ProfileManagerCouldNotRemove", "Could not remove plugin.");

        public static string ErrorCouldNotChangeState =>
            Loc.Localize("ProfileManagerCouldNotChangeState", "Could not change plugin state.");

        public static string TutorialTitle =>
            Loc.Localize("ProfileManagerTutorial", "About Collections");
        
        public static string TutorialParagraphOne =>
            Loc.Localize("ProfileManagerTutorialParagraphOne", "Collections are shareable lists of plugins that can be enabled or disabled in the plugin installer or via chat commands.\nWhen a plugin is part of a collection, it will be enabled if the collection is enabled. If a plugin is part of multiple collections, it will be enabled if one or more collections it is a part of are enabled.");
        
        public static string TutorialParagraphTwo =>
            Loc.Localize("ProfileManagerTutorialParagraphTwo", "You can add plugins to collections by clicking the plus button when editing a collection on this screen, or by using the button with the toolbox icon on the \"Installed Plugins\" screen.");
        
        public static string TutorialParagraphThree =>
            Loc.Localize("ProfileManagerTutorialParagraphThree", "If a collection's \"Start on boot\" checkbox is ticked, the collection and the plugins within will be enabled every time the game starts up, even if it has been manually disabled in a prior session.");

        public static string TutorialParagraphFour =>
            Loc.Localize("ProfileManagerTutorialParagraphFour", "Individual plugins inside a collection also have a checkbox next to them. This indicates if a plugin is active within that collection - if the checkbox is not ticked, the plugin will not be enabled if that collection is active. Mind that it will still be enabled if the plugin is an active part of any other collection.");

        public static string TutorialCommands =>
            Loc.Localize("ProfileManagerTutorialCommands", "You can use the following commands in chat or in macros to manage active collections:");
        
        public static string TutorialCommandsEnable =>
            Loc.Localize("ProfileManagerTutorialCommandsEnable", "/xlenableprofile \"Collection Name\" - Enable a collection");

        public static string TutorialCommandsDisable =>
            Loc.Localize("ProfileManagerTutorialCommandsDisable", "/xldisableprofile \"Collection Name\" - Disable a collection");
        
        public static string TutorialCommandsToggle =>
            Loc.Localize("ProfileManagerTutorialCommandsToggle", "/xltoggleprofile \"Collection Name\" - Toggle a collection's state");
        
        public static string TutorialCommandsEnd =>
            Loc.Localize("ProfileManagerTutorialCommandsEnd", "If you run multiple of these commands, they will be executed in order.");

        public static string NotInstalled(string name) =>
            Loc.Localize("ProfileManagerNotInstalled", "{0} (Not Installed)").Format(name);
    }
}
