using System.Linq;
using System.Numerics;

using Dalamud.Game.Internal.Gui;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A window for displaying IME details.
    /// </summary>
    internal class IMEWindow : Window
    {
        private readonly DalamudIME dalamudIME;

        /// <summary>
        /// Initializes a new instance of the <see cref="IMEWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public IMEWindow(Dalamud dalamud)
            : base("Dalamud IME", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing)
        {
            this.dalamudIME = dalamud.IME;
            this.Size = new Vector2(100, 200);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            if (this.dalamudIME == null || !this.dalamudIME.IsEnabled)
            {
                ImGui.Text("IME unavailable.");
                return;
            }

            ImGui.Text(this.dalamudIME.ImmComp);

            ImGui.Separator();
            for (var i = 0; i < this.dalamudIME.ImmCand.Count; i++)
            {
                ImGui.Text($"{i + 1}. {this.dalamudIME.ImmCand[i]}");
            }
        }
    }
}
