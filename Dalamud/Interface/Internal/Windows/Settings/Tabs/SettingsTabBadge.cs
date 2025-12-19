using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Badge;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Storage.Assets;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class SettingsTabBadge : SettingsTab
{
    private string badgePassword = string.Empty;
    private bool badgeWasError = false;

    public override string Title => Loc.Localize("DalamudSettingsBadge", "Badges");

    public override SettingsOpenKind Kind => SettingsOpenKind.ServerInfoBar;

    public override SettingsEntry[] Entries { get; } =
    [
        new SettingsEntry<bool>(
            LazyLoc.Localize("DalamudSettingsShowBadgesOnTitleScreen", "Show Badges on Title Screen"),
            LazyLoc.Localize("DalamudSettingsShowBadgesOnTitleScreenHint", "If enabled, your unlocked badges will also be shown on the title screen."),
            c => c.ShowBadgesOnTitleScreen,
            (v, c) => c.ShowBadgesOnTitleScreen = v),
    ];

    public override void Draw()
    {
        var badgeManager = Service<BadgeManager>.Get();
        var dalamudInterface = Service<DalamudInterface>.Get();

        ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarHint", "Plugins can put additional information into your server information bar(where world & time can be seen).\nYou can reorder and disable these here."));

        ImGuiHelpers.ScaledDummy(5);

        ImGui.Text(Loc.Localize("DalamudSettingsBadgesUnlock", "Unlock a badge"));
        ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingsBadgesUnlockHint", "If you have received a code for a badge, enter it here to unlock the badge.\nCodes are usually given out during community events or contests."));
        ImGui.InputTextWithHint(
            "##BadgePassword",
            Loc.Localize("DalamudSettingsBadgesUnlockHintInput", "Enter badge code here"),
            ref this.badgePassword,
            100);
        ImGui.SameLine();
        if (ImGui.Button(Loc.Localize("DalamudSettingsBadgesUnlockButton", "Unlock Badge")))
        {
            if (badgeManager.TryUnlockBadge(this.badgePassword.Trim(), BadgeUnlockMethod.User, out var unlockedBadge))
            {
                dalamudInterface.StartBadgeUnlockAnimation(unlockedBadge);
                this.badgeWasError = false;
            }
            else
            {
                this.badgeWasError = true;
            }
        }

        if (this.badgeWasError)
        {
            ImGuiHelpers.ScaledDummy(5);
            ImGui.TextColored(ImGuiColors.DalamudRed, Loc.Localize("DalamudSettingsBadgesUnlockError", "Failed to unlock badge. The code may be invalid or you may have already unlocked this badge."));
        }

        ImGuiHelpers.ScaledDummy(5);

        base.Draw();

        ImGui.Separator();

        ImGuiHelpers.ScaledDummy(5);

        var haveBadges = badgeManager.UnlockedBadges.ToArray();

        if (haveBadges.Length == 0)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarDidNone", "You did not unlock any badges yet.\nBadges can be unlocked by participating in community events or contests."));
        }

        var badgeTexture = Service<DalamudAssetManager>.Get().GetDalamudTextureWrap(DalamudAsset.BadgeAtlas);
        foreach (var badge in haveBadges)
        {
            var uvs = badge.GetIconUv(badgeTexture.Width, badgeTexture.Height);
            var sectionSize = ImGuiHelpers.GlobalScale * 66;

            var startCursor = ImGui.GetCursorPos();

            ImGui.SetCursorPos(startCursor);

            var iconSize = ImGuiHelpers.ScaledVector2(64, 64);
            var cursorBeforeImage = ImGui.GetCursorPos();
            var rectOffset = ImGui.GetWindowContentRegionMin() + ImGui.GetWindowPos();

            if (ImGui.IsRectVisible(rectOffset + cursorBeforeImage, rectOffset + cursorBeforeImage + iconSize))
            {
                ImGui.Image(badgeTexture.Handle, iconSize, uvs.Uv0, uvs.Uv1);
                ImGui.SameLine();
                ImGui.SetCursorPos(cursorBeforeImage);
            }

            ImGui.SameLine();

            ImGuiHelpers.ScaledDummy(5);
            ImGui.SameLine();

            var cursor = ImGui.GetCursorPos();

            // Name
            ImGui.Text(badge.Name());

            cursor.Y += ImGui.GetTextLineHeightWithSpacing();
            ImGui.SetCursorPos(cursor);

            // Description
            ImGui.TextWrapped(badge.Description());

            startCursor.Y += sectionSize;
            ImGui.SetCursorPos(startCursor);

            ImGuiHelpers.ScaledDummy(5);
        }
    }
}
