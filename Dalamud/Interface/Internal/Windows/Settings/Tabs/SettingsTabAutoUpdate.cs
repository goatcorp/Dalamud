using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.AutoUpdate;
using Dalamud.Plugin.Internal.Types;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabAutoUpdates : SettingsTab
{
    private AutoUpdateBehavior behavior;
    private bool checkPeriodically;
    private string pickerSearch = string.Empty;
    private List<AutoUpdatePreference> autoUpdatePreferences = [];
    
    public override SettingsEntry[] Entries { get; } = Array.Empty<SettingsEntry>();

    public override string Title => Loc.Localize("DalamudSettingsAutoUpdates", "Auto-Updates");

    public override void Draw()
    {
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudWhite, Loc.Localize("DalamudSettingsAutoUpdateHint",
                                                "Dalamud can update your plugins automatically, making sure that you always " +
                                                "have the newest features and bug fixes. You can choose when and how auto-updates are run here."));
        ImGuiHelpers.ScaledDummy(2);
        
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsAutoUpdateDisclaimer1",
                                                "You can always update your plugins manually by clicking the update button in the plugin list. " +
                                                "You can also opt into updates for specific plugins by right-clicking them and selecting \"Always auto-update\"."));
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsAutoUpdateDisclaimer2",
                                                "Dalamud will only notify you about updates while you are idle."));
        
        ImGuiHelpers.ScaledDummy(8);
        
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudWhite, Loc.Localize("DalamudSettingsAutoUpdateBehavior",
                                                "When the game starts..."));
        var behaviorInt = (int)this.behavior;
        ImGui.RadioButton(Loc.Localize("DalamudSettingsAutoUpdateNone", "Do not check for updates automatically"), ref behaviorInt, (int)AutoUpdateBehavior.None);
        ImGui.RadioButton(Loc.Localize("DalamudSettingsAutoUpdateNotify", "Only notify me of new updates"), ref behaviorInt, (int)AutoUpdateBehavior.OnlyNotify);
        ImGui.RadioButton(Loc.Localize("DalamudSettingsAutoUpdateMainRepo", "Auto-update main repository plugins"), ref behaviorInt, (int)AutoUpdateBehavior.UpdateMainRepo);
        ImGui.RadioButton(Loc.Localize("DalamudSettingsAutoUpdateAll", "Auto-update all plugins"), ref behaviorInt, (int)AutoUpdateBehavior.UpdateAll);
        this.behavior = (AutoUpdateBehavior)behaviorInt;

        if (this.behavior == AutoUpdateBehavior.UpdateAll)
        {
            var warning = Loc.Localize(
                "DalamudSettingsAutoUpdateAllWarning",
                "Warning: This will update all plugins, including those not from the main repository.\n" +
                "These updates are not reviewed by the Dalamud team and may contain malicious code.");
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudOrange, warning);
        }
        
        ImGuiHelpers.ScaledDummy(8);
        
        ImGui.Checkbox(Loc.Localize("DalamudSettingsAutoUpdatePeriodically", "Periodically check for new updates while playing"), ref this.checkPeriodically);
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsAutoUpdatePeriodicallyHint",
                                                "Plugins won't update automatically after startup, you will only receive a notification while you are not actively playing."));
        
        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);
        
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudWhite, Loc.Localize("DalamudSettingsAutoUpdateOptedIn",
                                                "Per-plugin overrides"));
        
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudWhite, Loc.Localize("DalamudSettingsAutoUpdateOverrideHint",
                                                "Here, you can choose to receive or not to receive updates for specific plugins. " +
                                                "This will override the settings above for the selected plugins."));

        if (this.autoUpdatePreferences.Count == 0)
        {
            ImGuiHelpers.ScaledDummy(20);
            
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                ImGuiHelpers.CenteredText(Loc.Localize("DalamudSettingsAutoUpdateOptedInHint2",
                                                       "You don't have auto-update rules for any plugins."));
            }
            
            ImGuiHelpers.ScaledDummy(2);
        }
        else
        {
            ImGuiHelpers.ScaledDummy(5);
            
            var pic = Service<PluginImageCache>.Get();

            var windowSize = ImGui.GetWindowSize();
            var pluginLineHeight = 32 * ImGuiHelpers.GlobalScale;
            Guid? wantRemovePluginGuid = null;
            
            foreach (var preference in this.autoUpdatePreferences)
            {
                var pmPlugin = Service<PluginManager>.Get().InstalledPlugins
                                                   .FirstOrDefault(x => x.EffectiveWorkingPluginId == preference.WorkingPluginId);

                var btnOffset = 2;

                if (pmPlugin != null)
                {
                    var cursorBeforeIcon = ImGui.GetCursorPos();
                    pic.TryGetIcon(pmPlugin, pmPlugin.Manifest, pmPlugin.IsThirdParty, out var icon, out _);
                    icon ??= pic.DefaultIcon;

                    ImGui.Image(icon.ImGuiHandle, new Vector2(pluginLineHeight));

                    if (pmPlugin.IsDev)
                    {
                        ImGui.SetCursorPos(cursorBeforeIcon);
                        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.7f);
                        ImGui.Image(pic.DevPluginIcon.ImGuiHandle, new Vector2(pluginLineHeight));
                        ImGui.PopStyleVar();
                    }
                    
                    ImGui.SameLine();

                    var text = $"{pmPlugin.Name}{(pmPlugin.IsDev ? " (dev plugin" : string.Empty)}";
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

                    var text = Loc.Localize("DalamudSettingsAutoUpdateOptInUnknownPlugin", "Unknown plugin");
                    var textHeight = ImGui.CalcTextSize(text);
                    var before = ImGui.GetCursorPos();

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (textHeight.Y / 2));
                    ImGui.TextUnformatted(text);
                    
                    ImGui.SetCursorPos(before);
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 320));
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                string OptKindToString(AutoUpdatePreference.OptKind kind)
                {
                    return kind switch
                    {
                        AutoUpdatePreference.OptKind.NeverUpdate => Loc.Localize("DalamudSettingsAutoUpdateOptInNeverUpdate", "Never update this"),
                        AutoUpdatePreference.OptKind.AlwaysUpdate => Loc.Localize("DalamudSettingsAutoUpdateOptInAlwaysUpdate", "Always update this"),
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }

                ImGui.SetNextItemWidth(ImGuiHelpers.GlobalScale * 250);
                if (ImGui.BeginCombo(
                        $"###autoUpdateBehavior{preference.WorkingPluginId}",
                        OptKindToString(preference.Kind)))
                {
                    foreach (var kind in Enum.GetValues<AutoUpdatePreference.OptKind>())
                    {
                        if (ImGui.Selectable(OptKindToString(kind)))
                        {
                            preference.Kind = kind;
                        }
                    }

                    ImGui.EndCombo();
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX(windowSize.X - (ImGuiHelpers.GlobalScale * 30 * btnOffset) - 5);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (pluginLineHeight / 2) - (ImGui.GetFrameHeight() / 2));

                if (ImGuiComponents.IconButton($"###removePlugin{preference.WorkingPluginId}", FontAwesomeIcon.Trash))
                {
                    wantRemovePluginGuid = preference.WorkingPluginId;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Loc.Localize("DalamudSettingsAutoUpdateOptInRemove", "Remove this override"));
            }
            
            if (wantRemovePluginGuid != null)
            {
                this.autoUpdatePreferences.RemoveAll(x => x.WorkingPluginId == wantRemovePluginGuid);
            }
        }

        void OnPluginPicked(LocalPlugin plugin)
        {
            var id = plugin.EffectiveWorkingPluginId;
            if (id == Guid.Empty)
                throw new InvalidOperationException("Plugin ID is empty.");
            
            this.autoUpdatePreferences.Add(new AutoUpdatePreference(id));
        }
        
        bool IsPluginDisabled(LocalPlugin plugin)
            => this.autoUpdatePreferences.Any(x => x.WorkingPluginId == plugin.EffectiveWorkingPluginId);
        
        bool IsPluginFiltered(LocalPlugin plugin)
            => !plugin.IsDev;
        
        var pickerId = DalamudComponents.DrawPluginPicker(
            "###autoUpdatePicker", ref this.pickerSearch, OnPluginPicked, IsPluginDisabled, IsPluginFiltered);
        
        const FontAwesomeIcon addButtonIcon = FontAwesomeIcon.Plus;
        var addButtonText = Loc.Localize("DalamudSettingsAutoUpdateOptInAdd", "Add new override");
        ImGuiHelpers.CenterCursorFor(ImGuiComponents.GetIconButtonWithTextWidth(addButtonIcon, addButtonText));
        if (ImGuiComponents.IconButtonWithText(addButtonIcon, addButtonText))
        {
            this.pickerSearch = string.Empty;
            ImGui.OpenPopup(pickerId);
        }

        base.Draw();
    }

    public override void Load()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        this.behavior = configuration.AutoUpdateBehavior ?? AutoUpdateBehavior.None;
        this.checkPeriodically = configuration.CheckPeriodicallyForUpdates;
        this.autoUpdatePreferences = configuration.PluginAutoUpdatePreferences;
        
        base.Load();
    }

    public override void Save()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        
        configuration.AutoUpdateBehavior = this.behavior;
        configuration.CheckPeriodicallyForUpdates = this.checkPeriodically;
        configuration.PluginAutoUpdatePreferences = this.autoUpdatePreferences;
        
        base.Save();
    }
}
