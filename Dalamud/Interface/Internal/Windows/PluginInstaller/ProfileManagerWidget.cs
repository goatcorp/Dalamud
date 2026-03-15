using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Player;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Utility;

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
    private bool lastWasPickingPlugin;

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
        if (!Service<DalamudConfiguration>.Get().ProfilesEnabled)
        {
            this.DrawChoice();
            return;
        }

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

    private void DrawChoice()
    {
        ImGuiHelpers.ScaledDummy(60);
        ImGuiHelpers.CenteredText(Locs.Choice1);
        ImGuiHelpers.CenteredText(Locs.Choice2);
        ImGuiHelpers.ScaledDummy(20);

        var buttonWidth = ImGui.GetWindowWidth() / 3;
        ImGuiHelpers.CenterCursorFor((int)buttonWidth);
        if (ImGui.Button(Locs.ChoiceConfirmation, new Vector2(buttonWidth, 40 * ImGuiHelpers.GlobalScale)))
        {
            var config = Service<DalamudConfiguration>.Get();
            config.ProfilesEnabled = true;
            config.QueueSave();
        }
    }

    private void DrawTutorial(string modalTitle)
    {
        var open = true;
        ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.Appearing);
        using (var popup = ImRaii.PopupModal(modalTitle, ref open))
        {
            if (popup)
            {
                using var scrolling = ImRaii.Child("###scrolling"u8, new Vector2(-1, -1));
                if (scrolling)
                {
                    ImGui.TextWrapped(Locs.TutorialParagraphOne);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextWrapped(Locs.TutorialParagraphTwo);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextWrapped(Locs.TutorialParagraphThree);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextWrapped(Locs.TutorialParagraphFour);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextWrapped(Locs.TutorialParagraphFive);
                    ImGuiHelpers.ScaledDummy(5);
                    ImGui.TextWrapped(Locs.TutorialCommands);

                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.TextWrapped(Locs.TutorialCommandsEnable);

                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.TextWrapped(Locs.TutorialCommandsDisable);

                    ImGui.Bullet();
                    ImGui.SameLine();
                    ImGui.TextWrapped(Locs.TutorialCommandsToggle);

                    ImGui.TextWrapped(Locs.TutorialCommandsEnd);
                    ImGuiHelpers.ScaledDummy(5);

                    var buttonWidth = 120f;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
                    if (ImGui.Button("OK"u8, new Vector2(buttonWidth, 40)))
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

        using var profileChooserChild = ImRaii.Child("###profileChooserScrolling"u8);
        if (profileChooserChild)
        {
            var contentRegionMaxX = ImGui.GetWindowContentRegionMax().X;
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

                // Center text in frame height
                var textHeight = ImGui.CalcTextSize(profile.Name);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetFrameHeight() / 2) - (textHeight.Y / 2));
                ImGui.Text(profile.Name);

                ImGui.SameLine();
                ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30));

                if (ImGuiComponents.IconButton($"###editButton{profile.Guid}", FontAwesomeIcon.PencilAlt))
                {
                    this.mode = Mode.EditSingleProfile;
                    this.editingProfileGuid = profile.Guid;
                    this.profileNameEdit = profile.Name;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.EditProfileHint);

                ImGui.SameLine();
                ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30 * 2) - 5);

                if (ImGuiComponents.IconButton($"###cloneButton{profile.Guid}", FontAwesomeIcon.Copy))
                    toCloneGuid = profile.Guid;

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.CloneProfileHint);

                ImGui.SameLine();
                ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30 * 3) - 5);

                if (ImGuiComponents.IconButton($"###exportButton{profile.Guid}", FontAwesomeIcon.FileExport))
                {
                    ImGui.SetClipboardText(profile.Model.SerializeForShare());
                    Service<NotificationManager>.Get().AddNotification(Locs.CopyToClipboardNotification, type: NotificationType.Success);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.CopyToClipboardHint);

                didAny = true;

                ImGuiHelpers.ScaledDummy(2);

                // Separator if not the last item
                if (profile != profman.Profiles.Last())
                {
                    // Very light grey
                    ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.DalamudGrey.WithAlpha(0.2f));
                    ImGui.Separator();
                    ImGui.PopStyleColor();

                    ImGuiHelpers.ScaledDummy(2);
                }
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

        var addPluginToProfilePopupId = DalamudComponents.DrawPluginPicker(
            "###addPluginToProfilePicker",
            ref this.pickerSearch,
            plugin =>
            {
                Task.Run(() => profile.AddOrUpdateAsync(plugin.EffectiveWorkingPluginId, plugin.Manifest.InternalName, true, false))
                    .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
            },
            plugin => !plugin.Manifest.SupportsProfiles ||
                      profile.Plugins.Any(x => x.WorkingPluginId == plugin.EffectiveWorkingPluginId));

        // Reapply states when closing plugin picker, as we might have changed want states for some plugins. Only do this when closing, to avoid doing it multiple times while picking.
        var isPickingPlugin = ImGui.IsPopupOpen("###addPluginToProfilePicker");
        switch (isPickingPlugin)
        {
            case false when this.lastWasPickingPlugin:
                // Clear search after closing
                this.pickerSearch = string.Empty;

                Task.Run(async () =>
                    {
                        await profman.ApplyAllWantStatesAsync("Finish adding plugins");
                    })
                    .ContinueWith(t =>
                    {
                        this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState);
                    });

                this.lastWasPickingPlugin = false;
                break;

            case true:
                this.lastWasPickingPlugin = true;
                break;
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
                    await profman.ApplyAllWantStatesAsync("Delete profile");
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
        if (ImGui.InputText("###profileNameInput"u8, ref this.profileNameEdit, 255))
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

        ImGui.Text(Locs.StartupBehavior);
        if (ImGui.BeginCombo("##startupBehaviorPicker"u8, Locs.PolicyToLocalisedName(profile.StartupPolicy)))
        {
            foreach (var policy in Enum.GetValues<ProfileModelV1.ProfileStartupPolicy>())
            {
                var name = Locs.PolicyToLocalisedName(policy);
                if (ImGui.Selectable(name, profile.StartupPolicy == policy))
                {
                    profile.StartupPolicy = policy;
                }
            }

            ImGui.EndCombo();
        }

        ImGuiHelpers.ScaledDummy(5);

        var model = (ProfileModelV1)profile.Model;

        if (Service<DalamudConfiguration>.Get().ProfilesEnableCharacters)
        {
            var enableForCharacters = model.EnableForCharacters;
            ImGui.Checkbox(
                Locs.EnableForSpecificCharacters,
                ref enableForCharacters);
            if (enableForCharacters != model.EnableForCharacters)
            {
                model.EnableForCharacters = enableForCharacters;
                Service<DalamudConfiguration>.Get().QueueSave();

                // Profile might now no longer want active
                Task.Run(async () =>
                    {
                        await profman.ApplyAllWantStatesAsync("Toggle enable for characters");
                    })
                    .ContinueWith(t =>
                    {
                        this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState);
                    });
            }
        }

        // If the profile is configured to enable by specific characters, show the character list and controls
        if (model.EnableForCharacters)
        {
            ulong? wantRemoveContentId = null;

            ImGui.Indent();

            foreach (var entry in model.EnabledCharacters.ToArray())
            {
                if (ImGuiComponents.IconButton($"###removeChar{entry.ContentId}", FontAwesomeIcon.Trash))
                {
                    wantRemoveContentId = entry.ContentId;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.Localize("ProfileManagerRemoveCharacter", "Remove character"));

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (5 * ImGuiHelpers.GlobalScale));

                string characterDisplay;
                if (!string.IsNullOrEmpty(entry.DisplayName) && !string.IsNullOrEmpty(entry.ServerName))
                {
                    characterDisplay =
                        $"{entry.DisplayName} <icon({(int)BitmapFontIcon.CrossWorld})> {entry.ServerName}";
                }
                else
                {
                    characterDisplay = entry.ContentId.ToString();
                }

                ImGuiHelpers.CompileSeStringWrapped(characterDisplay);

                if (entry != model.EnabledCharacters.LastOrDefault())
                {
                    ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.DalamudGrey.WithAlpha(0.2f));
                    ImGui.Separator();
                    ImGui.PopStyleColor();
                }
            }

            if (model.EnabledCharacters.Count == 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                ImGui.TextColoredWrapped(
                    ImGuiColors.DalamudGrey,
                    Loc.Localize(
                        "ProfileManagerNoCharactersAdded",
                        "This collection will not be active for any characters until you add some with the button below."));
                ImGui.PopStyleColor();
            }

            ImGui.Unindent();

            if (wantRemoveContentId != null)
            {
                var toRem =
                    model.EnabledCharacters.FirstOrDefault(x => x.ContentId == wantRemoveContentId.Value);
                if (toRem != null)
                {
                    model.EnabledCharacters.Remove(toRem);
                    Service<DalamudConfiguration>.Get().QueueSave();
                }

                // Profile might now no longer want active
                Task.Run(async () => { await profman.ApplyAllWantStatesAsync("Remove character"); })
                    .ContinueWith(t => { this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState); });
            }

            ImGuiHelpers.ScaledDummy(5);

            var player = Service<PlayerState>.Get();
            if (player.IsLoaded)
            {
                using var disabled = ImRaii.Disabled(model.EnabledCharacters.Any(x => x.ContentId == player.ContentId));
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Plus, $"Add current character: {player.CharacterName}"))
                {
                    var serverName = player.HomeWorld.Value.Name.ExtractText();
                    model.EnabledCharacters.Add(new ProfileModelV1.ProfileModelV1Character(player.CharacterName, player.ContentId, serverName));
                    Service<DalamudConfiguration>.Get().QueueSave();

                    // Profile might now want active
                    Task.Run(async () =>
                        {
                            await profman.ApplyAllWantStatesAsync("Add character");
                        })
                        .ContinueWith(t =>
                        {
                            this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState);
                        });
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, Loc.Localize("ProfileManagerCharacterNotLoaded", "You must be logged in to add your current character."));
            }

            ImGuiHelpers.ScaledDummy(5);
        }

        ImGui.Separator();
        var wantPluginAddPopup = false;

        using var pluginListChild = ImRaii.Child("###profileEditorPluginList"u8);
        if (pluginListChild)
        {
            var contentRegionMaxX = ImGui.GetWindowContentRegionMax().X;
            var pluginLineHeight = 32 * ImGuiHelpers.GlobalScale;
            Guid? wantRemovePluginGuid = null;

            using var syncScope = profile.GetSyncScope();
            foreach (var profileEntry in profile.Plugins.ToArray())
            {
                didAny = true;
                var pmPlugin = pm.InstalledPlugins.FirstOrDefault(x => x.EffectiveWorkingPluginId == profileEntry.WorkingPluginId);
                var btnOffset = 2;

                if (pmPlugin != null)
                {
                    var cursorBeforeIcon = ImGui.GetCursorPos();
                    pic.TryGetIcon(pmPlugin, pmPlugin.Manifest, pmPlugin.IsThirdParty, out var icon, out _);
                    icon ??= pic.DefaultIcon;

                    ImGui.Image(icon.Handle, new Vector2(pluginLineHeight));

                    if (pmPlugin.IsDev)
                    {
                        ImGui.SetCursorPos(cursorBeforeIcon);
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.7f);
                        ImGui.Image(pic.DevPluginIcon.Handle, new Vector2(pluginLineHeight));
                        ImGui.PopStyleVar();
                    }

                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (5 * ImGuiHelpers.GlobalScale));

                    var text = $"{pmPlugin.Name}{(pmPlugin.IsDev ? " (dev plugin" : string.Empty)}";
                    var textHeight = ImGui.CalcTextSize(text);
                    var before = ImGui.GetCursorPos();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (textHeight.Y / 2));
                    ImGui.Text(text);

                    ImGui.SetCursorPos(before);
                }
                else
                {
                    ImGui.Image(pic.DefaultIcon.Handle, new Vector2(pluginLineHeight));
                    ImGui.SameLine();

                    var text = Locs.NotInstalled(profileEntry.InternalName);
                    var textHeight = ImGui.CalcTextSize(text);
                    var before = ImGui.GetCursorPos();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (textHeight.Y / 2));
                    ImGui.Text(text);

                    var firstAvailableInstalled = pm.InstalledPlugins.FirstOrDefault(x => x.InternalName == profileEntry.InternalName);
                    var installable =
                        pm.AvailablePlugins.FirstOrDefault(
                            x => x.InternalName == profileEntry.InternalName && !x.SourceRepo.IsThirdParty);

                    if (firstAvailableInstalled != null)
                    {
                        ImGui.Text($"Match to plugin '{firstAvailableInstalled.Name}'?");
                        ImGui.SameLine();
                        if (ImGuiComponents.IconButtonWithText(
                                FontAwesomeIcon.Check,
                                "Yes, use this one"))
                        {
                            profileEntry.WorkingPluginId = firstAvailableInstalled.EffectiveWorkingPluginId;
                            Task.Run(async () =>
                                {
                                    await profman.ApplyAllWantStatesAsync("Match plugin");
                                })
                                .ContinueWith(t =>
                                {
                                    this.installer.DisplayErrorContinuation(t, Locs.ErrorCouldNotChangeState);
                                });
                        }
                    }
                    else if (installable != null)
                    {
                        ImGui.SameLine();
                        ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30 * 2) - 2);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));
                        btnOffset = 3;

                        if (ImGuiComponents.IconButton($"###installMissingPlugin{installable.InternalName}", FontAwesomeIcon.Download))
                        {
                            this.installer.StartInstall(installable, false);
                        }

                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip(Locs.InstallPlugin);
                    }

                    ImGui.SetCursorPos(before);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                var enabled = profileEntry.IsEnabled;
                if (ImGui.Checkbox($"###{this.editingProfileGuid}-{profileEntry.InternalName}", ref enabled))
                {
                    Task.Run(() => profile.AddOrUpdateAsync(profileEntry.WorkingPluginId, profileEntry.InternalName, enabled))
                        .ContinueWith(this.installer.DisplayErrorContinuation, Locs.ErrorCouldNotChangeState);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(contentRegionMaxX - (ImGuiHelpers.GlobalScale * 30 * btnOffset) - 5);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                if (ImGuiComponents.IconButton($"###removePlugin{profileEntry.InternalName}", FontAwesomeIcon.Trash))
                {
                    wantRemovePluginGuid = profileEntry.WorkingPluginId;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Locs.RemovePlugin);

                // Separator if not the last item
                if (profileEntry != profile.Plugins.Last())
                {
                    // Very light grey
                    ImGui.PushStyleColor(ImGuiCol.Border, ImGuiColors.DalamudGrey.WithAlpha(0.2f));
                    ImGui.Separator();
                    ImGui.PopStyleColor();
                }
            }

            if (wantRemovePluginGuid != null)
            {
                // TODO: handle error
                Task.Run(() => profile.RemoveAsync(wantRemovePluginGuid.Value))
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

            ImGui.Text(addPluginsText);

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
        public static string StartupBehavior =>
            Loc.Localize("ProfileManagerStartupBehavior", "Startup behavior");

        public static string EnableForSpecificCharacters => Loc.Localize(
            "ProfileManagerEnableForCharacters",
            "Only enable for specific characters");

        public static string TooltipEnableDisable =>
            Loc.Localize("ProfileManagerEnableDisableHint", "Enable/Disable this collection");

        public static string InstallPlugin => Loc.Localize("ProfileManagerInstall", "Install this plugin");

        public static string RemovePlugin =>
            Loc.Localize("ProfileManagerRemoveFromProfile", "Remove plugin from this collection");

        public static string AddPlugin => Loc.Localize("ProfileManagerAddPlugin", "Add a plugin!");

        public static string NoPluginsInProfile =>
            Loc.Localize("ProfileManagerNoPluginsInProfile", "Collection has no plugins!");

        public static string DeleteProfileHint => Loc.Localize("ProfileManagerDeleteProfile", "Delete this collection");

        public static string CopyToClipboardHint =>
            Loc.Localize("ProfileManagerCopyToClipboard", "Copy collection to clipboard for sharing");

        public static string CopyToClipboardNotification =>
            Loc.Localize("ProfileManagerCopyToClipboardHint", "Copied to clipboard!");

        public static string BackToOverview => Loc.Localize("ProfileManagerBackToOverview", "Back to overview");

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

        public static string TutorialParagraphFive =>
            Loc.Localize("ProfileManagerTutorialParagraphFive", "When ticking the \"{0}\" checkbox, the collection will only be active for specific characters. You can add characters to the list by clicking the plus button and selecting a character, or by clicking the button with the person icon to add your current character. This is useful if you want different collections active on different characters.")
               .Format(EnableForSpecificCharacters);

        public static string TutorialCommands =>
            Loc.Localize("ProfileManagerTutorialCommands", "You can use the following commands in chat or in macros to manage active collections:");

        public static string TutorialCommandsEnable =>
            Loc.Localize("ProfileManagerTutorialCommandsEnable", "{0} \"Collection Name\" - Enable a collection").Format(PluginManagementCommandHandler.CommandEnableProfile);

        public static string TutorialCommandsDisable =>
            Loc.Localize("ProfileManagerTutorialCommandsDisable", "{0} \"Collection Name\" - Disable a collection").Format(PluginManagementCommandHandler.CommandDisableProfile);

        public static string TutorialCommandsToggle =>
            Loc.Localize("ProfileManagerTutorialCommandsToggle", "{0} \"Collection Name\" - Toggle a collection's state").Format(PluginManagementCommandHandler.CommandToggleProfile);

        public static string TutorialCommandsEnd =>
            Loc.Localize("ProfileManagerTutorialCommandsEnd", "If you run multiple of these commands, they will be executed in order.");

        public static string Choice1 =>
            Loc.Localize("ProfileManagerChoice1", "Plugin collections are a new feature that allow you to group plugins into collections which can be toggled and shared.");

        public static string Choice2 =>
            Loc.Localize("ProfileManagerChoice2", "They are experimental and may still contain bugs. Do you want to enable them now?");

        public static string ChoiceConfirmation =>
            Loc.Localize("ProfileManagerChoiceConfirmation", "Yes, enable Plugin Collections");

        public static string NotInstalled(string name) =>
            Loc.Localize("ProfileManagerNotInstalled", "{0} (Not Installed)").Format(name);

        public static string PolicyToLocalisedName(ProfileModelV1.ProfileStartupPolicy policy)
        {
            return policy switch
            {
                ProfileModelV1.ProfileStartupPolicy.RememberState => Loc.Localize(
                    "ProfileManagerRememberState",
                    "Remember state"),
                ProfileModelV1.ProfileStartupPolicy.AlwaysEnable => Loc.Localize(
                    "ProfileManagerAlwaysEnableAtBoot",
                    "Always enable at boot"),
                ProfileModelV1.ProfileStartupPolicy.AlwaysDisable => Loc.Localize(
                    "ProfileManagerAlwaysDisableAtBoot",
                    "Always disable at boot"),
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
            };
        }
    }
}
