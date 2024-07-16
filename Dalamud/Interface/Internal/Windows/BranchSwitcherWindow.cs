using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Support;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Window responsible for switching Dalamud beta branches.
/// </summary>
public class BranchSwitcherWindow : Window
{
    private Dictionary<string, DalamudReleases.DalamudVersionInfo>? branches;
    private int selectedBranchIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="BranchSwitcherWindow"/> class.
    /// </summary>
    public BranchSwitcherWindow()
        : base("Branch Switcher", ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.ShowCloseButton = true;
        this.RespectCloseHotkey = true;
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        Task.Run(async () =>
        {
            var releaseSvc = await Service<DalamudReleases>.GetAsync();
            
            this.branches = await releaseSvc.GetVersionsForAllTracks();
            Debug.Assert(this.branches != null, "this.branches != null");
            
            var resolvedBranchName = await releaseSvc.GetCurrentTrack(); // validates key and resolves correct track name.
            
            this.selectedBranchIndex = this.branches!.Any(x => x.Key == resolvedBranchName) ?
                                           this.branches.TakeWhile(x => x.Key != resolvedBranchName).Count()
                                           : 0;
        });

        base.OnOpen();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.branches == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Loading branches...");
            return;
        }

        var si = Service<Dalamud>.Get().StartInfo;

        var itemsArray = this.branches.Select(x => x.Key).ToArray();
        ImGui.ListBox("Branch", ref this.selectedBranchIndex, itemsArray, itemsArray.Length);

        var pickedBranch = this.branches.ElementAt(this.selectedBranchIndex);

        if (pickedBranch.Value.SupportedGameVer != si.GameVersion)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Can't pick this branch. GameVer != SupportedGameVer.");
        }
        else
        {
            ImGui.Text($"Version: {pickedBranch.Value.AssemblyVersion})");
            ImGui.Text($"Runtime: {pickedBranch.Value.RuntimeVersion}");

            ImGuiHelpers.ScaledDummy(5);

            void Pick()
            {
                var config = Service<DalamudConfiguration>.Get();
                config.DalamudBetaKind = pickedBranch.Key;
                config.DalamudBetaKey = pickedBranch.Value.Key;
                config.QueueSave();
            }

            if (ImGui.Button("Pick"))
            {
                Pick();
                this.IsOpen = false;
            }

            ImGui.SameLine();

            if (ImGui.Button("Pick & Restart"))
            {
                Pick();

                // If we exit immediately, we need to write out the new config now
                Service<DalamudConfiguration>.Get().ForceSave();

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var xlPath = Path.Combine(appData, "XIVLauncher", "XIVLauncher.exe");

                if (File.Exists(xlPath))
                {
                    Process.Start(xlPath);
                    Environment.Exit(0);
                }
            }
        }
    }
}
