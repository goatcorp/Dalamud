using System.Numerics;

using Dalamud.Game.Gui.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A window for displaying IME details.
    /// </summary>
    internal class IMEWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IMEWindow"/> class.
        /// </summary>
        public IMEWindow()
            : base("Dalamud IME", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing)
        {
            this.Size = new Vector2(100, 200);
            this.SizeCondition = ImGuiCond.FirstUseEver;

            this.RespectCloseHotkey = false;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var ime = Service<DalamudIME>.GetNullable();

            if (ime == null || !ime.IsEnabled)
            {
                ImGui.Text("IME is unavailable.");
                return;
            }

            ImGui.Text(ime.ImmComp);

            ImGui.Separator();
            for (var i = 0; i < ime.ImmCand.Count; i++)
            {
                ImGui.Text($"{i + 1}. {ime.ImmCand[i]}");
            }
        }
    }
}
