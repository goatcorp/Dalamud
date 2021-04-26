using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Internal;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using ImGuiScene;
using Serilog;

namespace Dalamud.Interface
{
    class DalamudCreditsWindow : Window, IDisposable {
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
daemitus
Aireil
kalilistic



Localization by:

Aireil
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

        public DalamudCreditsWindow(Dalamud dalamud)
            : base("Dalamud Credits", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize, true)
        {
            this.dalamud = dalamud;
            this.logoTexture = this.dalamud.InterfaceManager.LoadImage(
                                       Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "logo.png"));
            this.framework = dalamud.Framework;

            this.Size = new Vector2(500, 400);
            this.SizeCondition = ImGuiCond.Always;

            this.PositionCondition = ImGuiCond.Always;

            this.BgAlpha = 0.5f;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            var pluginCredits = this.dalamud.PluginManager.Plugins.Where(x => x.Definition != null).Aggregate(string.Empty, (current, plugin) => current + $"{plugin.Definition.Name} by {plugin.Definition.Author}\n");

            this.creditsText =
                string.Format(CreditsTextTempl, typeof(Dalamud).Assembly.GetName().Version, pluginCredits);

            this.framework.Gui.SetBgm(132);
        }

        public override void OnClose()
        {
            base.OnClose();

            this.framework.Gui.SetBgm(9999);
        }

        public void Dispose() {
            this.logoTexture?.Dispose();
        }

        public override void Draw() {
            var screenSize = ImGui.GetMainViewport().Size;
            var windowSize = ImGui.GetWindowSize();

            this.Position = new Vector2((screenSize.X / 2) - windowSize.X / 2, (screenSize.Y / 2) - windowSize.Y / 2);

            ImGui.BeginChild("scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.NoScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));

            ImGui.Dummy(new Vector2(0, 340f) * ImGui.GetIO().FontGlobalScale);
            ImGui.Text("");

            ImGui.SameLine(150f);
            ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(190f, 190f) * ImGui.GetIO().FontGlobalScale);

            ImGui.Dummy(new Vector2(0, 20f) * ImGui.GetIO().FontGlobalScale);

            var windowX = ImGui.GetWindowSize().X;

            foreach (var creditsLine in this.creditsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)) {
                var lineLenX = ImGui.CalcTextSize(creditsLine).X;

                ImGui.Dummy(new Vector2((windowX / 2) - lineLenX / 2, 0f));
                ImGui.SameLine();
                ImGui.TextUnformatted(creditsLine);
            }

            ImGui.PopStyleVar();

            var curY = ImGui.GetScrollY();
            var maxY = ImGui.GetScrollMaxY();

            if (curY < maxY - 1)
            {
                ImGui.SetScrollY(curY + 1);
            }

            ImGui.EndChild();
        }
    }
}
