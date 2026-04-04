using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.CorePlugin.PluginInstallerV2.Interfaces;
using Dalamud.Interface;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal.Types.Manifest;

namespace Dalamud.CorePlugin.PluginInstallerV2.Widgets;

/// <summary>
/// Class responsible for drawing the AvailablePlugins List.
/// </summary>
internal class AvailablePluginsWidget : IPluginInstallerWidget
{
    private const float PluginEntryHeight = 80.0f;

    /// <inheritdoc/>
    public required PluginInstallerWindow2 ParentWindow { get; init; }

    private RemotePluginManifest? SelectedPlugin { get; set; }

    /// <inheritdoc/>
    public void Draw()
    {
        if (this.SelectedPlugin is null)
        {
            ImGuiClip.ClippedDraw(this.ParentWindow.PluginListManager.PluginListAvailable, this.DrawPluginEntry, PluginEntryHeight * ImGuiHelpers.GlobalScale);
        }
        else
        {
            this.DrawSelectedPlugin();
        }
    }

    private static IDalamudTextureWrap GetPluginIcon(RemotePluginManifest manifest)
    {
        var imageCache = Service<PluginImageCache>.Get();

        var iconTex = imageCache.DefaultIcon;
        var hasIcon = imageCache.TryGetIcon(
            null, manifest, manifest.SourceRepo.IsThirdParty, out var cachedIconTex, out _);

        if (hasIcon && cachedIconTex != null)
        {
            iconTex = cachedIconTex;
        }

        return iconTex;
    }

    private void DrawPluginEntry(RemotePluginManifest manifest)
    {
        var startPosition = ImGui.GetCursorPos();
        var selectableSize = new Vector2(ImGui.GetContentRegionAvail().X, PluginEntryHeight);

        if (ImGui.Selectable($"##{manifest.InternalName}", false, ImGuiSelectableFlags.None, selectableSize))
        {
            this.SelectedPlugin = manifest;
        }

        ImGui.SetCursorPos(startPosition);

        this.DrawPluginParts(manifest);
    }

    private void DrawPluginParts(RemotePluginManifest manifest)
    {
        this.DrawPluginIcon(manifest);

        ImGui.SameLine();

        this.DrawPluginTitle(manifest);
        this.DrawPluginDownloadCount(manifest);
    }

    private void DrawPluginIcon(RemotePluginManifest manifest)
    {
        var startPosition = ImGui.GetCursorPos();

        ImGui.SetCursorPos(startPosition + new Vector2(2.0f, 2.0f));
        ImGui.Image(GetPluginIcon(manifest).Handle, new Vector2(PluginEntryHeight - 4.0f, PluginEntryHeight - 4.0f));
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 4.0f);
    }

    private void DrawPluginTitle(RemotePluginManifest manifest)
    {
        using var biggerFont = this.ParentWindow.FontManager.LargerFontHandle.Value.Push();

        ImGui.Text(manifest.Name);
    }

    private void DrawPluginDownloadCount(RemotePluginManifest manifest)
    {
        ImGui.Text(manifest.DownloadCount.ToString("N"));
    }

    private void DrawSelectedPlugin()
    {
        using (ImRaii.PushColor(ImGuiCol.Button, Vector4.Zero))
        using (this.ParentWindow.FontManager.LargerIconFontHandle.Value.Push())
        {
            if (ImGui.Button(FontAwesomeIcon.ChevronCircleLeft.ToIconString(), new Vector2(64.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionMax().Y)))
            {
                this.SelectedPlugin = null;
            }
        }
    }

    // Not needed... YET -midorikami
    // private PluginHeaderFlags GetHeaderFlags(RemotePluginManifest manifest)
    // {
    //     var dalamudConfiguration = Service<DalamudConfiguration>.Get();
    //     var pluginManager = Service<PluginManager>.Get();
    //
    //     var flags = PluginHeaderFlags.None;
    //
    //     if (manifest.SourceRepo.IsThirdParty)
    //     {
    //         flags.AddFlag(PluginHeaderFlags.IsThirdParty);
    //     }
    //
    //     if (!dalamudConfiguration.SeenPluginInternalName.Contains(manifest.InternalName))
    //     {
    //         flags.AddFlag(PluginHeaderFlags.IsNew);
    //     }
    //
    //     var useTesting = pluginManager.UseTesting(manifest);
    //
    //     var effectiveApiLevel = manifest.DalamudApiLevel;
    //     if (useTesting && manifest.TestingDalamudApiLevel is { } testingApiLevel)
    //     {
    //         effectiveApiLevel = testingApiLevel;
    //     }
    //
    //     var isOutdated = effectiveApiLevel < PluginManager.DalamudApiLevel;
    //     if (isOutdated)
    //     {
    //         flags.AddFlag(PluginHeaderFlags.IsInstallableOutdated);
    //     }
    //
    //     if (useTesting || manifest.IsTestingExclusive)
    //     {
    //         flags.AddFlag(PluginHeaderFlags.IsTesting);
    //     }
    //
    //     var isIncompatible = manifest.MinimumDalamudVersion != null &&
    //                          manifest.MinimumDalamudVersion > Versioning.GetAssemblyVersionParsed();
    //     if (isIncompatible)
    //     {
    //         flags.AddFlag(PluginHeaderFlags.IsIncompatible);
    //     }
    //
    //     return flags;
    // }
}
