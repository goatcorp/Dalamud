using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the plugin installer.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        private static readonly ModuleLog Log = new("CorePlugin");

        private readonly Dalamud dalamud;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public PluginWindow(Dalamud dalamud)
            : base("CorePlugin", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar)
        {
            this.dalamud = dalamud;
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.Always;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
        }
    }
}
