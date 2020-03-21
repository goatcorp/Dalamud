using System;
using System.Numerics;
using Dalamud.Game.Internal;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    class DalamudCreditsWindow : IDisposable {
        private string creditsText = @"
                                                Dalamud
                              A FFXIV Hooking Framework



                                    created by:

                                        goat
                                        Mino
                                        Meli
                                        attick
                                        Aida-Enna
                                        perchbird
                                        Wintermute



                                    Special thanks:
                                        Adam
                                        karashiiro
                                        Kubera
                                        Truci
                                        Haplo

             Everyone in the XIVLauncher Discord server
";

        private TextureWrap logoTexture;
        private Framework framework;

        public DalamudCreditsWindow(TextureWrap logoTexture, Framework framework) {
            this.logoTexture = logoTexture;
            this.framework = framework;

            framework.Gui.SetBgm(726);
        }

        public void Dispose() {
            this.logoTexture.Dispose();
        }

        public bool Draw() {
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

            var isOpen = true;

            ImGui.SetNextWindowBgAlpha(0.5f);

            if (!ImGui.Begin("Dalamud Credits", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
            {
                ImGui.End();
                return false;
            }

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            ImGui.Dummy(new Vector2(0, 60f));
            ImGui.Text("");

            ImGui.SameLine(150f);
            ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(150f, 150f));

            ImGui.Dummy(new Vector2(0, 20f));

            ImGui.TextUnformatted(this.creditsText);

            ImGui.PopStyleVar();

            if (ImGui.GetScrollY() < ImGui.GetScrollMaxY())
                ImGui.SetScrollY(ImGui.GetScrollY() + 0.2f);

            ImGui.EndChild();
            ImGui.End();

            if (!isOpen)
                this.framework.Gui.SetBgm(9999);

            return isOpen;
        }
    }
}
