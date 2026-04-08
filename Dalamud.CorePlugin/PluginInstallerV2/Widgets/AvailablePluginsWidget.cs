using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.CorePlugin.PluginInstallerV2.Drawing;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the AvailablePlugins List, and selected plugin information pane.
/// </summary>
internal class AvailablePluginsWidget : IPluginInstallerWidget
{
    // Initialized via required ParentWindow property.
    private readonly AvailablePluginRenderer pluginEntryRenderer = null!;

    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow
    {
        get;
        init
        {
            field = value;
            this.pluginEntryRenderer = new AvailablePluginRenderer { ParentWindow = value };
        }
    }

    private IPluginManifest? SelectedPlugin { get; set; }

    /// <inheritdoc/>
    public void Draw()
    {
        if (this.SelectedPlugin is null)
        {
            if (this.ParentWindow.PluginListManager.PluginListAvailable.Count is not 0)
            {
                ImGuiClip.ClippedDraw(this.ParentWindow.PluginListManager.PluginListAvailable, this.DrawPluginEntry, this.pluginEntryRenderer.EntryInnerHeight * ImGuiHelpers.GlobalScale);
            }
            else
            {
                ImGuiHelpers.ScaledDummy(15.0f);

                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudOrange))
                {
                    ImGuiHelpers.CenteredText(PluginInstallerLocs.TabBody_SearchNoCompatible);
                }
            }
        }
        else
        {
            this.DrawSelectedPlugin();
        }
    }

    private void DrawPluginEntry(RemotePluginManifest manifest)
    {
        this.pluginEntryRenderer.DrawAvailablePlugin(manifest, manifestEntry =>
        {
            this.SelectedPlugin = manifestEntry;
        });
    }

    private void DrawSelectedPlugin()
    {
        if (this.SelectedPlugin is null)
        {
            return;
        }

        this.DrawPluginInformation(this.SelectedPlugin);

        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
        {
            if (ImGui.Button(FontAwesomeIcon.ChevronCircleLeft.ToIconString(), new Vector2(64.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionMax().Y)))
            {
                this.SelectedPlugin = null;
            }
        }
    }

    /// <summary>
    /// Draws the lage pae version of this plugin's information.
    /// </summary>
    /// <param name="selectedPlugin">Plugin Manifest.</param>
    private void DrawPluginInformation(IPluginManifest selectedPlugin)
    {
        using var id = ImRaii.PushId(selectedPlugin.InternalName);

        // Title
        // Author
        // Tags
        // Downloads
        // Source (Main/Custom)
        // Source URL if exists
        // Images
        // Description
        // Changelog
        // Controls (footer)

        var titleHeight = 100.0f * ImGuiHelpers.GlobalScale;

        using (var titleChild = ImRaii.Child("TitleChild", new Vector2(ImGui.GetContentRegionAvail().X, titleHeight), true))
        {
            if (titleChild.Success)
            {

            }
        }
    }
}
