using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Configuration;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal class ThirdRepoSettingsEntry : SettingsEntry
{
    private List<ThirdPartyRepoSettings> thirdRepoList = [];
    private bool thirdRepoListChanged;
    private string thirdRepoTempUrl = string.Empty;
    private string thirdRepoAddError = string.Empty;
    private DateTime timeSinceOpened;

    public override void OnOpen()
    {
        this.timeSinceOpened = DateTime.Now;
    }

    public override void OnClose()
    {
        this.thirdRepoList =
            [.. Service<DalamudConfiguration>.Get().ThirdRepoList.Select(x => x.Clone())];
    }

    public override void Load()
    {
        this.thirdRepoList =
            [.. Service<DalamudConfiguration>.Get().ThirdRepoList.Select(x => x.Clone())];
        this.thirdRepoListChanged = false;
    }

    public override void Save()
    {
        Service<DalamudConfiguration>.Get().ThirdRepoList =
            [.. this.thirdRepoList.Select(x => x.Clone())];

        if (this.thirdRepoListChanged)
        {
            _ = Service<PluginManager>.Get().SetPluginReposFromConfigAsync(true);
            this.thirdRepoListChanged = false;
        }
    }

    public override void Draw()
    {
        var config = Service<DalamudConfiguration>.Get();

        using var id = ImRaii.PushId("thirdRepo"u8);
        ImGui.Text(Loc.Localize("DalamudSettingsCustomRepo", "Custom Plugin Repositories"));
        if (this.thirdRepoListChanged)
        {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.HealerGreen))
            {
                ImGui.SameLine();
                ImGui.Text(Loc.Localize("DalamudSettingsChanged", "(Changed)"));
            }
        }

        ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingCustomRepoHint", "Add custom plugin repositories."));

        ImGuiHelpers.ScaledDummy(2);

        config.ThirdRepoSpeedbumpDismissed ??= config.ThirdRepoList.Any(x => x.IsEnabled);
        var disclaimerDismissed = config.ThirdRepoSpeedbumpDismissed.Value;

        ImGui.PushFont(InterfaceManager.IconFont);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudOrange);
        ImGui.TextWrapped(FontAwesomeIcon.ExclamationTriangle.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(2);
        ImGui.SameLine();
        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarningReadThis", "READ THIS FIRST!"));
        ImGui.SameLine();
        ImGuiHelpers.ScaledDummy(2);
        ImGui.SameLine();
        ImGui.PushFont(InterfaceManager.IconFont);
        ImGui.TextWrapped(FontAwesomeIcon.ExclamationTriangle.ToIconString());
        ImGui.PopFont();

        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarning", "We cannot take any responsibility for custom plugins and repositories."));
        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarning5", "If someone told you to copy/paste something here, it's very possible that you are being scammed or taken advantage of."));
        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarning2", "Plugins have full control over your PC, like any other program, and may cause harm or crashes."));
        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarning4", "They can delete your character, steal your FC or Discord account, and burn down your house."));
        ImGui.TextWrapped(Loc.Localize("DalamudSettingCustomRepoWarning3", "Please make absolutely sure that you only install plugins from developers you trust."));

        ImGui.PopStyleColor();

        if (!disclaimerDismissed)
        {
            const int speedbumpTime = 15;
            var elapsed = DateTime.Now - this.timeSinceOpened;
            if (elapsed < TimeSpan.FromSeconds(speedbumpTime))
            {
                ImGui.BeginDisabled();
                ImGui.Button(
                    Loc.Localize("DalamudSettingCustomRepoWarningPleaseWait", "Please wait {0} seconds...").Format(speedbumpTime - elapsed.Seconds));
                ImGui.EndDisabled();
            }
            else
            {
                if (ImGui.Button(Loc.Localize("DalamudSettingCustomRepoWarningIReadIt", "Ok, I have read and understood this warning")))
                {
                    config.ThirdRepoSpeedbumpDismissed = true;
                    config.QueueSave();
                }
            }
        }

        ImGuiHelpers.ScaledDummy(2);

        using var disabled = ImRaii.Disabled(!disclaimerDismissed);

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Columns(4);
        ImGui.SetColumnWidth(0, 18 + (5 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(1, ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - (18 + 16 + 14) - ((5 + 45 + 26) * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(2, 16 + (45 * ImGuiHelpers.GlobalScale));
        ImGui.SetColumnWidth(3, 14 + (26 * ImGuiHelpers.GlobalScale));

        ImGui.Separator();

        ImGui.Text("#"u8);
        ImGui.NextColumn();
        ImGui.Text("URL"u8);
        ImGui.NextColumn();
        ImGui.Text("Enabled"u8);
        ImGui.NextColumn();
        ImGui.Text(string.Empty);
        ImGui.NextColumn();

        ImGui.Separator();

        ImGui.Text("0"u8);
        ImGui.NextColumn();
        ImGui.Text("XIVLauncher"u8);
        ImGui.NextColumn();
        ImGui.NextColumn();
        ImGui.NextColumn();
        ImGui.Separator();

        ThirdPartyRepoSettings repoToRemove = null;

        var repoNumber = 1;
        foreach (var thirdRepoSetting in this.thirdRepoList)
        {
            var isEnabled = thirdRepoSetting.IsEnabled;

            id.Push(thirdRepoSetting.Url);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(repoNumber.ToString()).X / 2));
            ImGui.Text(repoNumber.ToString());
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            var url = thirdRepoSetting.Url;
            if (ImGui.InputText($"##thirdRepoInput", ref url, 65535, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                var contains = this.thirdRepoList.Select(repo => repo.Url).Contains(url);
                if (thirdRepoSetting.Url == url)
                {
                    // no change.
                }
                else if (contains && thirdRepoSetting.Url != url)
                {
                    this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoExists", "Repo already exists.");
                    Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
                }
                else if (!ValidThirdPartyRepoUrl(url))
                {
                    this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoNotUrl", "The entered address is not a valid URL.\nDid you mean to enter it as a DevPlugin in the fields above instead?");
                    Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
                }
                else
                {
                    thirdRepoSetting.Url = url;
                    this.thirdRepoListChanged = url != thirdRepoSetting.Url;
                }
            }

            ImGui.NextColumn();

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 7 - (12 * ImGuiHelpers.GlobalScale));
            if (ImGui.Checkbox("##thirdRepoCheck"u8, ref isEnabled))
            {
                this.thirdRepoListChanged = true;
            }

            ImGui.NextColumn();

            if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            {
                repoToRemove = thirdRepoSetting;
            }

            id.Pop();

            ImGui.NextColumn();
            ImGui.Separator();

            thirdRepoSetting.IsEnabled = isEnabled;

            repoNumber++;
        }

        if (repoToRemove != null)
        {
            this.thirdRepoList.Remove(repoToRemove);
            this.thirdRepoListChanged = true;
        }

        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ImGui.GetColumnWidth() / 2) - 8 - (ImGui.CalcTextSize(repoNumber.ToString()).X / 2));
        ImGui.Text(repoNumber.ToString());
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##thirdRepoUrlInput"u8, ref this.thirdRepoTempUrl, 300);
        ImGui.NextColumn();
        // Enabled button
        ImGui.NextColumn();
        if (!string.IsNullOrEmpty(this.thirdRepoTempUrl) && ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
        {
            this.thirdRepoTempUrl = this.thirdRepoTempUrl.TrimEnd();
            if (this.thirdRepoList.Any(r => string.Equals(r.Url, this.thirdRepoTempUrl, StringComparison.InvariantCultureIgnoreCase)))
            {
                this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoExists", "Repo already exists.");
                Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
            }
            else if (!ValidThirdPartyRepoUrl(this.thirdRepoTempUrl))
            {
                this.thirdRepoAddError = Loc.Localize("DalamudThirdRepoNotUrl", "The entered address is not a valid URL.\nDid you mean to enter it as a DevPlugin in the fields above instead?");
                Task.Delay(5000).ContinueWith(t => this.thirdRepoAddError = string.Empty);
            }
            else
            {
                this.thirdRepoList.Add(new ThirdPartyRepoSettings
                {
                    Url = this.thirdRepoTempUrl,
                    IsEnabled = true,
                });
                this.thirdRepoListChanged = true;
                this.thirdRepoTempUrl = string.Empty;
            }
        }

        ImGui.Columns(1);

        if (!string.IsNullOrEmpty(this.thirdRepoAddError))
        {
            ImGui.TextColoredWrapped(new Vector4(1, 0, 0, 1), this.thirdRepoAddError);
        }
    }

    private static bool ValidThirdPartyRepoUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
        && (uriResult.Scheme == Uri.UriSchemeHttps || uriResult.Scheme == Uri.UriSchemeHttp);
}
