using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// A window documenting contributors to the project.
    /// </summary>
    internal class CreditsWindow : Window, IDisposable
    {
        private const float CreditFPS = 60.0f;
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
aers


We use these awesome
FFXIV C# libraries:

Lumina by Adam
FFXIVClientStructs by aers



Thanks to everyone in the XIVLauncher
Discord server

Join us at: https://discord.gg/3NMcUV5



Licensed under AGPL V3 or later
Contribute at: https://github.com/goatsoft/Dalamud


Thank you for using XIVLauncher and Dalamud!
";

        private readonly Dalamud dalamud;
        private readonly TextureWrap logoTexture;
        private readonly Stopwatch creditsThrottler;

        private string creditsText;
        private readonly InterfaceManager interfaceManager;
        private readonly PluginManager pluginManager;
        private readonly Framework framework;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreditsWindow"/> class.
        /// </summary>
        public CreditsWindow()
            : base("Dalamud Credits", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize, true)
        {
            this.dalamud = Service<Dalamud>.Get();
            this.interfaceManager = Service<InterfaceManager>.Get();
            this.pluginManager = Service<PluginManager>.Get();
            this.framework = Service<Framework>.Get();

            this.logoTexture = this.interfaceManager.LoadImage(Path.Combine(this.dalamud.AssetDirectory.FullName, "UIRes", "logo.png"));
            this.creditsThrottler = new();

            this.Size = new Vector2(500, 400);
            this.SizeCondition = ImGuiCond.Always;

            this.PositionCondition = ImGuiCond.Always;

            this.BgAlpha = 0.5f;
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
            var pluginCredits = this.pluginManager.InstalledPlugins
                .Where(plugin => plugin.Manifest != null)
                .Select(plugin => $"{plugin.Manifest.Name} by {plugin.Manifest.Author}\n")
                .Aggregate(string.Empty, (current, next) => $"{current}{next}");

            this.creditsText = string.Format(CreditsTextTempl, typeof(Dalamud).Assembly.GetName().Version, pluginCredits);

            Service<GameGui>.Get().SetBgm(132);
            this.creditsThrottler.Restart();
        }

        /// <inheritdoc/>
        public override void OnClose()
        {
            this.creditsThrottler.Reset();
            Service<GameGui>.Get().SetBgm(9999);
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            var screenSize = ImGui.GetMainViewport().Size;
            var windowSize = ImGui.GetWindowSize();

            this.Position = (screenSize - windowSize) / 2;

            ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

            ImGuiHelpers.ScaledDummy(0, 340f);
            ImGui.Text(string.Empty);

            ImGui.SameLine(150f);
            ImGui.Image(this.logoTexture.ImGuiHandle, ImGuiHelpers.ScaledVector2(190f, 190f));

            ImGuiHelpers.ScaledDummy(0, 20f);

            var windowX = ImGui.GetWindowSize().X;

            foreach (var creditsLine in this.creditsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                var lineLenX = ImGui.CalcTextSize(creditsLine).X;

                ImGui.Dummy(new Vector2((windowX / 2) - (lineLenX / 2), 0f));
                ImGui.SameLine();
                ImGui.TextUnformatted(creditsLine);
            }

            ImGui.PopStyleVar();

            if (this.creditsThrottler.Elapsed.TotalMilliseconds > (1000.0f / CreditFPS))
            {
                var curY = ImGui.GetScrollY();
                var maxY = ImGui.GetScrollMaxY();

                if (curY < maxY - 1)
                {
                    ImGui.SetScrollY(curY + 1);
                }
            }

            ImGui.EndChild();
        }

        /// <summary>
        /// Disposes of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.logoTexture?.Dispose();
        }
    }
}
