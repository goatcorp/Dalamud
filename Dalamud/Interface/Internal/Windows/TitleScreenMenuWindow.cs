using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Console;
using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Internal;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Component.GUI;

using ImGuiNET;

using Lumina.Text.ReadOnly;

using LSeStringBuilder = Lumina.Text.SeStringBuilder;

namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Class responsible for drawing the main plugin window.
/// </summary>
internal class TitleScreenMenuWindow : Window, IDisposable
{
    private const float TargetFontSizePt = 18f;
    private const float TargetFontSizePx = TargetFontSizePt * 4 / 3;

    private readonly ClientState clientState;
    private readonly DalamudConfiguration configuration;
    private readonly GameGui gameGui;
    private readonly TitleScreenMenu titleScreenMenu;

    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly IFontAtlas privateAtlas;
    private readonly Lazy<IFontHandle> myFontHandle;
    private readonly Lazy<IDalamudTextureWrap> shadeTexture;
    private readonly AddonLifecycleEventListener versionStringListener;

    private readonly Dictionary<Guid, InOutCubic> shadeEasings = new();
    private readonly Dictionary<Guid, InOutQuint> moveEasings = new();
    private readonly Dictionary<Guid, InOutCubic> logoEasings = new();

    private readonly IConsoleVariable<bool> showTsm;

    private InOutCubic? fadeOutEasing;

    private State state = State.Hide;

    private int lastLoadedPluginCount = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="TitleScreenMenuWindow"/> class.
    /// </summary>
    /// <param name="clientState">An instance of <see cref="ClientState"/>.</param>
    /// <param name="configuration">An instance of <see cref="DalamudConfiguration"/>.</param>
    /// <param name="dalamudAssetManager">An instance of <see cref="DalamudAssetManager"/>.</param>
    /// <param name="fontAtlasFactory">An instance of <see cref="FontAtlasFactory"/>.</param>
    /// <param name="framework">An instance of <see cref="Framework"/>.</param>
    /// <param name="titleScreenMenu">An instance of <see cref="TitleScreenMenu"/>.</param>
    /// <param name="gameGui">An instance of <see cref="GameGui"/>.</param>
    /// <param name="consoleManager">An instance of <see cref="ConsoleManager"/>.</param>
    /// <param name="addonLifecycle">An instance of <see cref="AddonLifecycle"/>.</param>
    public TitleScreenMenuWindow(
        ClientState clientState,
        DalamudConfiguration configuration,
        DalamudAssetManager dalamudAssetManager,
        FontAtlasFactory fontAtlasFactory,
        Framework framework,
        GameGui gameGui,
        TitleScreenMenu titleScreenMenu,
        ConsoleManager consoleManager,
        AddonLifecycle addonLifecycle)
        : base(
            "TitleScreenMenuOverlay",
            ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus)
    {
        this.showTsm = consoleManager.AddVariable("dalamud.show_tsm", "Show the Title Screen Menu", true);

        this.clientState = clientState;
        this.configuration = configuration;
        this.gameGui = gameGui;
        this.titleScreenMenu = titleScreenMenu;

        this.IsOpen = true;
        this.DisableWindowSounds = true;
        this.ForceMainWindow = true;

        this.Position = new Vector2(0, 200);
        this.PositionCondition = ImGuiCond.Always;
        this.RespectCloseHotkey = false;

        this.shadeTexture = new(() => dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.TitleScreenMenuShade));
        this.privateAtlas = fontAtlasFactory.CreateFontAtlas(this.WindowName, FontAtlasAutoRebuildMode.Async);
        this.scopedFinalizer.Add(this.privateAtlas);

        this.myFontHandle = new(
            () => this.scopedFinalizer.Add(
                this.privateAtlas.NewDelegateFontHandle(
                    e => e.OnPreBuild(
                        toolkit => toolkit.AddDalamudDefaultFont(
                            TargetFontSizePx,
                            titleScreenMenu.Entries.SelectMany(x => x.Name).ToGlyphRange())))));

        titleScreenMenu.EntryListChange += this.TitleScreenMenuEntryListChange;
        this.scopedFinalizer.Add(() => titleScreenMenu.EntryListChange -= this.TitleScreenMenuEntryListChange);

        this.shadeTexture = new(() => dalamudAssetManager.GetDalamudTextureWrap(DalamudAsset.TitleScreenMenuShade));

        framework.Update += this.FrameworkOnUpdate;
        this.scopedFinalizer.Add(() => framework.Update -= this.FrameworkOnUpdate);

        this.versionStringListener = new AddonLifecycleEventListener(AddonEvent.PreDraw, "_TitleRevision", this.OnVersionStringDraw);
        addonLifecycle.RegisterListener(this.versionStringListener);
        this.scopedFinalizer.Add(() => addonLifecycle.UnregisterListener(this.versionStringListener));
    }

    private enum State
    {
        Hide,
        Show,
        FadeOut,
    }

    /// <summary>
    /// Gets or sets a value indicating whether drawing is allowed.
    /// </summary>
    public bool AllowDrawing { get; set; } = true;

    /// <inheritdoc/>
    public void Dispose() => this.scopedFinalizer.Dispose();

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
    public override void Draw()
    {
        if (!this.AllowDrawing || !this.showTsm.Value)
            return;

        var scale = ImGui.GetIO().FontGlobalScale;
        var entries = this.titleScreenMenu.PluginEntries;

        var hovered = ImGui.IsWindowHovered(
            ImGuiHoveredFlags.RootAndChildWindows |
            ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);

        Service<InterfaceManager>.Get().OverrideGameCursor = !hovered;

        switch (this.state)
        {
            case State.Show:
            {
                var i = 0;
                foreach (var entry in entries)
                {
                    if (!entry.IsShowConditionSatisfied())
                        continue;

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

                    var finalPos = (i + 1) * this.shadeTexture.Value.Height * scale;
                    var pos = moveEasing.ValueClamped * finalPos;

                    // FIXME(goat): Sometimes, easings can overshoot and bring things out of alignment.
                    if (moveEasing.IsDone)
                    {
                        pos = finalPos;
                    }

                    this.DrawEntry(entry, moveEasing.IsRunning && i != 0, true, i == 0, true, moveEasing.IsDone);

                    var cursor = ImGui.GetCursorPos();
                    cursor.Y = (float)pos;
                    ImGui.SetCursorPos(cursor);
                    i++;
                }

                // Don't check for hover if we're in the middle of an animation, as it will cause flickering.
                if (this.moveEasings.Any(x => !x.Value.IsDone))
                    break;

                if (!hovered)
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

                using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, (float)this.fadeOutEasing.ValueClamped))
                {
                    var i = 0;
                    foreach (var entry in entries)
                    {
                        if (!entry.IsShowConditionSatisfied())
                            continue;

                        var finalPos = (i + 1) * this.shadeTexture.Value.Height * scale;

                        this.DrawEntry(entry, i != 0, true, i == 0, false, false);

                        var cursor = ImGui.GetCursorPos();
                        cursor.Y = finalPos;
                        ImGui.SetCursorPos(cursor);
                        i++;
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
                if (entries.Count > 0 && this.DrawEntry(entries[0], true, false, true, true, false))
                {
                    this.state = State.Show;
                }

                this.moveEasings.Clear();
                this.logoEasings.Clear();
                this.shadeEasings.Clear();
                break;
            }
        }
    }

    private bool DrawEntry(
        ITitleScreenMenuEntry entry, bool inhibitFadeout, bool showText, bool isFirst, bool overrideAlpha, bool interactable)
    {
        using var fontScopeDispose = this.myFontHandle.Value.Push();

        var scale = ImGui.GetIO().FontGlobalScale;

        if (!this.shadeEasings.TryGetValue(entry.Id, out var shadeEasing))
        {
            shadeEasing = new InOutCubic(TimeSpan.FromMilliseconds(350));
            this.shadeEasings.Add(entry.Id, shadeEasing);
        }

        var initialCursor = ImGui.GetCursorPos();

        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, (float)shadeEasing.ValueClamped))
        {
            var texture = this.shadeTexture.Value;
            ImGui.Image(texture.ImGuiHandle, new Vector2(texture.Width, texture.Height) * scale);
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
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, isFirst ? 1f : (float)logoEasing.ValueClamped);
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
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, showText ? (float)logoEasing.ValueClamped : 0f);
        }

        // Drop shadow
        ImGui.SetCursorPos(cursor);
        ImGuiHelpers.SeStringWrapped(
            ReadOnlySeString.FromText(entry.Name),
            new()
            {
                FontSize = TargetFontSizePx * ImGui.GetIO().FontGlobalScale,
                Edge = true,
                Shadow = true,
            });

        if (overrideAlpha)
        {
            ImGui.PopStyleVar();
        }

        initialCursor.Y += entry.Texture.Height * scale;
        ImGui.SetCursorPos(initialCursor);

        return isHover;
    }

    private void FrameworkOnUpdate(IFramework unused)
    {
        this.IsOpen = !this.clientState.IsLoggedIn;

        if (!this.configuration.ShowTsm)
            this.IsOpen = false;

        var charaSelect = this.gameGui.GetAddonByName("CharaSelect", 1);
        var charaMake = this.gameGui.GetAddonByName("CharaMake", 1);
        var titleDcWorldMap = this.gameGui.GetAddonByName("TitleDCWorldMap", 1);
        if (charaMake != IntPtr.Zero || charaSelect != IntPtr.Zero || titleDcWorldMap != IntPtr.Zero)
            this.IsOpen = false;
    }

    private unsafe void OnVersionStringDraw(AddonEvent ev, AddonArgs args)
    {
        if (args is not AddonDrawArgs drawArgs) return;

        var addon = (AtkUnitBase*)drawArgs.Addon;
        var textNode = addon->GetTextNodeById(3);

        // look and feel init. should be harmless to set.
        textNode->TextFlags |= (byte)TextFlags.MultiLine;
        textNode->AlignmentType = AlignmentType.TopLeft;

        var containsDalamudVersionString = textNode->OriginalTextPointer == textNode->NodeText.StringPtr;
        if (!this.configuration.ShowTsm || !this.showTsm.Value)
        {
            if (containsDalamudVersionString)
                textNode->SetText(addon->AtkValues[1].String);
            this.lastLoadedPluginCount = -1;
            return;
        }

        var pm = Service<PluginManager>.GetNullable();
        var count = pm?.LoadedPluginCount ?? 0;

        // Avoid rebuilding the string every frame.
        if (containsDalamudVersionString && count == this.lastLoadedPluginCount)
            return;
        this.lastLoadedPluginCount = count;

        var lssb = LSeStringBuilder.SharedPool.Get();
        lssb.Append(new ReadOnlySeStringSpan(addon->AtkValues[1].String)).Append("\n\n");
        lssb.PushEdgeColorType(701).PushColorType(539)
            .Append(SeIconChar.BoxedLetterD.ToIconChar())
            .PopColorType().PopEdgeColorType();
        lssb.Append($" Dalamud: {Util.GetScmVersion()}");

        lssb.Append($" - {count} {(count != 1 ? "plugins" : "plugin")} loaded");

        if (pm?.SafeMode is true)
            lssb.PushColorType(17).Append(" [SAFE MODE]").PopColorType();

        textNode->SetText(lssb.GetViewAsSpan());
        LSeStringBuilder.SharedPool.Return(lssb);
    }

    private void TitleScreenMenuEntryListChange() => this.privateAtlas.BuildFontsAsync();
}
