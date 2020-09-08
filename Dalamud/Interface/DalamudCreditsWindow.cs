using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Internal;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    class DalamudCreditsWindow : IDisposable {
        private const string CreditsTextTempl = @"
Dalamud
A FFXIV Hooking Framework
Version D{0}


created by:

goat
Mino
Meli
attick
Aida-Enna
perchbird
Wintermute
fmauNeko
Caraxi
Adam
nibs/Poliwrath
karashiiro
Pohky



Localization by:

Airiel
Akira
area402
Ridge
availuzje
CBMaca
Delaene
fang2hou
Miu
fmauNeko
qtBxi
JasonLucas
karashiiro
hibiya
sayumizumi
N30N014
Neocrow
OhagiYamada
xDarkOne
Truci
Roy
xenris
Xorus



Logo by:

gucciBane



Your plugins were made by:

{1}


Special thanks:

Adam
karashiiro
Kubera
Truci
Haplo
Franz

Everyone in the XIVLauncher Discord server
Join us at: https://discord.gg/3NMcUV5



Licensed under AGPL
Contribute at: https://github.com/goatsoft/Dalamud


Thank you for using XIVLauncher and Dalamud!
";

        private readonly Dalamud dalamud;
        private TextureWrap logoTexture;
        private Framework framework;

        private string creditsText;

        public DalamudCreditsWindow(Dalamud dalamud, TextureWrap logoTexture, Framework framework) {
            this.dalamud = dalamud;
            this.logoTexture = logoTexture;
            this.framework = framework;

            framework.Gui.SetBgm(132);

            var pluginCredits = dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Aggregate(string.Empty, (current, plugin) => current + $"{plugin.Definition.Name} by {plugin.Definition.Author}\n");

            this.creditsText =
                string.Format(CreditsTextTempl, typeof(Dalamud).Assembly.GetName().Version, pluginCredits);
        }

        public void Dispose() {
            this.logoTexture.Dispose();
        }

        public bool Draw() {
            var windowSize = new Vector2(500, 400);
            ImGui.SetNextWindowSize(windowSize, ImGuiCond.Always);
            
            var screenSize = ImGui.GetIO().DisplaySize;
            ImGui.SetNextWindowPos(new Vector2((screenSize.X / 2) - windowSize.X /2, (screenSize.Y / 2) - windowSize.Y / 2), ImGuiCond.Always);

            var isOpen = true;

            ImGui.SetNextWindowBgAlpha(0.5f);

            if (!ImGui.Begin("Dalamud Credits", ref isOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
            {
                ImGui.End();
                return false;
            }

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            ImGui.Dummy(new Vector2(0, 340f));
            ImGui.Text("");

            ImGui.SameLine(150f);
            ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(190f, 190f));

            ImGui.Dummy(new Vector2(0, 20f));

            var windowX = ImGui.GetWindowSize().X;

            foreach (var creditsLine in this.creditsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)) {
                var lineLenX = ImGui.CalcTextSize(creditsLine).X;
                
                ImGui.Dummy(new Vector2((windowX / 2) - lineLenX / 2, 0f));
                ImGui.SameLine();
                ImGui.TextUnformatted(creditsLine);
            }

            ImGui.PopStyleVar();

            if (ImGui.GetScrollY() < ImGui.GetScrollMaxY() - 0.2f)
                ImGui.SetScrollY(ImGui.GetScrollY() + 0.2f);

            ImGui.EndChild();
            ImGui.End();

            if (!isOpen)
                this.framework.Gui.SetBgm(9999);

            return isOpen;
        }
    }
}
