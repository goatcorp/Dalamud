using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using CheapLoc;

using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Internal;
using Dalamud.Storage.Assets;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabAbout : SettingsTab
{
    private const float CreditFps = 60.0f;
    private const string ThankYouText = "Thank you!";
    private const string CreditsTextTempl = @"
Dalamud
A FFXIV Plugin Framework
Version D{0}



Created by:

goat
daemitus
Soreepeong
MidoriKami
Aireil
ff-meli
Caraxi
KazWolfe
kalilistic
lmcintyre
Ottermandias
karashiiro
attickdoor
Haselnussbomber
anna-is-cute
pohky
avafloww
rreminy
Infiziert90
marzent
fitzchivalrik
MgAl2O4
nebel
SheepGoMeh
wolfcomp
0ceal0t
NotAdam
Bluefissure
redstrate
marimelon
pmgr
aers
Poliwrath
MalRD
philpax



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



Plogon
The Plugin Build System
brought to you by:

goat
NotNite
Styr1x
Kouzukii
wolfcomp
Philpax
All DIP enjoyers



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
philpax
Aida
Khayle
Dale
Arcane Disgea
Risu
Tom
Blooym
Valk


We use these awesome libraries:

FFXIVClientStructs ({2})
Copyright (c) 2021 aers
Licensed under the MIT License

Lumina by Adam
Licensed under the WTFPL v2.0

Reloaded Libraries by Sewer56
Licensed under the GNU Lesser General Public License v3.0

DotNetCorePlugins
Copyright (c) Nate McMaster
Licensed under the Apache License, Version 2.0

json
Copyright (c) 2013-2022 Niels Lohmann
Licensed under the MIT License

nmd by Nomade040
Licensed under the Unlicense

MinHook
Copyright (C) 2009-2017 Tsuda Kageyu
Licensed under the BSD 2-Clause License

SRELL
Copyright (c) 2012-2022, Nozomu Katoo

STB Libraries
Copyright (c) 2017 Sean Barrett
Licensed under the MIT License

Please see licenses.txt for more information.


Thanks to everyone in the XIVLauncher Discord server!
Join us at: https://discord.gg/3NMcUV5



Dalamud is licensed under AGPL v3 or later.
Contribute at: https://github.com/goatcorp/Dalamud
";

    private readonly Stopwatch creditsThrottler;
    private readonly IFontAtlas privateAtlas;

    private string creditsText;
    private bool isBgmSet;

    private bool resetNow = false;
    private IDalamudTextureWrap? logoTexture;
    private IFontHandle? thankYouFont;

    public SettingsTabAbout()
    {
        this.creditsThrottler = new();

        this.privateAtlas = Service<FontAtlasFactory>
                            .Get()
                            .CreateFontAtlas(nameof(SettingsTabAbout), FontAtlasAutoRebuildMode.Async);
    }

    public override SettingsEntry[] Entries { get; } = { };

    public override string Title => Loc.Localize("DalamudAbout", "About");

    /// <inheritdoc/>
    public override unsafe void OnOpen()
    {
        var pluginCredits = Service<PluginManager>.Get().InstalledPlugins
                                                  .Where(plugin => plugin.Manifest != null)
                                                  .Select(plugin => $"{plugin.Manifest.Name} by {plugin.Manifest.Author}\n")
                                                  .Aggregate(string.Empty, (current, next) => $"{current}{next}");

        this.creditsText = string.Format(CreditsTextTempl, typeof(Dalamud).Assembly.GetName().Version, pluginCredits, Util.GetGitHashClientStructs());

        var gameGui = Service<GameGui>.Get();
        var clientState = Service<ClientState>.Get();
        var playerState = PlayerState.Instance();

        if (!gameGui.IsOnTitleScreen() && clientState.IsClientIdle() && playerState != null)
        {
            gameGui.SetBgm((ushort)(playerState->MaxExpansion > 3 ? 833 : 132));
            this.isBgmSet = true;
        }

        this.creditsThrottler.Restart();

        this.thankYouFont ??= this.privateAtlas.NewGameFontHandle(new(GameFontFamilyAndSize.TrumpGothic34));

        this.resetNow = true;

        Service<DalamudInterface>.Get().SetCreditsDarkeningAnimation(true);
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        this.creditsThrottler.Reset();

        var gameGui = Service<GameGui>.Get();
        var clientState = Service<ClientState>.Get();

        if (this.isBgmSet)
        {
            if (!gameGui.IsOnTitleScreen() && clientState.IsClientIdle())
                gameGui.SetBgm(9999);

            this.isBgmSet = false;
        }

        Service<DalamudInterface>.Get().SetCreditsDarkeningAnimation(false);
    }

    public override void Draw()
    {
        var windowSize = ImGui.GetWindowSize();

        using var child = ImRaii.Child("scrolling", new Vector2(-1, -10 * ImGuiHelpers.GlobalScale), false, ImGuiWindowFlags.NoScrollbar);
        if (!child)
            return;

        if (this.resetNow)
        {
            ImGui.SetScrollY(0);
            this.resetNow = false;
        }

        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
        {
            ImGuiHelpers.ScaledDummy(0, windowSize.Y + 20f);
            ImGui.Text(string.Empty);

            const float imageSize = 190f;
            ImGui.SameLine((ImGui.GetWindowWidth() / 2) - (imageSize / 2));
            this.logoTexture ??= Service<DalamudAssetManager>.Get().GetDalamudTextureWrap(DalamudAsset.Logo);
            ImGui.Image(this.logoTexture.ImGuiHandle, ImGuiHelpers.ScaledVector2(imageSize));

            ImGuiHelpers.ScaledDummy(0, 20f);

            var windowX = ImGui.GetWindowSize().X;

            foreach (var creditsLine in this.creditsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                var lineLenX = ImGui.CalcTextSize(creditsLine).X;

                ImGui.Dummy(new Vector2((windowX / 2) - (lineLenX / 2), 0f));
                ImGui.SameLine();
                ImGui.TextUnformatted(creditsLine);
            }

            ImGuiHelpers.ScaledDummy(0, 40f);

            if (this.thankYouFont != null)
            {
                using var fontPush = this.thankYouFont.Push();
                var thankYouLenX = ImGui.CalcTextSize(ThankYouText).X;

                ImGui.Dummy(new Vector2((windowX / 2) - (thankYouLenX / 2), 0f));
                ImGui.SameLine();
                ImGui.TextUnformatted(ThankYouText);
            }

            ImGuiHelpers.ScaledDummy(0, windowSize.Y + 50f);
        }

        if (this.creditsThrottler.Elapsed.TotalMilliseconds > (1000.0f / CreditFps))
        {
            var curY = ImGui.GetScrollY();
            var maxY = ImGui.GetScrollMaxY();

            if (curY < maxY - 1)
            {
                ImGui.SetScrollY(curY + 1);
            }
            else
            {
                ImGui.SetScrollY(0);
            }
        }

        base.Draw();
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    public override void Dispose() => this.privateAtlas.Dispose();
}
