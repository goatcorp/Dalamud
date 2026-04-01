using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Text;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.Settings.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Utility.Internal;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal sealed class SettingsTabFools26 : SettingsTab
{
    private bool dismissed = false;

    public override string Title => "Verification";

    public override SettingsOpenKind Kind => SettingsOpenKind.General;

    public override SettingsEntry[] Entries { get; } = [];

    public override void Draw()
    {
        ImGuiHelpers.ScaledDummy(40);

        if (this.dismissed)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);
            ImGui.PushFont(InterfaceManager.IconFont);
            ImGuiHelpers.CenteredText(FontAwesomeIcon.Check.ToIconString());
            ImGui.PopFont();
            ImGui.PopStyleColor();

            ImGuiHelpers.ScaledDummy(5);
            ImGuiHelpers.CenteredText("Ok, we won't ask you again! But this was an April Fools joke, so we wouldn't have anyway!");
            ImGuiHelpers.ScaledDummy(5);
            ImGuiHelpers.CenteredText("If you want to turn off April Fools events for the future, you can disable the");
            ImGuiHelpers.CenteredText("\"Seasonal Events\" option in the \"Look & Feel\" tab!");
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.ParsedPurple);
            ImGui.PushFont(InterfaceManager.IconFont);
            ImGuiHelpers.CenteredText(FontAwesomeIcon.IdCard.ToIconString());
            ImGui.PopFont();
            ImGui.PopStyleColor();

            ImGuiHelpers.ScaledDummy(5);
            ImGuiHelpers.CenteredText("Due to new regulation introduced by your character's city state, going into effect on April 1st,");
            ImGuiHelpers.CenteredText("we need to verify your character. This is done locally on your PC and takes less than a minute.");

            ImGuiHelpers.ScaledDummy(10);

            var buttonSize = new Vector2(160, 40);
            var contentWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX((contentWidth - buttonSize.X) / 2);
            if (ImGui.Button("Verify Now", buttonSize))
            {
                var di = Service<DalamudInterface>.Get();
                di.ToggleSettingsWindow();
                di.OpenFools26Verify();
            }

            ImGuiHelpers.ScaledDummy(5);
            buttonSize = new Vector2(160, 22);
            ImGui.SetCursorPosX((contentWidth - buttonSize.X) / 2);
            if (ImGui.Button("No, don't ask again", buttonSize))
            {
                var config = Service<DalamudConfiguration>.Get();
                config.Fools26Dismissed = true;
                config.QueueSave();
                this.dismissed = true;
            }
        }
    }
}
