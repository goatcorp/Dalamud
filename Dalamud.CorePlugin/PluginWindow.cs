using System;
using System.Diagnostics;
using System.Linq;

using Dalamud.CorePlugin.MyFonts;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Windowing;

using ImGuiNET;

using JetBrains.Annotations;

using Vector2 = System.Numerics.Vector2;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        private readonly Stopwatch stopwatchLoad = new();

        private string buffer = "Testing 12345 테스트 可能";

        [CanBeNull]
        private FontChainAtlas fontChainAtlas;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        public PluginWindow()
            : base("CorePlugin")
        {
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.fontChainAtlas?.Dispose();
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (ImGui.Button("Test"))
            {
                this.fontChainAtlas?.Dispose();
                this.stopwatchLoad.Restart();
                this.fontChainAtlas = new();
            }

            if (this.fontChainAtlas is null)
                return;

            ImGui.TextUnformatted("=====================");
            ImGui.PushFont(this.fontChainAtlas[new(GameFontFamily.Axis), 12f * 4 / 3]);
            ImGui.InputTextMultiline(
                "Test Here",
                ref this.buffer,
                1024,
                new(ImGui.GetContentRegionAvail().X, 80));
            ImGui.PopFont();
            foreach (var gffas in Enum.GetValues<GameFontFamilyAndSize>().Where(x => x != GameFontFamilyAndSize.Undefined))
            {
                var gfs = new GameFontStyle(gffas);
                ImGui.PushFont(this.fontChainAtlas[new(gfs.Family), gfs.SizePx]);
                ImGui.TextUnformatted(this.buffer);
                ImGui.PopFont();
            }

            this.stopwatchLoad.Stop();
            ImGui.TextUnformatted($"Took {this.stopwatchLoad.ElapsedMilliseconds}ms");
        }
    }
}
