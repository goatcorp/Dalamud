using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
                this.fontChainAtlas = new();
            }

            if (this.fontChainAtlas is null)
                return;

            ImGui.TextUnformatted("=====================");
            ImGui.PushFont(this.fontChainAtlas[0]);
            ImGui.TextUnformatted("Testing 12345");
            ImGui.PopFont();
            ImGui.TextUnformatted("=====================");
        }

    }
}
