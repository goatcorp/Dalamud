using System.Diagnostics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

internal static class VisitRepoUrlButton
{
    public static bool Draw(string? repoUrl, bool big)
    {
        if (!string.IsNullOrEmpty(repoUrl) && repoUrl.StartsWith("https://"))
        {
            ImGui.SameLine();

            var clicked = big ? ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Globe, "Open website") : ImGuiComponents.IconButton(FontAwesomeIcon.Globe);
            if (clicked)
            {
                try
                {
                    _ = Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName = repoUrl,
                            UseShellExecute = true,
                        });
                }
                catch (Exception ex)
                {
                    PluginInstallerWindow.Log.Error(ex, $"Could not open repoUrl: {repoUrl}");
                }
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(PluginInstallerLocs.PluginButtonToolTip_VisitPluginUrl);

            return true;
        }

        return false;
    }
}
