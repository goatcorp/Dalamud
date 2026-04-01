using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Windows.PluginInstaller.Enums;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Internal.Profiles;
using Dalamud.Plugin.Internal.Types;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller.Parts;

/// <summary>
/// Class responsible for showing a progress overlay.
/// </summary>
internal class ProgressOverlay
{
    private readonly PluginInstallerWindow pluginInstaller;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressOverlay"/> class.
    /// </summary>
    /// <param name="pluginInstaller">Reference to main Installer Window.</param>
    public ProgressOverlay(PluginInstallerWindow pluginInstaller)
    {
        this.pluginInstaller = pluginInstaller;
    }

    /// <summary>
    /// Draw progress overlay.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Throws out of range if PluginInstaller.LoadingIndicatorKind is undefined.</exception>
    public void Draw()
    {
        var pluginManager = Service<PluginManager>.Get();
        var profileManager = Service<ProfileManager>.Get();

        var isWaitingManager = !pluginManager.PluginsReady || !pluginManager.ReposReady;
        var isWaitingProfiles = profileManager.IsBusy;

        var isLoading = this.pluginInstaller.AnyOperationInProgress || isWaitingManager || isWaitingProfiles;

        if (isWaitingManager)
        {
            this.pluginInstaller.loadingIndicatorKind = LoadingIndicatorKind.Manager;
        }
        else if (isWaitingProfiles)
        {
            this.pluginInstaller.loadingIndicatorKind = LoadingIndicatorKind.ProfilesLoading;
        }

        if (!isLoading)
        {
            return;
        }

        ImGui.SetCursorPos(Vector2.Zero);

        var windowSize = ImGui.GetWindowSize();
        var titleHeight = ImGui.GetFontSize() + (ImGui.GetStyle().FramePadding.Y * 2);

        this.DrawLoadingChild(titleHeight, windowSize, pluginManager);
    }

    private void DrawLoadingChild(float titleHeight, Vector2 windowSize, PluginManager pluginManager)
    {
        using var loadingChild = ImRaii.Child("###installerLoadingFrame"u8, new Vector2(-1, -1), false);
        if (!loadingChild)
        {
            return;
        }

        ImGui.GetWindowDrawList().PushClipRectFullScreen();
        ImGui.GetWindowDrawList().AddRectFilled(
            ImGui.GetWindowPos() + new Vector2(0, titleHeight),
            ImGui.GetWindowPos() + windowSize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f * ImGui.GetStyle().Alpha)),
            ImGui.GetStyle().WindowRounding,
            ImDrawFlags.RoundCornersBottom);
        ImGui.PopClipRect();

        ImGui.SetCursorPosY(windowSize.Y / 2);

        switch (this.pluginInstaller.loadingIndicatorKind)
        {
            case LoadingIndicatorKind.Unknown:
                ImGuiHelpers.CenteredText("Doing something, not sure what!");
                break;

            case LoadingIndicatorKind.EnablingSingle:
                ImGuiHelpers.CenteredText("Enabling plugin...");
                break;

            case LoadingIndicatorKind.DisablingSingle:
                ImGuiHelpers.CenteredText("Disabling plugin...");
                break;

            case LoadingIndicatorKind.UpdatingSingle:
                ImGuiHelpers.CenteredText("Updating plugin...");
                break;

            case LoadingIndicatorKind.UpdatingAll:
                ImGuiHelpers.CenteredText("Updating plugins...");
                break;

            case LoadingIndicatorKind.Installing:
                ImGuiHelpers.CenteredText("Installing plugin...");
                break;

            case LoadingIndicatorKind.Manager:
                if (pluginManager.PluginsReady && !pluginManager.ReposReady)
                {
                    ImGuiHelpers.CenteredText("Loading repositories...");
                    ImGuiHelpers.ScaledDummy(10);

                    DrawProgressBar(
                        pluginManager.Repos,
                        x => x.State != PluginRepositoryState.Success &&
                             x.State != PluginRepositoryState.Fail &&
                             x.IsEnabled,
                        x => x.IsEnabled,
                        x => ImGuiHelpers.CenteredText($"Loading {x.PluginMasterUrl}"));
                }
                else if (!pluginManager.PluginsReady && pluginManager.ReposReady)
                {
                    ImGuiHelpers.CenteredText("Loading installed plugins...");
                    ImGuiHelpers.ScaledDummy(10);

                    DrawProgressBar(
                        pluginManager.InstalledPlugins,
                        x => x.State == PluginState.Loading,
                        x => x.State is PluginState.Loaded or
                                 PluginState.LoadError or
                                 PluginState.Loading,
                        x => ImGuiHelpers.CenteredText($"Loading {x.Name}"));
                }
                else
                {
                    ImGuiHelpers.CenteredText("Loading repositories and plugins...");
                }

                break;

            case LoadingIndicatorKind.ProfilesLoading:
                ImGuiHelpers.CenteredText("Collections are being applied...");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(this.pluginInstaller.loadingIndicatorKind), this.pluginInstaller.loadingIndicatorKind, null);
        }

        if (DateTime.Now - this.pluginInstaller.timeLoaded > TimeSpan.FromSeconds(30) && !pluginManager.PluginsReady)
        {
            ImGuiHelpers.ScaledDummy(10);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGuiHelpers.CenteredText("One of your plugins may be blocking the installer.");
            ImGuiHelpers.CenteredText("You can try restarting in safe mode, and deleting the plugin.");
            ImGui.PopStyleColor();

            ImGuiHelpers.BeginHorizontalButtonGroup()
                        .Add(
                            "Restart in Safe Mode",
                            () =>
                            {
                                var config = Service<DalamudConfiguration>.Get();
                                config.PluginSafeMode = true;
                                config.ForceSave();
                                Dalamud.RestartGame();
                            })
                        .SetCentered(true)
                        .WithHeight(30)
                        .Draw();
        }
    }

    private static void DrawProgressBar<T>(IEnumerable<T> items, Func<T, bool> pendingFunc, Func<T, bool> totalFunc, Action<T> renderPending)
    {
        var windowSize = ImGui.GetWindowSize();

        var numLoaded = 0;
        var total = 0;

        var itemsArray = items as T[] ?? items.ToArray();
        var allPending = itemsArray.Where(pendingFunc)
                                   .ToArray();
        var allLoadedOrLoading = itemsArray.Count(totalFunc);

        // Cap number of items we show to avoid clutter
        const int maxShown = 3;
        foreach (var repo in allPending.Take(maxShown))
        {
            renderPending(repo);
        }

        ImGuiHelpers.ScaledDummy(10);

        numLoaded += allLoadedOrLoading - allPending.Length;
        total += allLoadedOrLoading;
        if (numLoaded != total)
        {
            ImGui.SetCursorPosX(windowSize.X / 3);
            ImGui.ProgressBar(numLoaded / (float)total, new Vector2(windowSize.X / 3, 50), $"{numLoaded}/{total}");
        }
    }
}
