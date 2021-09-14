using System.Numerics;

using Dalamud.Game.Gui.Internal;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A window for displaying IME details.
    /// </summary>
    internal class IMEWindow : Window
    {
        private const int ImePageSize = 9;

        /// <summary>
        /// Initializes a new instance of the <see cref="IMEWindow"/> class.
        /// </summary>
        public IMEWindow()
            : base("Dalamud IME", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.AlwaysAutoResize)
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

            var native = ime.ImmCandNative;
            for (var i = 0; i < ime.ImmCand.Count; i++)
            {
                var selected = i == (native.Selection % ImePageSize);

                if (selected)
                    ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.HealerGreen);

                ImGui.Text($"{i + 1}. {ime.ImmCand[i]}");

                if (selected)
                    ImGui.PopStyleColor();
            }

            var totalIndex = native.Selection + 1;
            var totalSize = native.Count;

            var pageStart = native.PageStart;
            var pageIndex = (pageStart / ImePageSize) + 1;
            var pageCount = (totalSize / ImePageSize) + 1;

            ImGui.Separator();
            ImGui.Text($"{totalIndex}/{totalSize} ({pageIndex}/{pageCount})");
        }
    }
}
