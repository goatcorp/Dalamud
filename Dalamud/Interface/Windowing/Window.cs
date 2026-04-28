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

using static TerraFX.Interop.Windows.Windows;

namespace Dalamud.Interface.Windowing;

/// <summary>
/// Base class you can use to implement an ImGui window for use with the built-in <see cref="WindowSystem"/>.
/// </summary>
public abstract class Window
{
    private const float FadeInOutTime = 0.072f;
    private const string AdditionsPopupName = "WindowSystemContextActions";
    private const float BlurNoiseOpacity = 0.17f;
    private const float MaxBlurStrength = 14f;
    private static readonly Vector4 BlurTintMultiplier = new(158 / 255f, 158 / 255f, 158 / 255f, 25 / 255f);

    private static readonly ModuleLog Log = ModuleLog.Create<WindowSystem>();

    private static bool wasEscPressedLastFrame = false;

    private readonly TitleBarButton additionsButton;
    private readonly List<TitleBarButton> allButtons = [];

    private bool internalLastIsOpen = false;
    private bool internalIsOpen = false;
    private bool internalIsPinned = false;
    private bool internalIsClickthrough = false;
    private bool didPushInternalAlpha = false;
    private float? internalAlpha = null;
    private float? internalBlurFactorOverride = null;
    private bool nextFrameBringToFront = false;

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
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="name">The name/ID of this window.
    /// If you have multiple windows with the same name, you will need to
    /// append a unique ID to it by specifying it after "###" behind the window title.
    /// </param>
    /// <param name="flags">The <see cref="ImGuiWindowFlags"/> of this window.</param>
    /// <param name="forceMainWindow">Whether this window should be limited to the main game window.</param>
    protected Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false)
    {
        this.WindowName = name;
        this.Flags = flags;
        this.ForceMainWindow = forceMainWindow;

        this.additionsButton = new()
        {
            Icon = FontAwesomeIcon.Bars,
            IconOffset = new Vector2(2.5f, 1),
            Click = _ =>
            {
                this.internalIsClickthrough = false;
                this.presetDirty = true;
                ImGui.OpenPopup(AdditionsPopupName);
            },
            Priority = int.MinValue,
            AvailableClickthrough = true,
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    /// <param name="name">The name/ID of this window.
    /// If you have multiple windows with the same name, you will need to
    /// append a unique ID to it by specifying it after "###" behind the window title.
    /// </param>
    protected Window(string name)
        : this(name, ImGuiWindowFlags.None)
    {
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
    /// Gets or sets the namespace of the window.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the name of the window.
    /// If you have multiple windows with the same name, you will need to
    /// append an unique ID to it by specifying it after "###" behind the window title.
    /// </summary>
    public string WindowName { get; set; }

    /// <summary>
    /// Gets a value indicating whether the window is focused.
    /// </summary>
    public bool IsFocused { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window is to be closed with a hotkey, like Escape, and keep game addons open in turn if it is closed.
    /// </summary>
    public bool RespectCloseHotkey { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this window should not generate sound effects when opening and closing.
    /// </summary>
    public bool DisableWindowSounds { get; set; } = false;

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is opened.
    /// </summary>
    public uint OnOpenSfxId { get; set; } = 23u;

    /// <summary>
    /// Gets or sets a value representing the sound effect id to be played when the window is closed.
    /// </summary>
    public uint OnCloseSfxId { get; set; } = 24u;

    /// <summary>
    /// Gets or sets a value indicating whether this window should not fade in and out, regardless of the users'
    /// preference.
    /// </summary>
    public bool DisableFadeInFadeOut { get; set; } = false;

    /// <summary>
    /// Gets or sets the position of this window.
    /// </summary>
    public Vector2? Position { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the position of this window is set.
    /// </summary>
    public ImGuiCond PositionCondition { get; set; }

    /// <summary>
    /// Gets or sets the size of the window. The size provided will be scaled by the global scale.
    /// </summary>
    public Vector2? Size { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the size of this window is set.
    /// </summary>
    public ImGuiCond SizeCondition { get; set; }

    /// <summary>
    /// Gets or sets the size constraints of the window. The size constraints provided will be scaled by the global scale.
    /// </summary>
    public WindowSizeConstraints? SizeConstraints { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this window is collapsed.
    /// </summary>
    public bool? Collapsed { get; set; }

    /// <summary>
    /// Gets or sets the condition that defines when the collapsed state of this window is set.
    /// </summary>
    public ImGuiCond CollapsedCondition { get; set; }

    /// <summary>
    /// Gets or sets the window flags.
    /// </summary>
    public ImGuiWindowFlags Flags { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ImGui window will be forced to stay inside the main game window.
    /// </summary>
    public bool ForceMainWindow { get; set; }

    /// <summary>
    /// Gets or sets this window's background alpha value.
    /// </summary>
    public float? BgAlpha { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this ImGui window should display a close button in the title bar.
    /// </summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this window should offer to be pinned via the window's titlebar context menu.
    /// </summary>
    public bool AllowPinning { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this window should offer to be made click-through via the window's titlebar context menu.
    /// </summary>
    public bool AllowClickthrough { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this window should apply a blur effect to the background behind it when drawn, if the user has enabled this feature globally.
    /// </summary>
    public bool AllowBackgroundBlur
    {
        get;
        set
        {
            field = value;
            if (!field)
            {
                this.internalBlurFactorOverride = null;
                this.presetDirty = true;
            }
        }
#pragma warning disable SA1500
#pragma warning disable SA1513
    } = true;
#pragma warning restore SA1513
#pragma warning restore SA1500

    /// <summary>
    /// Gets a value indicating whether this window is pinned.
    /// </summary>
    public bool IsPinned => this.internalIsPinned && this.AllowPinning;

    /// <summary>
    /// Gets a value indicating whether this window is click-through.
    /// </summary>
    public bool IsClickthrough => this.internalIsClickthrough && this.AllowClickthrough;

    /// <summary>
    /// Gets or sets a list of available title bar buttons.
    ///
    /// If <see cref="AllowPinning"/> or <see cref="AllowClickthrough"/> are set to true, and this features is not
    /// disabled globally by the user, an internal title bar button to manage these is added when drawing, but it will
    /// not appear in this collection. If you wish to remove this button, set both of these values to false.
    /// </summary>
    public List<TitleBarButton> TitleBarButtons { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this window will stay open.
    /// </summary>
    public bool IsOpen
    {
        get => this.internalIsOpen;
        set => this.internalIsOpen = value;
    }

    private bool CanShowCloseButton => this.ShowCloseButton && !this.IsClickthrough;

    /// <summary>
    /// Toggle window is open state.
    /// </summary>
    public void Toggle()
    {
        this.IsOpen ^= true;
    }

    /// <summary>
    /// Bring this window to the front.
    /// </summary>
    public void BringToFront()
    {
        if (!this.IsOpen)
            return;

        this.nextFrameBringToFront = true;
    }

    /// <summary>
    /// Code to always be executed before the open-state of the window is checked.
    /// </summary>
    public virtual void PreOpenCheck()
    {
    }

    /// <summary>
    /// Additional conditions for the window to be drawn, regardless of its open-state.
    /// </summary>
    /// <returns>
    /// True if the window should be drawn, false otherwise.
    /// </returns>
    /// <remarks>
    /// Not being drawn due to failing this condition will not change focus or trigger OnClose.
    /// This is checked before PreDraw, but after Update.
    /// </remarks>
    public virtual bool DrawConditions()
    {
        return true;
    }

    /// <summary>
    /// Code to be executed before conditionals are applied and the window is drawn.
    /// </summary>
    public virtual void PreDraw()
    {
        if (this.internalAlpha.HasValue)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, this.internalAlpha.Value);
            this.didPushInternalAlpha = true;
        }
    }

    /// <summary>
    /// Code to be executed after the window is drawn.
    /// </summary>
    public virtual void PostDraw()
    {
        if (this.didPushInternalAlpha)
        {
            ImGui.PopStyleVar();
            this.didPushInternalAlpha = false;
        }
    }

    /// <summary>
    /// Code to be executed every time the window renders.
    /// </summary>
    /// <remarks>
    /// In this method, implement your drawing code.
    /// You do NOT need to ImGui.Begin your window.
    /// </remarks>
    public abstract void Draw();

    /// <summary>
    /// Code to be executed when the window is opened.
    /// </summary>
    public virtual void OnOpen()
    {
    }

    /// <summary>
    /// Code to be executed when the window is closed.
    /// </summary>
    public virtual void OnClose()
    {
    }

    /// <summary>
    /// Code to be executed when the window is safe to be disposed or removed from the window system.
    /// Doing so in <see cref="OnClose"/> may result in animations not playing correctly.
    /// </summary>
    public virtual void OnSafeToRemove()
    {
    }

    /// <summary>
    /// Code to be executed every frame, even when the window is collapsed.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Draw the window via ImGui.
    /// </summary>
    /// <param name="internalDrawParams">Parameters controlling window behavior.</param>
    /// <param name="persistence">Handler for window persistence data.</param>
    internal void DrawInternal(WindowDrawParameters internalDrawParams, WindowSystemPersistence? persistence)
    {
        this.PreOpenCheck();
        var doFades = !internalDrawParams.Flags.HasFlag(WindowDrawFlags.IsReducedMotion) && !this.DisableFadeInFadeOut;

        if (!this.IsOpen)
        {
            if (this.internalIsOpen != this.internalLastIsOpen)
            {
                this.internalLastIsOpen = this.internalIsOpen;
                this.OnClose();

                this.IsFocused = false;

                if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.DisableWindowSounds)
                {
                    unsafe
                    {
                        UIGlobals.PlaySoundEffect(this.OnCloseSfxId);
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
                    this.OnSafeToRemove();
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

        this.Update();
        if (!this.DrawConditions())
            return;

        var hasNamespace = !string.IsNullOrEmpty(this.Namespace);

        if (hasNamespace)
            ImGui.PushID(this.Namespace);

        this.PreHandlePreset(persistence);

        if (this.internalLastIsOpen != this.internalIsOpen && this.internalIsOpen)
        {
            this.internalLastIsOpen = this.internalIsOpen;
            this.OnOpen();

            if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseSoundEffects) && !this.DisableWindowSounds)
            {
                unsafe
                {
                    UIGlobals.PlaySoundEffect(this.OnOpenSfxId);
                }
            }
        }

        var isErrorStylePushed = false;
        if (!this.hasError)
        {
            this.PreDraw();
            this.ApplyConditionals();
        }
        else
        {
            Style.StyleModelV1.DalamudStandard.Push();
            isErrorStylePushed = true;
        }

        if (this.ForceMainWindow)
            ImGuiHelpers.ForceNextWindowMainViewport();

        var wasFocused = this.IsFocused;
        if (wasFocused && this is not StyleEditorWindow)
        {
            var style = ImGui.GetStyle();
            var focusedHeaderColor = style.Colors[(int)ImGuiCol.TitleBgActive];
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, focusedHeaderColor);
        }

        if (this.nextFrameBringToFront)
        {
            ImGui.SetNextWindowFocus();
            this.nextFrameBringToFront = false;
        }

        var flags = this.Flags;

        if (this.IsPinned || this.IsClickthrough)
        {
            flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        }

        if (this.IsClickthrough)
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

            effectiveWindowBgAlpha *= this.internalAlpha ?? 1f;
        }

        var windowHasBackground = effectiveWindowBgAlpha != 0f;

        if (this.CanShowCloseButton ? ImGui.Begin(this.WindowName, ref this.internalIsOpen, flags) : ImGui.Begin(this.WindowName, flags))
        {
            // Apply background blur
            {
                var effectiveBlurFactor = this.internalBlurFactorOverride ?? internalDrawParams.DefaultBackgroundBlurStrength;
                var shouldBlur = this.AllowBackgroundBlur &&
                                 effectiveBlurFactor != 0f &&
                                 ImGui.GetWindowViewport().ID == ImGui.GetMainViewport().ID &&
                                 windowHasBackground;

                // TODO: Fade between active/inactive tint?
                if (shouldBlur)
                {
                    var wPos = ImGui.GetWindowPos();
                    ImGuiHelpers.PrependBlurBehind(
                        ImGui.GetWindowDrawList(),
                        wPos,
                        wPos + ImGui.GetWindowSize(),
                        effectiveBlurFactor * MaxBlurStrength,
                        ImGui.GetStyle().WindowRounding,
                        tintColor: ImGui.GetStyle().Colors[ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) ? (int)ImGuiCol.TitleBgActive : (int)ImGuiCol.TitleBg] * BlurTintMultiplier,
                        noiseOpacity: BlurNoiseOpacity * effectiveWindowBgAlpha);
                }
            }

            var context = ImGui.GetCurrentContext();
            if (!context.IsNull)
            {
                ImGuiP.GetCurrentWindow().InheritNoInputs = this.IsClickthrough;
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
                    this.Draw();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error during Draw(): {WindowName}", this.WindowName);

                    this.hasError = true;
                    this.lastError = ex;
                }
            }
        }

        var flagsApplicableForTitleBarIcons = !flags.HasFlag(ImGuiWindowFlags.NoDecoration) &&
                                              !flags.HasFlag(ImGuiWindowFlags.NoTitleBar);
        var showAdditions = (this.AllowPinning || this.AllowClickthrough || this.AllowBackgroundBlur) &&
                            internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseAdditionalOptions) &&
                            flagsApplicableForTitleBarIcons;
        var printWindow = false;
        if (showAdditions)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 1f);

            if (ImGui.BeginPopup(AdditionsPopupName, ImGuiWindowFlags.NoMove))
            {
                if (this.IsClickthrough)
                    ImGui.BeginDisabled();

                if (this.AllowPinning)
                {
                    var showAsPinned = this.IsPinned || this.IsClickthrough;
                    if (ImGui.Checkbox(Loc.Localize("WindowSystemContextActionPin", "Pin Window"), ref showAsPinned))
                    {
                        this.internalIsPinned = showAsPinned;
                        this.presetDirty = true;
                    }

                    ImGuiComponents.HelpMarker(
                        Loc.Localize("WindowSystemContextActionPinHint", "Pinned windows will not move or resize when you click and drag them, nor will they close when escape is pressed."));
                }

                if (this.IsClickthrough)
                    ImGui.EndDisabled();

                if (this.AllowClickthrough)
                {
                    if (ImGui.Checkbox(
                            Loc.Localize("WindowSystemContextActionClickthrough", "Make clickthrough"),
                            ref this.internalIsClickthrough))
                    {
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
                if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset") + "##resetAlpha"))
                {
                    this.internalAlpha = null;
                    this.presetDirty = true;
                }

                if (this.AllowBackgroundBlur)
                {
                    var blurOverride =
                        (this.internalBlurFactorOverride ?? internalDrawParams.DefaultBackgroundBlurStrength) * 100f;
                    if (ImGui.SliderFloat(Loc.Localize("WindowSystemContextActionBlur", "Background Blur"), ref blurOverride, 0f, 100f, "%.1f%%"))
                    {
                        this.internalBlurFactorOverride = blurOverride / 100f;
                        this.presetDirty = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.Button(Loc.Localize("WindowSystemContextActionReset", "Reset") + "##resetBlur"))
                    {
                        this.internalBlurFactorOverride = null;
                        this.presetDirty = true;
                    }
                }

                ImGui.TextColored(
                    ImGuiColors.DalamudGrey,
                    Loc.Localize(
                        "WindowSystemContextActionClickthroughDisclaimer",
                        "Open this menu again by clicking the three dashes to disable clickthrough."));

                if (ImGui.Button(Loc.Localize("WindowSystemContextActionPrintWindow", "Print window")))
                    printWindow = true;

                ImGui.EndPopup();
            }

            ImGui.PopStyleVar();
        }

        if (flagsApplicableForTitleBarIcons)
        {
            this.allButtons.Clear();
            this.allButtons.EnsureCapacity(this.TitleBarButtons.Count + 1);
            this.allButtons.AddRange(this.TitleBarButtons);
            if (showAdditions)
                this.allButtons.Add(this.additionsButton);
            this.allButtons.Sort(static (a, b) => b.Priority - a.Priority);
            this.DrawTitleBarButtons();
        }

        if (wasFocused && this is not StyleEditorWindow)
        {
            ImGui.PopStyleColor();
        }

        this.IsFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

        if (internalDrawParams.Flags.HasFlag(WindowDrawFlags.UseFocusManagement) && !this.IsPinned)
        {
            var escapeDown = Service<KeyState>.Get()[VirtualKey.ESCAPE];
            if (escapeDown && this.IsFocused && !wasEscPressedLastFrame && this.RespectCloseHotkey)
            {
                this.IsOpen = false;
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
        if (!this.internalIsOpen && this.fadeOutTexture == null && doFades && !isCollapsed && !isDocked)
        {
            this.fadeOutTexture = Service<TextureManager>.Get().CreateDrawListTexture(
                "WindowFadeOutTexture");
            this.fadeOutTexture.ResizeAndDrawWindow(this.WindowName, Vector2.One);
            this.fadeOutTimer = FadeInOutTime;
        }

        if (printWindow)
        {
            var tex = Service<TextureManager>.Get().CreateDrawListTexture(
                Loc.Localize("WindowSystemContextActionPrintWindow", "Print window"));
            tex.ResizeAndDrawWindow(this.WindowName, Vector2.One);
            _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                this.WindowName,
                this.WindowName,
                Task.FromResult<IDalamudTextureWrap>(tex));
        }

        if (isErrorStylePushed)
        {
            Style.StyleModelV1.DalamudStandard.Pop();
        }
        else
        {
            this.PostDraw();
        }

        this.PostHandlePreset(persistence);

        if (hasNamespace)
            ImGui.PopID();
    }

    private unsafe void ApplyConditionals()
    {
        if (this.Position.HasValue)
        {
            var pos = this.Position.Value;

            if (this.ForceMainWindow)
                pos += ImGuiHelpers.MainViewport.Pos;

            ImGui.SetNextWindowPos(pos, this.PositionCondition);
        }

        if (this.Size.HasValue)
        {
            ImGui.SetNextWindowSize(this.Size.Value * ImGuiHelpers.GlobalScale, this.SizeCondition);
        }

        if (this.Collapsed.HasValue)
        {
            ImGui.SetNextWindowCollapsed(this.Collapsed.Value, this.CollapsedCondition);
        }

        if (this.SizeConstraints.HasValue)
        {
            ImGui.SetNextWindowSizeConstraints(this.SizeConstraints.Value.MinimumSize * ImGuiHelpers.GlobalScale, this.SizeConstraints.Value.MaximumSize * ImGuiHelpers.GlobalScale);
        }

        var maxBgAlpha = this.internalAlpha ?? this.BgAlpha;
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

    private void PreHandlePreset(WindowSystemPersistence? persistence)
    {
        if (persistence == null || this.hasInitializedFromPreset)
            return;

        var id = ImGui.GetID(this.WindowName);
        this.presetWindow = persistence.GetWindow(id);

        this.hasInitializedFromPreset = true;

        // Fresh preset - don't apply anything
        if (this.presetWindow == null)
        {
            this.presetWindow = new PresetModel.PresetWindow();
            this.presetDirty = true;
            return;
        }

        this.internalIsPinned = this.presetWindow.IsPinned;
        this.internalIsClickthrough = this.presetWindow.IsClickThrough;
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
            this.presetWindow.IsPinned = this.internalIsPinned;
            this.presetWindow.IsClickThrough = this.internalIsClickthrough;
            this.presetWindow.Alpha = this.internalAlpha;
            this.presetWindow.BlurFactorOverride = this.internalBlurFactorOverride;

            var id = ImGui.GetID(this.WindowName);
            persistence.SaveWindow(id, this.presetWindow!);
            this.presetDirty = false;

            Log.Verbose("Saved preset for {WindowName}", this.WindowName);
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

            if (this.IsClickthrough)
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
                pressed = held = hovered && (GetKeyState(VK.VK_LBUTTON) & 0x8000) != 0;
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
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left) && !this.IsClickthrough)
                ImGuiP.StartMouseMovingWindow(window);

            return pressed;
        }

        foreach (var button in this.allButtons)
        {
            if (this.IsClickthrough && !button.AvailableClickthrough)
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
        if (ImGui.Begin(this.WindowName, flags))
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
        ImGui.TextColoredWrapped(ImGuiColors.ErrorForeground, Loc.Localize("WindowSystemErrorOccurred", "An error occurred while rendering this window. Please contact the developer for details."));

        ImGuiHelpers.ScaledDummy(5);

        if (ImGui.Button(Loc.Localize("WindowSystemErrorRecoverButton", "Attempt to retry")))
        {
            this.hasError = false;
            this.lastError = null;
        }

        ImGui.SameLine();

        if (ImGui.Button(Loc.Localize("WindowSystemErrorClose", "Close Window")))
        {
            this.IsOpen = false;
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

    /// <summary>
    /// Structure detailing the size constraints of a window.
    /// </summary>
    public struct WindowSizeConstraints
    {
        private Vector2 internalMaxSize = new(float.MaxValue);

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSizeConstraints"/> struct.
        /// </summary>
        public WindowSizeConstraints()
        {
        }

        /// <summary>
        /// Gets or sets the minimum size of the window.
        /// </summary>
        public Vector2 MinimumSize { get; set; } = new(0);

        /// <summary>
        /// Gets or sets the maximum size of the window.
        /// </summary>
        public Vector2 MaximumSize
        {
            get => this.GetSafeMaxSize();
            set => this.internalMaxSize = value;
        }

        private Vector2 GetSafeMaxSize()
        {
            var currentMin = this.MinimumSize;

            if (this.internalMaxSize.X < currentMin.X || this.internalMaxSize.Y < currentMin.Y)
                return new Vector2(float.MaxValue);

            return this.internalMaxSize;
        }
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
        /// Gets the sigma value to be used for background blur, if enabled..
        /// </summary>
        public float DefaultBackgroundBlurStrength { get; init; }
    }

    /// <summary>
    /// Structure describing a title bar button.
    /// </summary>
    public class TitleBarButton
    {
        /// <summary>
        /// Gets or sets the icon of the button.
        /// </summary>
        public FontAwesomeIcon Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color. If null, the default text color is used.
        /// </summary>
        public Vector4? IconColor { get; set; }

        /// <summary>
        /// Gets or sets a vector by which the position of the icon within the button shall be offset.
        /// Automatically scaled by the global font scale for you.
        /// </summary>
        public Vector2 IconOffset { get; set; }

        /// <summary>
        /// Gets or sets an action that is called when a tooltip shall be drawn.
        /// May be null if no tooltip shall be drawn.
        /// </summary>
        public Action? ShowTooltip { get; set; }

        /// <summary>
        /// Gets or sets an action that is called when the button is clicked.
        /// </summary>
        public Action<ImGuiMouseButton>? Click { get; set; }

        /// <summary>
        /// Gets or sets the priority the button shall be shown in.
        /// Lower = closer to ImGui default buttons.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the button shall be clickable
        /// when the respective window is set to clickthrough.
        /// </summary>
        public bool AvailableClickthrough { get; set; }
    }
}
