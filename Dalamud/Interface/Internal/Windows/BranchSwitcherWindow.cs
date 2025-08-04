using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Networking.Http;
using Newtonsoft.Json;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Window responsible for switching Dalamud beta branches.
/// </summary>
public class BranchSwitcherWindow : Window
{
    private const string BranchInfoUrl = "https://kamori.goats.dev/Dalamud/Release/Meta";

    private Dictionary<string, VersionEntry>? branches;
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
            var client = Service<HappyHttpClient>.Get().SharedHttpClient;
            this.branches = await client.GetFromJsonAsync<Dictionary<string, VersionEntry>>(BranchInfoUrl);
            Debug.Assert(this.branches != null, "this.branches != null");

            var config = Service<DalamudConfiguration>.Get();
            this.selectedBranchIndex = this.branches!.Any(x => x.Key == config.DalamudBetaKind) ?
                                           this.branches.TakeWhile(x => x.Key != config.DalamudBetaKind).Count()
                                           : 0;

            if (this.branches.ElementAt(this.selectedBranchIndex).Value.Key != config.DalamudBetaKey)
                this.selectedBranchIndex = 0;
        });

        base.OnOpen();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        if (this.branches == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Loading branches..."u8);
            return;
        }

        var si = Service<Dalamud>.Get().StartInfo;

        var itemsArray = this.branches.Select(x => x.Key).ToArray();
        ImGui.ListBox("Branch", ref this.selectedBranchIndex, itemsArray);

        var pickedBranch = this.branches.ElementAt(this.selectedBranchIndex);

        if (pickedBranch.Value.SupportedGameVer != si.GameVersion)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Can't pick this branch. GameVer != SupportedGameVer."u8);
        }
        else
        {
            ImGui.Text($"Version: {pickedBranch.Value.AssemblyVersion} ({pickedBranch.Value.GitSha ?? "unk"})");
            ImGui.Text($"Runtime: {pickedBranch.Value.RuntimeVersion}");

            ImGuiHelpers.ScaledDummy(5);

            void Pick()
            {
                var config = Service<DalamudConfiguration>.Get();
                config.DalamudBetaKind = pickedBranch.Key;
                config.DalamudBetaKey = pickedBranch.Value.Key;
                config.QueueSave();
            }

            if (ImGui.Button("Pick"u8))
            {
                Pick();
                this.IsOpen = false;
            }

            ImGui.SameLine();

            if (ImGui.Button("Pick & Restart"u8))
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

    private class VersionEntry
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("track")]
        public string? Track { get; set; }

        [JsonProperty("assemblyVersion")]
        public string? AssemblyVersion { get; set; }

        [JsonProperty("runtimeVersion")]
        public string? RuntimeVersion { get; set; }

        [JsonProperty("runtimeRequired")]
        public bool RuntimeRequired { get; set; }

        [JsonProperty("supportedGameVer")]
        public string? SupportedGameVer { get; set; }

        [JsonProperty("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonProperty("gitSha")]
        public string? GitSha { get; set; }
    }
}
