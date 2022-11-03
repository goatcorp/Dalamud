using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Game.Gui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// A window documenting contributors to the project.
/// </summary>
internal class CreditsWindow : Window, IDisposable
{
    private const float CreditFps = 60.0f;
    private const string ThankYouText = "Thank you!";
    private const string CreditsTextTempl = @"
Dalamud
A FFXIV Plugin Framework
Version D{0}


created by:

goat
daemitus
Soreepeong
ff-meli
attickdoor
Caraxi
ascclemens
kalilistic
0ceal0t
lmcintyre
pohky
Aireil
fitzchivalrik
MgAl2O4
NotAdam
marimelon
karashiiro
pmgr
Ottermandias
aers
Poliwrath
Minizbot2021
MalRD
SheepGoMeh
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


We use these awesome libraries:

Lumina by Adam
FFXIVClientStructs by aers ({2})

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

Please see licenses.txt for more information.


Thanks to everyone in the XIVLauncher Discord server

Join us at: https://discord.gg/3NMcUV5



Dalamud is licensed under AGPL v3 or later
Contribute at: https://github.com/goatsoft/Dalamud
";

    private readonly TextureWrap logoTexture;
    private readonly Stopwatch creditsThrottler;

    private string creditsText;

    private GameFontHandle? thankYouFont;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreditsWindow"/> class.
    /// </summary>
    public CreditsWindow()
        : base("Dalamud Credits", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar, true)
    {
        var dalamud = Service<Dalamud>.Get();
        var interfaceManager = Service<InterfaceManager>.Get();

        this.logoTexture = interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "logo.png"));
        this.creditsThrottler = new();

        this.Size = new Vector2(500, 400);
        this.SizeCondition = ImGuiCond.Always;

        this.PositionCondition = ImGuiCond.Always;

        this.BgAlpha = 0.8f;
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
        var pluginCredits = Service<PluginManager>.Get().InstalledPlugins
                                                  .Where(plugin => plugin.Manifest != null)
                                                  .Select(plugin => $"{plugin.Manifest.Name} by {plugin.Manifest.Author}\n")
                                                  .Aggregate(string.Empty, (current, next) => $"{current}{next}");

        this.creditsText = string.Format(CreditsTextTempl, typeof(Dalamud).Assembly.GetName().Version, pluginCredits, Util.GetGitHashClientStructs());

        Service<GameGui>.Get().SetBgm(833);
        this.creditsThrottler.Restart();

        if (this.thankYouFont == null)
        {
            var gfm = Service<GameFontManager>.Get();
            this.thankYouFont = gfm.NewFontRef(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic34));
        }
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
        this.creditsThrottler.Reset();
        Service<GameGui>.Get().SetBgm(9999);
    }

    /// <inheritdoc/>
    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

        base.PreDraw();
    }

    /// <inheritdoc/>
    public override void PostDraw()
    {
        ImGui.PopStyleVar();

        base.PostDraw();
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var screenSize = ImGui.GetMainViewport().Size;
        var windowSize = ImGui.GetWindowSize();

        this.Position = (screenSize - windowSize) / 2;

        ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGuiHelpers.ScaledDummy(0, windowSize.Y + 20f);
        ImGui.Text(string.Empty);

        const float imageSize = 190f;
        ImGui.SameLine((ImGui.GetWindowWidth() / 2) - (imageSize / 2));
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
            ImGui.PushFont(this.thankYouFont.ImFont);
            var thankYouLenX = ImGui.CalcTextSize(ThankYouText).X;

            ImGui.Dummy(new Vector2((windowX / 2) - (thankYouLenX / 2), 0f));
            ImGui.SameLine();
            ImGui.TextUnformatted(ThankYouText);

            ImGui.PopFont();
        }

        ImGuiHelpers.ScaledDummy(0, windowSize.Y + 50f);

        ImGui.PopStyleVar();

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

        ImGui.EndChild();

        ImGui.SetCursorPos(new Vector2(0));
        ImGui.BeginChild("button", Vector2.Zero, false, ImGuiWindowFlags.NoScrollbar);

        var closeButtonSize = new Vector2(30);
        ImGui.PushFont(InterfaceManager.IconFont);
        ImGui.SetCursorPos(new Vector2(windowSize.X - closeButtonSize.X - 5, 5));
        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

        if (ImGui.Button(FontAwesomeIcon.Times.ToIconString(), closeButtonSize))
        {
            this.IsOpen = false;
        }

        ImGui.PopStyleColor(3);
        ImGui.PopFont();
        ImGui.EndChild();
    }

    /// <summary>
    /// Disposes of managed and unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        this.logoTexture?.Dispose();
        this.thankYouFont?.Dispose();
    }
}
