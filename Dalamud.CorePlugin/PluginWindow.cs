using System;
using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
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
