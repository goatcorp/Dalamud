using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.DesignSystem;
using Dalamud.Interface.Internal.Windows.StyleEditor;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing.Persistence;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.UI;

using TerraFX.Interop.Windows;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Base class you can use to implement an ImGui window for use with the built-in <see cref="WindowSystem"/>.
/// </summary>
public class WindowHost
{
    private const float FadeInOutTime = 0.072f;
    private const float FocusFadeTime = 0.062f;
    private const float BlurNoiseOpacity = 0.17f;
    private const float MaxBlurStrength = 14f;
    private const string AdditionsPopupName = "WindowSystemContextActions";

    private static readonly ModuleLog Log = ModuleLog.Create<WindowSystem>();

    private static bool wasEscPressedLastFrame = false;

    private readonly TitleBarButton additionsButton;
    private readonly List<TitleBarButton> allButtons = [];

    private bool internalLastIsOpen = false;
    private bool didPushInternalAlpha = false;
    private float? internalAlpha = null;

    private float? internalBlurFactorOverride = null;

    private bool hasInitializedFromPreset = false;
    private PresetModel.PresetWindow? presetWindow;
    private bool presetDirty = false;

    private bool pushedFadeInAlpha = false;
    private float fadeInTimer = 0f;
    private float fadeOutTimer = 0f;
    private IDrawListTextureWrap? fadeOutTexture = null;
    private Vector2 fadeOutSize = Vector2.Zero;
    private Vector2 fadeOutOrigin = Vector2.Zero;

    private bool hasError = false;
    private Exception? lastError;

    private float focusTransitionProgress = 0f;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowHost"/> class.
    /// </summary>
    /// <param name="window">A plugin provided window.</param>
    internal WindowHost(IWindow window)
    {
        this.Window = window;

        this.additionsButton = new()
        {
            Icon = FontAwesomeIcon.Bars,
            IconOffset = new Vector2(2.5f, 1),
            Click = _ =>
            {
                this.Window.IsClickthrough = false;
                this.presetDirty = true;
                ImGui.OpenPopup(AdditionsPopupName);
            },
            Priority = int.MinValue,
            AvailableClickthrough = true,
        };
    }

    /// <summary>
    /// Flags to control window behavior.
    /// </summary>
    [Flags]
    internal enum WindowDrawFlags
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None = 0,

        /// <summary>
        /// Enable window opening/closing sound effects.
        /// </summary>
        UseSoundEffects = 1 << 0,

        /// <summary>
        /// Hook into the game's focus management.
        /// </summary>
        UseFocusManagement = 1 << 1,

        /// <summary>
        /// Enable the built-in "additional options" menu on the title bar.
        /// </summary>
        UseAdditionalOptions = 1 << 2,

        /// <summary>
        /// Do not draw non-critical animations.
        /// </summary>
        IsReducedMotion = 1 << 3,
    }

    /// <summary>
    /// Gets or sets the backing window provided by the plugin.
    /// </summary>
    public IWindow Window { get; set; }

    private bool CanShowCloseButton => this.Window.ShowCloseButton && !this.Window.IsClickthrough;

    /// <summary>
    /// Draw the window via ImGui.
    /// </summary>
    /// <param name="internalDrawParams">Parameters controlling window behavior.</param>
    /// <param name="persistence">Handler for window persistence data.</param>
    internal void DrawInternal(WindowDrawParameters internalDrawParams, WindowSystemPersistence? persistence)
    {
        this.Window.PreOpenCheck();
        var doFades = !internalDrawParams.Flags.HasFlag(WindowDrawFlags.IsReducedMotion) && !this.Window.DisableFadeInFadeOut;

        if (!this.Window.IsOpen)
        {
            if (this.Window.IsOpen != this.internalLastIsOpen)
            {
                this.internalLastIsOpen = this.Window.IsOpen;
                this.Window.OnClose();

                this.Window.IsFocused = false;
                this.Window.IsHovered = false;

                if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.Window.DisableWindowSounds)
                {
                    unsafe
                    {
                        UIGlobals.PlaySoundEffect(this.Window.OnCloseSfxId);
                    }
                }
            }

            if (this.fadeOutTexture != null)
            {
                this.fadeOutTimer -= ImGui.GetIO().DeltaTime;
                if (this.fadeOutTimer <= 0f)
                {
                    this.fadeOutTexture.Dispose();
                    this.fadeOutTexture = null;
                    this.Window.OnSafeToRemove();
                }
                else
                {
                    this.DrawFakeFadeOutWindow();
                }
            }

            this.fadeInTimer = doFades ? 0f : FadeInOutTime;
            return;
        }

        this.fadeInTimer += ImGui.GetIO().DeltaTime;
        if (this.fadeInTimer > FadeInOutTime)
            this.fadeInTimer = FadeInOutTime;

        this.Window.Update();

        if (!this.Window.DrawConditions())
        {
            this.Window.IsFocused = false;
            this.Window.IsHovered = false;
            return;
        }

        var hasNamespace = !string.IsNullOrEmpty(this.Window.Namespace);

        if (hasNamespace)
            ImGui.PushID(this.Window.Namespace);

        this.PreHandlePreset(persistence);

        if (this.internalLastIsOpen != this.Window.IsOpen && this.Window.IsOpen)
        {
            this.internalLastIsOpen = this.Window.IsOpen;
            this.Window.OnOpen();

            if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.Window.DisableWindowSounds)
            {
                unsafe
                {
                    UIGlobals.PlaySoundEffect(this.Window.OnOpenSfxId);
                }
            }
        }

        var isErrorStylePushed = false;
        if (!this.hasError)
        {
            if (this.internalAlpha.HasValue)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, this.internalAlpha.Value);
                this.didPushInternalAlpha = true;
            }

            this.Window.PreDraw();
            this.ApplyConditionals();
        }
        else
        {
            Style.StyleModelV1.DalamudStandard.Push();
            isErrorStylePushed = true;
        }

        if (this.Window.ForceMainWindow)
            ImGuiHelpers.ForceNextWindowMainViewport();

        var wasFocused = this.Window.IsFocused;

        // Smoothly fade title and tint colors bar when switching between active/inactive
        if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.IsReducedMotion))
        {
            this.focusTransitionProgress = wasFocused ? 1f : 0f;
        }
        else
        {
            var focusFadeStep = ImGui.GetIO().DeltaTime / FocusFadeTime;
            this.focusTransitionProgress = Math.Clamp(
                this.focusTransitionProgress + (wasFocused ? focusFadeStep : -focusFadeStep),
                0f,
                1f);
        }

        var t = this.focusTransitionProgress;
        var easedFocusProgress = 1f - (1f - t) * (1f - t) * (1f - t);

        if (this.Window is not StyleEditorWindow)
        {
            var style = ImGui.GetStyle();
            var lerpedTitleBgColor = Vector4.Lerp(
                style.Colors[(int)ImGuiCol.TitleBg],
                style.Colors[(int)ImGuiCol.TitleBgActive],
                easedFocusProgress);
            ImGui.PushStyleColor(ImGuiCol.TitleBg, lerpedTitleBgColor);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, lerpedTitleBgColor);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, lerpedTitleBgColor);
        }

        if (this.Window.RequestFocus)
        {
            ImGui.SetNextWindowFocus();
            this.Window.RequestFocus = false;
        }

        var flags = this.Window.Flags;

        if (this.Window.IsPinned || this.Window.IsClickthrough)
        {
            flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        }

        if (this.Window.IsClickthrough)
        {
            flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMouseInputs;
        }

        // If we have an error, reset all flags to default, and unlock window size.
        if (this.hasError)
        {
            flags = ImGuiWindowFlags.None;
            ImGui.SetNextWindowCollapsed(false, ImGuiCond.Once);
            ImGui.SetNextWindowSizeConstraints(Vector2.Zero, Vector2.PositiveInfinity);
        }

        // Determine window background alpha
        float effectiveWindowBgAlpha;
        {
            ref var nextWindowData = ref ImGui.GetCurrentContext().NextWindowData;
            effectiveWindowBgAlpha = ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg].W;
            if (nextWindowData.Flags.HasFlag(ImGuiNextWindowDataFlags.HasBgAlpha))
            {
                effectiveWindowBgAlpha = nextWindowData.BgAlphaVal;
            }

            if (flags.HasFlag(ImGuiWindowFlags.NoBackground))
            {
                effectiveWindowBgAlpha = 0;
            }
        }

        var windowHasBackground = effectiveWindowBgAlpha != 0f;

        var isWindowOpen = this.Window.IsOpen;
        if (this.CanShowCloseButton ? ImGui.Begin(this.Window.WindowName, ref isWindowOpen, flags) : ImGui.Begin(this.Window.WindowName, flags))
        {
            // Apply background blur
            {
                var effectiveBlurFactor = this.internalBlurFactorOverride ?? internalDrawParams.DefaultBackgroundBlurStrength;
                var shouldBlur = this.Window.AllowBackgroundBlur &&
                                 effectiveBlurFactor != 0f &&
                                 ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID &&
                                 windowHasBackground;

                if (shouldBlur)
                {
                    var wPos = ImGui.GetWindowPos();
                    ImGuiHelpers.PrependBlurBehind(
                        ImGui.GetWindowDrawList(),
                        wPos,
                        wPos + ImGui.GetWindowSize(),
                        float.Lerp(0.005f, effectiveBlurFactor, this.internalAlpha ?? 1f) * MaxBlurStrength,
                        ImGui.GetStyle().WindowRounding,
                        tintColor: Vector4.Lerp(internalDrawParams.DefaultBackgroundBlurTint, internalDrawParams.DefaultBackgroundBlurTintActive, easedFocusProgress),
                        noiseOpacity: float.Lerp(0.09f, 1f, effectiveWindowBgAlpha * this.internalAlpha ?? 1f) * BlurNoiseOpacity,
                        luminosityColor: internalDrawParams.DefaultBackgroundBlurLuminosity);
                }
            }

            var context = ImGui.GetCurrentContext();
            if (!context.IsNull)
            {
                ImGuiP.GetCurrentWindow().InheritNoInputs = this.Window.IsClickthrough;
            }

            if (ImGui.GetWindowViewport().ID != ImGui.GetMainViewport().ID)
            {
                if ((flags & ImGuiWindowFlags.NoInputs) == ImGuiWindowFlags.NoInputs)
                    ImGui.GetWindowViewport().Flags |= ImGuiViewportFlags.NoInputs;
                else
                    ImGui.GetWindowViewport().Flags &= ~ImGuiViewportFlags.NoInputs;
            }

            if (this.hasError)
            {
                this.DrawErrorMessage();
            }
            else
            {
                // Draw the actual window contents
                try
                {
                    this.Window.Draw();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during Draw(): {WindowName}", this.Window.WindowName);

                    this.hasError = true;
                    this.lastError = ex;
                }
            }
        }

        if (this.Window.IsOpen && !isWindowOpen)
        {
            this.Window.IsOpen = false;
        }

        // VP handling
        var windowViewport = ImGui.GetWindowViewport();
        var isMainViewport = windowViewport.ID == ImGui.GetMainViewport().ID;
        if (!isMainViewport)
        {
            if (this.Window.IsTopMost)
                ImGui.GetWindowViewport().Flags |= ImGuiViewportFlags.TopMost;
            else
                ImGui.GetWindowViewport().Flags &= ~ImGuiViewportFlags.TopMost;
        }

        var flagsApplicableForTitleBarIcons = !flags.HasFlag(ImGuiWindowFlags.NoDecoration) &&
                                              !flags.HasFlag(ImGuiWindowFlags.NoTitleBar);
        var showAdditions = (this.Window.AllowPinning || this.Window.AllowClickthrough || this.Window.AllowBackgroundBlur) &&
                            internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseAdditionalOptions) &&
                            flagsApplicableForTitleBarIcons;
        var printWindow = false;
        if (showAdditions)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);

            if (ImGui.BeginPopup(AdditionsPopupName, ImGuiWindowFlags.NoMove))
            {
                if (this.Window.IsClickthrough)
                    ImGui.BeginDisabled();

                if (this.Window.AllowPinning)
                {
                    var showAsPinned = this.Window.IsPinned || this.Window.IsClickthrough;
                    if (ImGui.Checkbox(Loc.Localize("WindowSystemContextActionPin", "Pin Window"), ref showAsPinned))
                    {
                        this.Window.IsPinned = showAsPinned;
                        this.presetDirty = true;
                    }

                    ImGuiComponents.HelpMarker(
                        Loc.Localize("WindowSystemContextActionPinHint", "Pinned windows will not move or resize when you click and drag them, nor will they close when escape is pressed."));
                }

                if (this.Window.IsClickthrough)
                    ImGui.EndDisabled();

                if (this.Window.AllowClickthrough)
                {
                    var isClickthrough = this.Window.IsClickthrough;
                    if (ImGui.Checkbox(
                            Loc.Localize("WindowSystemContextActionClickthrough", "Make clickthrough"),
                            ref isClickthrough))
                    {
                        this.Window.IsClickthrough = isClickthrough;
                        this.presetDirty = true;
                    }

                    ImGuiComponents.HelpMarker(
                        Loc.Localize("WindowSystemContextActionClickthroughHint", "Clickthrough windows will not receive mouse input, move or resize. They are completely inert."));

                    if (!isMainViewport)
                    {
                        var isTopMost = this.Window.IsTopMost;
                        if (ImGui.Checkbox(
                                Loc.Localize("WindowSystemContextActionTopMost", "Stay on top"),
                                ref isTopMost))
                        {
                            this.Window.IsTopMost = isTopMost;
                            this.presetDirty = true;
                        }

                        ImGuiComponents.HelpMarker(
                            Loc.Localize("WindowSystemContextActionTopMostHint", "Stay-on-top windows will not move into the background."));
                    }
                }

                ImGuiHelpers.ScaledDummy(5);

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                using (ImRaii.Disabled(this.internalAlpha == null || this.internalAlpha == ImGui.GetStyle().Alpha))
                {
                    if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset") + "##resetAlpha"))
                    {
                        this.internalAlpha = null;
                        this.presetDirty = true;
                    }
                }

                ImGui.SameLine();

                var alpha = (this.internalAlpha ?? ImGui.GetStyle().Alpha) * 100f;

                if (ImGui.SliderFloat(Loc.Localize("WindowSystemContextActionAlpha", "Opacity"), ref alpha, 20f,
                                      100f, "%.1f%%"))
                {
                    this.internalAlpha = Math.Clamp(alpha / 100f, 0.2f, 1f);
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    this.presetDirty = true;
                }

                if (this.Window.AllowBackgroundBlur)
                {
                    using var disabled = ImRaii.Disabled(!isMainViewport);

                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    using (ImRaii.Disabled(this.internalBlurFactorOverride == null || this.internalBlurFactorOverride == internalDrawParams.DefaultBackgroundBlurStrength))
                    {
                        if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset") + "##resetBlur"))
                        {
                            this.internalBlurFactorOverride = null;
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            this.presetDirty = true;
                        }
                    }

                    ImGui.SameLine();

                    var blurOverride =
                        (this.internalBlurFactorOverride ?? internalDrawParams.DefaultBackgroundBlurStrength) * 100f;
                    if (ImGui.SliderFloat(Loc.Localize("WindowSystemContextActionBlur", "Background Blur"), ref blurOverride, 0f, 100f, "%.1f%%"))
                    {
                        this.internalBlurFactorOverride = blurOverride / 100f;
                        this.presetDirty = true;
                    }

                    if (!isMainViewport && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip(Loc.Localize("WindowSystemContextActionBlurDisabledHint", "Background blur only takes effect when the window is inside the game window."));
                    }
                }

                if (this.Window.AllowClickthrough)
                {
                    ImGui.TextColored(
                        ImGuiColors.DalamudGrey,
                        Loc.Localize(
                            "WindowSystemContextActionClickthroughDisclaimer",
                            "Open this menu again by clicking the three dashes to disable clickthrough."));
                }

                if (ImGui.Button(Loc.Localize("WindowSystemContextActionPrintWindow", "Print window")))
                    printWindow = true;

                ImGui.EndPopup();
            }

            ImGui.PopStyleVar();
        }

        if (flagsApplicableForTitleBarIcons)
        {
            this.allButtons.Clear();
            this.allButtons.EnsureCapacity(this.Window.TitleBarButtons.Count + 1);
            this.allButtons.AddRange(this.Window.TitleBarButtons);
            if (showAdditions)
                this.allButtons.Add(this.additionsButton);
            this.allButtons.Sort(static (a, b) => b.Priority - a.Priority);
            this.DrawTitleBarButtons();
        }

        if (this.Window is not StyleEditorWindow)
        {
            ImGui.PopStyleColor(3);
        }

        this.Window.IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
        this.Window.IsHovered = ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows);

        if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseFocusManagement) && !this.Window.IsPinned)
        {
            var escapeDown = Service<KeyState>.Get()[VirtualKey.ESCAPE];
            if (escapeDown && this.Window.IsFocused && !wasEscPressedLastFrame && this.Window.RespectCloseHotkey)
            {
                this.Window.IsOpen = false;
                wasEscPressedLastFrame = true;
            }
            else if (!escapeDown && wasEscPressedLastFrame)
            {
                wasEscPressedLastFrame = false;
            }
        }

        this.fadeOutSize = ImGui.GetWindowSize();
        this.fadeOutOrigin = ImGui.GetWindowPos();
        var isCollapsed = ImGui.IsWindowCollapsed();
        var isDocked = ImGui.IsWindowDocked();

        ImGui.End();

        if (this.pushedFadeInAlpha)
        {
            ImGui.PopStyleVar();
            this.pushedFadeInAlpha = false;
        }

        // TODO: No fade-out if the window is collapsed. We could do this if we knew the "FullSize" of the window
        // from the internal ImGuiWindow, but I don't want to mess with that here for now. We can do this a lot
        // easier with the new bindings.
        // TODO: No fade-out if docking is enabled and the window is docked, since this makes them "unsnap".
        // Ideally we should get rid of this "fake window" thing and just insert a new drawlist at the correct spot.
        if (!this.Window.IsOpen && this.fadeOutTexture == null && doFades && !isCollapsed && !isDocked)
        {
            this.fadeOutTexture = Service<TextureManager>.Get().CreateDrawListTexture(
                "WindowFadeOutTexture");
            this.fadeOutTexture.ResizeAndDrawWindow(this.Window.WindowName, Vector2.One);
            this.fadeOutTimer = FadeInOutTime;
        }

        if (printWindow)
        {
            var tex = Service<TextureManager>.Get().CreateDrawListTexture(
                Loc.Localize("WindowSystemContextActionPrintWindow", "Print window"));
            tex.ResizeAndDrawWindow(this.Window.WindowName, Vector2.One);
            _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                this.Window.WindowName,
                this.Window.WindowName,
                Task.FromResult<IDalamudTextureWrap>(tex));
        }

        if (isErrorStylePushed)
        {
            Style.StyleModelV1.DalamudStandard.Pop();
        }
        else
        {
            if (this.didPushInternalAlpha)
            {
                ImGui.PopStyleVar();
                this.didPushInternalAlpha = false;
            }

            this.Window.PostDraw();
        }

        this.PostHandlePreset(persistence);

        if (hasNamespace)
            ImGui.PopID();
    }

    private unsafe void ApplyConditionals()
    {
        if (this.Window.Position.HasValue)
        {
            var pos = this.Window.Position.Value;

            if (this.Window.ForceMainWindow)
                pos += ImGuiHelpers.MainViewport.Pos;

            ImGui.SetNextWindowPos(pos, this.Window.PositionCondition);
        }

        if (this.Window.Size.HasValue)
        {
            ImGui.SetNextWindowSize(this.Window.Size.Value * ImGuiHelpers.GlobalScale, this.Window.SizeCondition);
        }

        if (this.Window.Collapsed.HasValue)
        {
            ImGui.SetNextWindowCollapsed(this.Window.Collapsed.Value, this.Window.CollapsedCondition);
        }

        if (this.Window.SizeConstraints.HasValue)
        {
            var (min, max) = this.GetValidatedConstraints(this.Window.SizeConstraints.Value);
            ImGui.SetNextWindowSizeConstraints(
                min * ImGuiHelpers.GlobalScale,
                max * ImGuiHelpers.GlobalScale);
        }

        var maxBgAlpha = this.Window.BgAlpha;
        var fadeInAlpha = this.fadeInTimer / FadeInOutTime;
        if (fadeInAlpha < 1f)
        {
            maxBgAlpha = maxBgAlpha.HasValue ?
                             Math.Clamp(maxBgAlpha.Value * fadeInAlpha, 0f, 1f) :
                             (*ImGui.GetStyleColorVec4(ImGuiCol.WindowBg)).W * fadeInAlpha;
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * fadeInAlpha);
            this.pushedFadeInAlpha = true;
        }

        if (maxBgAlpha.HasValue)
        {
            ImGui.SetNextWindowBgAlpha(maxBgAlpha.Value);
        }
    }

    private (Vector2 Min, Vector2 Max) GetValidatedConstraints(WindowSizeConstraints constraints)
    {
        var min = constraints.MinimumSize;
        var max = constraints.MaximumSize;

        // If max < min, treat as "no constraint" (float.MaxValue)
        if (max.X < min.X || max.Y < min.Y)
            max = new Vector2(float.MaxValue);

        return (min, max);
    }

    private void PreHandlePreset(WindowSystemPersistence? persistence)
    {
        if (persistence == null || this.hasInitializedFromPreset)
            return;

        var id = ImGui.GetID(this.Window.WindowName);
        this.presetWindow = persistence.GetWindow(id);

        this.hasInitializedFromPreset = true;

        // Fresh preset - don't apply anything
        if (this.presetWindow == null)
        {
            this.presetWindow = new PresetModel.PresetWindow();
            this.presetDirty = true;
            return;
        }

        this.Window.IsPinned = this.presetWindow.IsPinned;
        this.Window.IsClickthrough = this.presetWindow.IsClickThrough;
        this.Window.IsTopMost = this.presetWindow.IsTopMost;
        this.internalAlpha = this.presetWindow.Alpha;
        this.internalBlurFactorOverride = this.presetWindow.BlurFactorOverride;
    }

    private void PostHandlePreset(WindowSystemPersistence? persistence)
    {
        if (persistence == null)
            return;

        Debug.Assert(this.presetWindow != null, "this.presetWindow != null");

        if (this.presetDirty)
        {
            this.presetWindow.IsPinned = this.Window.IsPinned;
            this.presetWindow.IsClickThrough = this.Window.IsClickthrough;
            this.presetWindow.IsTopMost = this.Window.IsTopMost;
            this.presetWindow.Alpha = this.internalAlpha;
            this.presetWindow.BlurFactorOverride = this.internalBlurFactorOverride;

            var id = ImGui.GetID(this.Window.WindowName);
            persistence.SaveWindow(id, this.presetWindow!);
            this.presetDirty = false;

            Log.Verbose("Saved preset for {WindowName}", this.Window.WindowName);
        }
    }

    private unsafe void DrawTitleBarButtons()
    {
        var window = ImGuiP.GetCurrentWindow();
        var flags = window.Flags;
        var titleBarRect = window.TitleBarRect();
        ImGui.PushClipRect(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), false);

        var style = ImGui.GetStyle();
        var fontSize = ImGui.GetFontSize();
        var drawList = ImGui.GetWindowDrawList();

        var padR = 0f;
        var buttonSize = ImGui.GetFontSize();

        var numNativeButtons = 0;
        if (this.CanShowCloseButton)
            numNativeButtons++;

        if (!flags.HasFlag(ImGuiWindowFlags.NoCollapse) && style.WindowMenuButtonPosition == ImGuiDir.Right)
            numNativeButtons++;

        // If there are no native buttons, pad from the right to make some space
        if (numNativeButtons == 0)
            padR += style.FramePadding.X;

        // Pad to the left, to get out of the way of the native buttons
        padR += numNativeButtons * (buttonSize + style.ItemInnerSpacing.X);

        Vector2 GetCenter(ImRect rect) => new((rect.Min.X + rect.Max.X) * 0.5f, (rect.Min.Y + rect.Max.Y) * 0.5f);

        var numButtons = 0;
        bool DrawButton(TitleBarButton button, Vector2 pos)
        {
            var id = ImGui.GetID($"###CustomTbButton{numButtons}");
            numButtons++;

            var max = pos + new Vector2(fontSize, fontSize);
            ImRect bb = new(pos, max);
            var isClipped = !ImGuiP.ItemAdd(bb, id, null, 0);
            bool hovered, held, pressed;

            if (this.Window.IsClickthrough)
            {
                // ButtonBehavior does not function if the window is clickthrough, so we have to do it ourselves
                var pad = ImGui.GetStyle().TouchExtraPadding;
                var rect = new ImRect(pos - pad, max + pad);
                hovered = rect.Contains(ImGui.GetMousePos());

                // Temporarily enable inputs
                // This will be reset on next frame, and then enabled again if it is still being hovered
                if (hovered && ImGui.GetWindowViewport().ID != ImGui.GetMainViewport().ID)
                    ImGui.GetWindowViewport().Flags &= ~ImGuiViewportFlags.NoInputs;

                // We can't use ImGui native functions here, because they don't work with clickthrough
                pressed = held = hovered && (Windows.Win32.PInvoke.GetKeyState(VK.VK_LBUTTON) & 0x8000) != 0;
            }
            else
            {
                pressed = ImGuiP.ButtonBehavior(bb, id, &hovered, &held, ImGuiButtonFlags.None);
            }

            if (isClipped)
                return pressed;

            // Render
            var bgCol = ImGui.GetColorU32((held && hovered) ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
            var textCol = button.IconColor.HasValue ? ImGui.GetColorU32(button.IconColor.Value) : ImGui.GetColorU32(ImGuiCol.Text);
            if (hovered || held)
                drawList.AddCircleFilled(GetCenter(bb) + new Vector2(0.0f, -0.5f), (fontSize * 0.5f) + 1.0f, bgCol);

            var offset = button.IconOffset * ImGuiHelpers.GlobalScale;
            drawList.AddText(InterfaceManager.IconFont, (float)(fontSize * 0.8), new Vector2(bb.Min.X + offset.X, bb.Min.Y + offset.Y), textCol, button.Icon.ToIconString());

            if (hovered)
                button.ShowTooltip?.Invoke();

            // Switch to moving the window after mouse is moved beyond the initial drag threshold
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !this.Window.IsClickthrough)
                ImGuiP.StartMouseMovingWindow(window);

            return pressed;
        }

        foreach (var button in this.allButtons)
        {
            if (this.Window.IsClickthrough && !button.AvailableClickthrough)
                continue;

            Vector2 position = new(titleBarRect.Max.X - padR - buttonSize, titleBarRect.Min.Y + style.FramePadding.Y);
            padR += buttonSize + style.ItemInnerSpacing.X;

            if (DrawButton(button, position))
                button.Click?.Invoke(ImGuiMouseButton.Left);
        }

        ImGui.PopClipRect();
    }

    private void DrawFakeFadeOutWindow()
    {
        // Draw a fake window to fade out, so that the fade out texture stays in the right place in the
        // focus order
        ImGui.SetNextWindowPos(this.fadeOutOrigin);
        ImGui.SetNextWindowSize(this.fadeOutSize);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        style.Push(ImGuiStyleVar.WindowBorderSize, 0);
        style.Push(ImGuiStyleVar.FrameBorderSize, 0);

        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav |
                                       ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMouseInputs |
                                       ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground;

        if (ImGui.Begin(this.Window.WindowName, flags))
        {
            var dl = ImGui.GetWindowDrawList();
            dl.AddImage(
                this.fadeOutTexture!.Handle,
                this.fadeOutOrigin,
                this.fadeOutOrigin + this.fadeOutSize,
                Vector2.Zero,
                Vector2.One,
                ImGui.ColorConvertFloat4ToU32(new(1f, 1f, 1f, Math.Clamp(this.fadeOutTimer / FadeInOutTime, 0f, 1f))));
        }

        ImGui.End();
    }

    private void DrawErrorMessage()
    {
        // TODO: Once window systems are services, offer to reload the plugin
        DalamudComponents.DrawErrorDisplay(
            Loc.Localize("WindowSystemErrorOccurred", "An error occurred while rendering this window. Please contact the developer for details."),
            this.lastError,
            [
                (Loc.Localize("WindowSystemErrorRecoverButton", "Attempt to retry"), () =>
                    {
                        this.hasError = false;
                        this.lastError = null;
                    }),
                (Loc.Localize("WindowSystemErrorClose", "Close Window"), () =>
                {
                    this.Window.IsOpen = false;
                    this.hasError = false;
                    this.lastError = null;
                })
            ]);
    }

    /// <summary>
    /// Parameters used when drawing a window through a <see cref="WindowSystem"/>.
    /// </summary>
    internal struct WindowDrawParameters
    {
        /// <summary>
        /// Gets flags that control window behavior.
        /// </summary>
        public WindowDrawFlags Flags { get; init; }

        /// <summary>
        /// Gets the strength value to be used for background blur, if enabled.
        /// </summary>
        public float DefaultBackgroundBlurStrength { get; init; }

        /// <summary>
        /// Gets the tint value to be used for background blur in inactive windows, if enabled.
        /// </summary>
        public Vector4 DefaultBackgroundBlurTint { get; init; }

        /// <summary>
        /// Gets the tint value to be used for background blur in active windows, if enabled.
        /// </summary>
        public Vector4 DefaultBackgroundBlurTintActive { get; init; }

        /// <summary>
        /// Gets the luminosity adjust value to be used for background blur, if enabled.
        /// </summary>
        public Vector4 DefaultBackgroundBlurLuminosity { get; init; }
    }
}
