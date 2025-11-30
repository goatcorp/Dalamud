using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using CheapLoc;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing.Persistence;
using Dalamud.Logging.Internal;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Base class you can use to implement an ImGui window for use with the built-in <see cref="WindowSystem"/>.
/// </summary>
public class WindowHost
{
    private const float FadeInOutTime = 0.072f;

    private static readonly ModuleLog Log = new("WindowSystem");

    private static bool wasEscPressedLastFrame = false;

    private bool internalLastIsOpen = false;
    private bool didPushInternalAlpha = false;
    private float? internalAlpha = null;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowHost"/> class.
    /// </summary>
    /// <param name="window">A plugin provided window.</param>
    internal WindowHost(IWindow window)
    {
        this.Window = window;
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
    /// <param name="internalDrawFlags">Flags controlling window behavior.</param>
    /// <param name="persistence">Handler for window persistence data.</param>
    internal void DrawInternal(WindowDrawFlags internalDrawFlags, WindowSystemPersistence? persistence)
    {
        this.Window.PreOpenCheck();
        var doFades = !internalDrawFlags.HasFlag(WindowDrawFlags.IsReducedMotion) && !this.Window.DisableFadeInFadeOut;

        if (!this.Window.IsOpen)
        {
            if (this.Window.IsOpen != this.internalLastIsOpen)
            {
                this.internalLastIsOpen = this.Window.IsOpen;
                this.Window.OnClose();

                this.Window.IsFocused = false;

                if (internalDrawFlags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.Window.DisableWindowSounds)
                    UIGlobals.PlaySoundEffect(this.Window.OnCloseSfxId);
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
            return;

        var hasNamespace = !string.IsNullOrEmpty(this.Window.Namespace);

        if (hasNamespace)
            ImGui.PushID(this.Window.Namespace);

        this.PreHandlePreset(persistence);

        if (this.internalLastIsOpen != this.Window.IsOpen && this.Window.IsOpen)
        {
            this.internalLastIsOpen = this.Window.IsOpen;
            this.Window.OnOpen();

            if (internalDrawFlags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.Window.DisableWindowSounds)
                UIGlobals.PlaySoundEffect(this.Window.OnOpenSfxId);
        }

        // TODO: We may have to allow for windows to configure if they should fade
        if (this.internalAlpha.HasValue)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, this.internalAlpha.Value);
            this.didPushInternalAlpha = true;
        }

        this.Window.PreDraw();
        this.ApplyConditionals();

        if (this.Window.ForceMainWindow)
            ImGuiHelpers.ForceNextWindowMainViewport();

        var wasFocused = this.Window.IsFocused;
        if (wasFocused)
        {
            var style = ImGui.GetStyle();
            var focusedHeaderColor = style.Colors[(int)ImGuiCol.TitleBgActive];
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, focusedHeaderColor);
        }

        if (this.Window.RequestFocus)
        {
            ImGui.SetNextWindowFocus();
            this.Window.RequestFocus = false;
        }

        var flags = this.Window.Flags;

        if (this.Window.IsPinned || this.Window.IsClickthrough)
            flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;

        if (this.Window.IsClickthrough)
            flags |= ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoMouseInputs;

        var isWindowOpen = this.Window.IsOpen;

        if (this.CanShowCloseButton ? ImGui.Begin(this.Window.WindowName, ref isWindowOpen, flags) : ImGui.Begin(this.Window.WindowName, flags))
        {
            if (this.Window.IsOpen != isWindowOpen)
            {
                this.Window.IsOpen = isWindowOpen;
            }

            var context = ImGui.GetCurrentContext();
            if (!context.IsNull)
            {
                ImGuiP.GetCurrentWindow().InheritNoInputs = this.Window.IsClickthrough;
            }

            // Not supported yet on non-main viewports
            if ((this.Window.IsPinned || this.Window.IsClickthrough || this.internalAlpha.HasValue) &&
                ImGui.GetWindowViewport().ID != ImGui.GetMainViewport().ID)
            {
                this.internalAlpha = null;
                this.Window.IsPinned = false;
                this.Window.IsClickthrough = false;
                this.presetDirty = true;
            }

            // Draw the actual window contents
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

        const string additionsPopupName = "WindowSystemContextActions";
        var flagsApplicableForTitleBarIcons = !flags.HasFlag(ImGuiWindowFlags.NoDecoration) &&
                                              !flags.HasFlag(ImGuiWindowFlags.NoTitleBar);
        var showAdditions = (this.Window.AllowPinning || this.Window.AllowClickthrough) &&
                            internalDrawFlags.HasFlag(WindowDrawFlags.UseAdditionalOptions) &&
                            flagsApplicableForTitleBarIcons;
        var printWindow = false;
        if (showAdditions)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);

            if (ImGui.BeginPopup(additionsPopupName, ImGuiWindowFlags.NoMove))
            {
                var isAvailable = ImGuiHelpers.CheckIsWindowOnMainViewport();

                if (!isAvailable)
                    ImGui.BeginDisabled();

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
                }

                var alpha = (this.internalAlpha ?? ImGui.GetStyle().Alpha) * 100f;
                if (ImGui.SliderFloat(Loc.Localize("WindowSystemContextActionAlpha", "Opacity"), ref alpha, 20f,
                                      100f))
                {
                    this.internalAlpha = Math.Clamp(alpha / 100f, 0.2f, 1f);
                    this.presetDirty = true;
                }

                ImGui.SameLine();
                if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset")))
                {
                    this.internalAlpha = null;
                    this.presetDirty = true;
                }

                if (isAvailable)
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                                      Loc.Localize("WindowSystemContextActionClickthroughDisclaimer",
                                                   "Open this menu again by clicking the three dashes to disable clickthrough."));
                }
                else
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey,
                                      Loc.Localize("WindowSystemContextActionViewportDisclaimer",
                                                   "These features are only available if this window is inside the game window."));
                }

                if (!isAvailable)
                    ImGui.EndDisabled();

                if (ImGui.Button(Loc.Localize("WindowSystemContextActionPrintWindow", "Print window")))
                    printWindow = true;

                ImGui.EndPopup();
            }

            ImGui.PopStyleVar();
        }

        unsafe
        {
            var window = ImGuiP.GetCurrentWindow();

            ImRect outRect;
            ImGuiP.TitleBarRect(&outRect, window);

            var additionsButton = new TitleBarButton
            {
                Icon = FontAwesomeIcon.Bars,
                IconOffset = new Vector2(2.5f, 1),
                Click = _ =>
                {
                    this.Window.IsClickthrough = false;
                    this.presetDirty = false;
                    ImGui.OpenPopup(additionsPopupName);
                },
                Priority = int.MinValue,
                AvailableClickthrough = true,
            };

            if (flagsApplicableForTitleBarIcons)
            {
                this.DrawTitleBarButtons(window, flags, outRect,
                                         showAdditions
                                             ? this.Window.TitleBarButtons.Append(additionsButton)
                                             : this.Window.TitleBarButtons);
            }
        }

        if (wasFocused)
        {
            ImGui.PopStyleColor();
        }

        this.Window.IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        if (internalDrawFlags.HasFlag(WindowDrawFlags.UseFocusManagement) && !this.Window.IsPinned)
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
            Log.Verbose("Attempting to fade out {WindowName}", this.Window.WindowName);
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

        if (this.didPushInternalAlpha)
        {
            ImGui.PopStyleVar();
            this.didPushInternalAlpha = false;
        }

        this.Window.PostDraw();

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

        var maxBgAlpha = this.internalAlpha ?? this.Window.BgAlpha;
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
        this.internalAlpha = this.presetWindow.Alpha;
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
            this.presetWindow.Alpha = this.internalAlpha;

            var id = ImGui.GetID(this.Window.WindowName);
            persistence.SaveWindow(id, this.presetWindow!);
            this.presetDirty = false;

            Log.Verbose("Saved preset for {WindowName}", this.Window.WindowName);
        }
    }

    private unsafe void DrawTitleBarButtons(ImGuiWindowPtr window, ImGuiWindowFlags flags, ImRect titleBarRect, IEnumerable<TitleBarButton> buttons)
    {
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
            bool hovered, held;
            var pressed = false;

            if (this.Window.IsClickthrough)
            {
                hovered = false;
                held = false;

                // ButtonBehavior does not function if the window is clickthrough, so we have to do it ourselves
                if (ImGui.IsMouseHoveringRect(pos, max))
                {
                    hovered = true;

                    // We can't use ImGui native functions here, because they don't work with clickthrough
                    if ((global::Windows.Win32.PInvoke.GetKeyState((int)VirtualKey.LBUTTON) & 0x8000) != 0)
                    {
                        held = true;
                        pressed = true;
                    }
                }
            }
            else
            {
                pressed = ImGuiP.ButtonBehavior(bb, id, &hovered, &held, ImGuiButtonFlags.None);
            }

            if (isClipped)
                return pressed;

            // Render
            var bgCol = ImGui.GetColorU32((held && hovered) ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
            var textCol = ImGui.GetColorU32(ImGuiCol.Text);
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

        foreach (var button in buttons.OrderBy(x => x.Priority))
        {
            if (this.Window.IsClickthrough && !button.AvailableClickthrough)
                return;

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
        ImGui.TextColoredWrapped(ImGuiColors.DalamudRed,Loc.Localize("WindowSystemErrorOccurred", "An error occurred while rendering this window. Please contact the developer for details."));

        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.Button(Loc.Localize("WindowSystemErrorRecoverButton", "Attempt to retry")))
        {
            this.hasError = false;
            this.lastError = null;
        }

        ImGui.SameLine();

        if (ImGui.Button(Loc.Localize("WindowSystemErrorClose", "Close Window")))
        {
            this.Window.IsOpen = false;
            this.hasError = false;
            this.lastError = null;
        }

        ImGuiHelpers.ScaledDummy(10);

        if (this.lastError != null)
        {
            using var child = ImRaii.Child("##ErrorDetails", new Vector2(0, 200 * ImGuiHelpers.GlobalScale), true);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudGrey))
            {
                ImGui.TextWrapped(Loc.Localize("WindowSystemErrorDetails", "Error Details:"));
                ImGui.Separator();
                ImGui.TextWrapped(this.lastError.ToString());
            }

            var childWindowSize = ImGui.GetWindowSize();
            var copyText = Loc.Localize("WindowSystemErrorCopy", "Copy");
            var buttonWidth = ImGuiComponents.GetIconButtonWithTextWidth(FontAwesomeIcon.Copy, copyText);
            ImGui.SetCursorPos(new Vector2(childWindowSize.X - buttonWidth - ImGui.GetStyle().FramePadding.X,
                                           ImGui.GetStyle().FramePadding.Y));
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Copy, copyText))
            {
                ImGui.SetClipboardText(this.lastError.ToString());
            }
        }
    }
}
