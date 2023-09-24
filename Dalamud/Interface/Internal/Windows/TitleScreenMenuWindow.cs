using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Class responsible for drawing the main plugin window.
/// </summary>
internal class TitleScreenMenuWindow : Window, IDisposable
{
    private const float TargetFontSizePt = 18f;
    private const float TargetFontSizePx = TargetFontSizePt * 4 / 3;

    private readonly IDalamudTextureWrap shadeTexture;

    private readonly Dictionary<Guid, InOutCubic> shadeEasings = new();
    private readonly Dictionary<Guid, InOutQuint> moveEasings = new();
    private readonly Dictionary<Guid, InOutCubic> logoEasings = new();
    private readonly Dictionary<string, InterfaceManager.SpecialGlyphRequest> specialGlyphRequests = new();

    private InOutCubic? fadeOutEasing;

    private State state = State.Hide;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleScreenMenuWindow"/> class.
    /// </summary>
    public TitleScreenMenuWindow()
        : base(
            "TitleScreenMenuOverlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus)
    {
        this.IsOpen = true;
        this.DisableWindowSounds = true;
        this.ForceMainWindow = true;

        this.Position = new Vector2(0, 200);
        this.PositionCondition = ImGuiCond.Always;
        this.RespectCloseHotkey = false;

        var dalamud = Service<Dalamud>.Get();
        var interfaceManager = Service<InterfaceManager>.Get();

        var shadeTex =
            interfaceManager.LoadImage(Path.Combine(dalamud.AssetDirectory.FullName, "UIRes", "tsmShade.png"));
        this.shadeTexture = shadeTex ?? throw new Exception("Could not load TSM background texture.");

        var framework = Service<Framework>.Get();
        framework.Update += this.FrameworkOnUpdate;
    }

    private enum State
    {
        Hide,
        Show,
        FadeOut,
    }

    /// <inheritdoc/>
    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        base.PreDraw();
    }

    /// <inheritdoc/>
    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        base.PostDraw();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.shadeTexture.Dispose();
        var framework = Service<Framework>.Get();
        framework.Update -= this.FrameworkOnUpdate;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        var scale = ImGui.GetIO().FontGlobalScale;
        var entries = Service<TitleScreenMenu>.Get().Entries
                                              .OrderByDescending(x => x.IsInternal)
                                              .ToList();

        switch (this.state)
        {
            case State.Show:
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    if (!this.moveEasings.TryGetValue(entry.Id, out var moveEasing))
                    {
                        moveEasing = new InOutQuint(TimeSpan.FromMilliseconds(400));
                        this.moveEasings.Add(entry.Id, moveEasing);
                    }

                    if (!moveEasing.IsRunning && !moveEasing.IsDone)
                    {
                        moveEasing.Restart();
                    }

                    if (moveEasing.IsDone)
                    {
                        moveEasing.Stop();
                    }

                    moveEasing.Update();

                    var finalPos = (i + 1) * this.shadeTexture.Height * scale;
                    var pos = moveEasing.Value * finalPos;

                    // FIXME(goat): Sometimes, easings can overshoot and bring things out of alignment.
                    if (moveEasing.IsDone)
                    {
                        pos = finalPos;
                    }

                    this.DrawEntry(entry, moveEasing.IsRunning && i != 0, true, i == 0, true, moveEasing.IsDone);

                    var cursor = ImGui.GetCursorPos();
                    cursor.Y = (float)pos;
                    ImGui.SetCursorPos(cursor);
                }

                if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows |
                                           ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
                {
                    this.state = State.FadeOut;
                }

                break;
            }

            case State.FadeOut:
            {
                this.fadeOutEasing ??= new InOutCubic(TimeSpan.FromMilliseconds(400))
                {
                    IsInverse = true,
                };

                if (!this.fadeOutEasing.IsRunning && !this.fadeOutEasing.IsDone)
                {
                    this.fadeOutEasing.Restart();
                }

                if (this.fadeOutEasing.IsDone)
                {
                    this.fadeOutEasing.Stop();
                }

                this.fadeOutEasing.Update();

                using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, (float)this.fadeOutEasing.Value))
                {
                    for (var i = 0; i < entries.Count; i++)
                    {
                        var entry = entries[i];

                        var finalPos = (i + 1) * this.shadeTexture.Height * scale;

                        this.DrawEntry(entry, i != 0, true, i == 0, false, false);

                        var cursor = ImGui.GetCursorPos();
                        cursor.Y = finalPos;
                        ImGui.SetCursorPos(cursor);
                    }
                }

                var isHover = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows |
                                                    ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

                if (!isHover && this.fadeOutEasing!.IsDone)
                {
                    this.state = State.Hide;
                    this.fadeOutEasing = null;
                }
                else if (isHover)
                {
                    this.state = State.Show;
                    this.fadeOutEasing = null;
                }

                break;
            }

            case State.Hide:
            {
                if (this.DrawEntry(entries[0], true, false, true, true, false))
                {
                    this.state = State.Show;
                }

                this.moveEasings.Clear();
                this.logoEasings.Clear();
                this.shadeEasings.Clear();
                break;
            }
        }

        var srcText = entries.Select(e => e.Name).ToHashSet();
        var keys = this.specialGlyphRequests.Keys.ToHashSet();
        keys.RemoveWhere(x => srcText.Contains(x));
        foreach (var key in keys)
        {
            this.specialGlyphRequests[key].Dispose();
            this.specialGlyphRequests.Remove(key);
        }
    }

    private bool DrawEntry(
        TitleScreenMenuEntry entry, bool inhibitFadeout, bool showText, bool isFirst, bool overrideAlpha, bool interactable)
    {
        InterfaceManager.SpecialGlyphRequest fontHandle;
        if (this.specialGlyphRequests.TryGetValue(entry.Name, out fontHandle) && fontHandle.Size != TargetFontSizePx)
        {
            fontHandle.Dispose();
            this.specialGlyphRequests.Remove(entry.Name);
            fontHandle = null;
        }

        if (fontHandle == null)
            this.specialGlyphRequests[entry.Name] = fontHandle = Service<InterfaceManager>.Get().NewFontSizeRef(TargetFontSizePx, entry.Name);

        ImGui.PushFont(fontHandle.Font);
        ImGui.SetWindowFontScale(TargetFontSizePx / fontHandle.Size);

        var scale = ImGui.GetIO().FontGlobalScale;

        if (!this.shadeEasings.TryGetValue(entry.Id, out var shadeEasing))
        {
            shadeEasing = new InOutCubic(TimeSpan.FromMilliseconds(350));
            this.shadeEasings.Add(entry.Id, shadeEasing);
        }

        var initialCursor = ImGui.GetCursorPos();

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, (float)shadeEasing.Value))
        {
            ImGui.Image(this.shadeTexture.ImGuiHandle, new Vector2(this.shadeTexture.Width * scale, this.shadeTexture.Height * scale));
        }

        var isHover = ImGui.IsItemHovered();
        if (isHover && (!shadeEasing.IsRunning || (shadeEasing.IsDone && shadeEasing.IsInverse)) && !inhibitFadeout)
        {
            shadeEasing.IsInverse = false;
            shadeEasing.Restart();
        }
        else if (!isHover && !shadeEasing.IsInverse && shadeEasing.IsRunning && !inhibitFadeout)
        {
            shadeEasing.IsInverse = true;
            shadeEasing.Restart();
        }

        var isClick = ImGui.IsItemClicked();
        if (isClick && interactable)
        {
            entry.Trigger();
        }

        shadeEasing.Update();

        if (!this.logoEasings.TryGetValue(entry.Id, out var logoEasing))
        {
            logoEasing = new InOutCubic(TimeSpan.FromMilliseconds(350));
            this.logoEasings.Add(entry.Id, logoEasing);
        }

        if (!logoEasing.IsRunning && !logoEasing.IsDone)
        {
            logoEasing.Restart();
        }

        if (logoEasing.IsDone)
        {
            logoEasing.Stop();
        }

        logoEasing.Update();

        ImGui.SetCursorPos(initialCursor);
        ImGuiHelpers.ScaledDummy(5);
        ImGui.SameLine();

        if (overrideAlpha)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, isFirst ? 1f : (float)logoEasing.Value);
        }
        else if (isFirst)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);
        }

        ImGui.Image(entry.Texture.ImGuiHandle, new Vector2(TitleScreenMenu.TextureSize * scale));
        if (overrideAlpha || isFirst)
        {
            ImGui.PopStyleVar();
        }

        ImGui.SameLine();

        ImGuiHelpers.ScaledDummy(10);
        ImGui.SameLine();

        var textHeight = ImGui.GetTextLineHeightWithSpacing();
        var cursor = ImGui.GetCursorPos();

        cursor.Y += (entry.Texture.Height * scale / 2) - (textHeight / 2);

        if (overrideAlpha)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, showText ? (float)logoEasing.Value : 0f);
        }

        // Drop shadow
        using (ImRaii.PushColor(ImGuiCol.Text, 0xFF000000))
        {
            for (int i = 0, i_ = (int)Math.Ceiling(1 * scale); i < i_; i++)
            {
                ImGui.SetCursorPos(new Vector2(cursor.X, cursor.Y + i));
                ImGui.Text(entry.Name);
            }
        }

        ImGui.SetCursorPos(cursor);
        ImGui.Text(entry.Name);

        if (overrideAlpha)
        {
            ImGui.PopStyleVar();
        }

        initialCursor.Y += entry.Texture.Height * scale;
        ImGui.SetCursorPos(initialCursor);

        ImGui.PopFont();

        return isHover;
    }

    private void FrameworkOnUpdate(IFramework framework)
    {
        var clientState = Service<ClientState>.Get();
        this.IsOpen = !clientState.IsLoggedIn;

        var configuration = Service<DalamudConfiguration>.Get();
        if (!configuration.ShowTsm)
            this.IsOpen = false;

        var gameGui = Service<GameGui>.Get();
        var charaSelect = gameGui.GetAddonByName("CharaSelect", 1);
        var charaMake = gameGui.GetAddonByName("CharaMake", 1);
        var titleDcWorldMap = gameGui.GetAddonByName("TitleDCWorldMap", 1);
        if (charaMake != IntPtr.Zero || charaSelect != IntPtr.Zero || titleDcWorldMap != IntPtr.Zero)
            this.IsOpen = false;
    }
}
