using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Interface.IME
{
    class DalamudIMEWindow : Window
    {
        private DalamudIME dalamudIME;
        public DalamudIMEWindow(DalamudIME dalamudIME)
            : base("Dalamud IME", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoFocusOnAppearing)
        {
            this.dalamudIME = dalamudIME;
            this.Size = new Vector2(100, 200);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }
        public override void Draw()
        {
            ImGui.Text(dalamudIME.ImmComp);
            ImGui.Separator();
            for (var i = 0; i < dalamudIME.ImmCand.Count; i++)
                ImGui.Text($"{i + 1}. {dalamudIME.ImmCand[i]}");
        }
    }
}
