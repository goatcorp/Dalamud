using System.Numerics;
using System.Runtime.Loader;

using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification.Internal.IconSource;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Represents an active notification.</summary>
internal sealed class ActiveNotification : IActiveNotification
{
    private readonly Notification underlyingNotification;

    private readonly Easing showEasing;
    private readonly Easing hideEasing;
    private readonly Easing progressEasing;
    private readonly Easing expandoEasing;

    /// <summary>The progress before for the progress bar animation with <see cref="progressEasing"/>.</summary>
    private float progressBefore;

    /// <summary>Used for calculating correct dismissal progressbar animation (left edge).</summary>
    private float prevProgressL;

    /// <summary>Used for calculating correct dismissal progressbar animation (right edge).</summary>
    private float prevProgressR;

    /// <summary>New progress value to be updated on next call to <see cref="UpdateAnimations"/>.</summary>
    private float? newProgress;

    /// <summary>New minimized value to be updated on next call to <see cref="UpdateAnimations"/>.</summary>
    private bool? newMinimized;

    /// <summary>Initializes a new instance of the <see cref="ActiveNotification"/> class.</summary>
    /// <param name="underlyingNotification">The underlying notification.</param>
    /// <param name="initiatorPlugin">The initiator plugin. Use <c>null</c> if originated by Dalamud.</param>
    public ActiveNotification(Notification underlyingNotification, LocalPlugin? initiatorPlugin)
    {
        this.underlyingNotification = underlyingNotification with
        {
            IconSource = underlyingNotification.IconSource?.Clone(),
        };
        this.InitiatorPlugin = initiatorPlugin;
        this.showEasing = new InCubic(NotificationConstants.ShowAnimationDuration);
        this.hideEasing = new OutCubic(NotificationConstants.HideAnimationDuration);
        this.progressEasing = new InOutCubic(NotificationConstants.ProgressChangeAnimationDuration);
        this.expandoEasing = new InOutCubic(NotificationConstants.ExpandoAnimationDuration);

        this.showEasing.Start();
        this.progressEasing.Start();
        try
        {
            this.UpdateIcon();
        }
        catch (Exception e)
        {
            // Ignore the one caused from ctor only; other UpdateIcon calls are from plugins, and they should handle the
            // error accordingly.
            Log.Error(e, $"{nameof(ActiveNotification)}#{this.Id} ctor: {nameof(this.UpdateIcon)} failed and ignored.");
        }
    }

    /// <inheritdoc/>
    public event NotificationDismissedDelegate? Dismiss;

    /// <inheritdoc/>
    public event Action<IActiveNotification>? Click;

    /// <inheritdoc/>
    public event Action<IActiveNotification>? DrawActions;

    /// <inheritdoc/>
    public event Action<IActiveNotification>? MouseEnter;

    /// <inheritdoc/>
    public event Action<IActiveNotification>? MouseLeave;

    /// <inheritdoc/>
    public long Id { get; } = IActiveNotification.CreateNewId();

    /// <summary>Gets the time of creating this notification.</summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>Gets the time of starting to count the timer for the expiration.</summary>
    public DateTime HoverRelativeToTime { get; private set; } = DateTime.Now;

    /// <summary>Gets the extended expiration time from <see cref="ExtendBy"/>.</summary>
    public DateTime ExtendedExpiry { get; private set; } = DateTime.Now;

    /// <inheritdoc/>
    public string Content
    {
        get => this.underlyingNotification.Content;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.Content = value;
        }
    }

    /// <inheritdoc/>
    public string? Title
    {
        get => this.underlyingNotification.Title;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.Title = value;
        }
    }

    /// <inheritdoc/>
    public string? MinimizedText
    {
        get => this.underlyingNotification.MinimizedText;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.MinimizedText = value;
        }
    }

    /// <inheritdoc/>
    public NotificationType Type
    {
        get => this.underlyingNotification.Type;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.Type = value;
        }
    }

    /// <inheritdoc/>
    public INotificationIconSource? IconSource
    {
        get => this.underlyingNotification.IconSource;
        set
        {
            if (this.IsDismissed)
            {
                value?.Dispose();
                return;
            }

            this.underlyingNotification.IconSource = value;
            this.UpdateIcon();
        }
    }

    /// <inheritdoc/>
    public DateTime HardExpiry
    {
        get => this.underlyingNotification.HardExpiry;
        set
        {
            if (this.underlyingNotification.HardExpiry == value || this.IsDismissed)
                return;
            this.underlyingNotification.HardExpiry = value;
            this.HoverRelativeToTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public TimeSpan InitialDuration
    {
        get => this.underlyingNotification.InitialDuration;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.InitialDuration = value;
            this.HoverRelativeToTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public TimeSpan HoverExtendDuration
    {
        get => this.underlyingNotification.HoverExtendDuration;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.HoverExtendDuration = value;
            this.HoverRelativeToTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public DateTime EffectiveExpiry
    {
        get
        {
            var initialDuration = this.InitialDuration;
            var expiryInitial =
                initialDuration == TimeSpan.MaxValue
                    ? DateTime.MaxValue
                    : this.CreatedAt + initialDuration;

            DateTime expiry;
            var hoverExtendDuration = this.HoverExtendDuration;
            if (hoverExtendDuration > TimeSpan.Zero && this.IsMouseHovered)
            {
                expiry = DateTime.MaxValue;
            }
            else
            {
                var expiryExtend =
                    hoverExtendDuration == TimeSpan.MaxValue
                        ? DateTime.MaxValue
                        : this.HoverRelativeToTime + hoverExtendDuration;

                expiry = expiryInitial > expiryExtend ? expiryInitial : expiryExtend;
                if (expiry < this.ExtendedExpiry)
                    expiry = this.ExtendedExpiry;
            }

            var he = this.HardExpiry;
            if (he < expiry)
                expiry = he;
            return expiry;
        }
    }

    /// <inheritdoc/>
    public bool ShowIndeterminateIfNoExpiry
    {
        get => this.underlyingNotification.ShowIndeterminateIfNoExpiry;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.ShowIndeterminateIfNoExpiry = value;
        }
    }

    /// <inheritdoc/>
    public bool Minimized
    {
        get => this.newMinimized ?? this.underlyingNotification.Minimized;
        set
        {
            if (this.IsDismissed)
                return;
            this.newMinimized = value;
        }
    }

    /// <inheritdoc/>
    public bool UserDismissable
    {
        get => this.underlyingNotification.UserDismissable;
        set
        {
            if (this.IsDismissed)
                return;
            this.underlyingNotification.UserDismissable = value;
        }
    }

    /// <inheritdoc/>
    public float Progress
    {
        get => this.newProgress ?? this.underlyingNotification.Progress;
        set
        {
            if (this.IsDismissed)
                return;
            this.newProgress = value;
        }
    }

    /// <inheritdoc/>
    public bool IsMouseHovered { get; private set; }

    /// <inheritdoc/>
    public bool IsDismissed => this.hideEasing.IsRunning;

    /// <summary>Gets a value indicating whether <see cref="InitiatorPlugin"/> has been unloaded.</summary>
    public bool IsInitiatorUnloaded { get; private set; }

    /// <summary>Gets or sets the plugin that initiated this notification.</summary>
    public LocalPlugin? InitiatorPlugin { get; set; }

    /// <summary>Gets or sets the icon of this notification.</summary>
    public INotificationMaterializedIcon? MaterializedIcon { get; set; }

    /// <summary>Gets the eased progress.</summary>
    private float ProgressEased
    {
        get
        {
            var underlyingProgress = this.underlyingNotification.Progress;
            if (Math.Abs(underlyingProgress - this.progressBefore) < 0.000001f || this.progressEasing.IsDone)
                return underlyingProgress;

            var state = Math.Clamp((float)this.progressEasing.Value, 0f, 1f);
            return this.progressBefore + (state * (underlyingProgress - this.progressBefore));
        }
    }

    /// <summary>Gets the default color of the notification.</summary>
    private Vector4 DefaultIconColor => this.Type switch
    {
        NotificationType.None => ImGuiColors.DalamudWhite,
        NotificationType.Success => ImGuiColors.HealerGreen,
        NotificationType.Warning => ImGuiColors.DalamudOrange,
        NotificationType.Error => ImGuiColors.DalamudRed,
        NotificationType.Info => ImGuiColors.TankBlue,
        _ => ImGuiColors.DalamudWhite,
    };

    /// <summary>Gets the default icon of the notification.</summary>
    private char? DefaultIconChar => this.Type switch
    {
        NotificationType.None => null,
        NotificationType.Success => FontAwesomeIcon.CheckCircle.ToIconChar(),
        NotificationType.Warning => FontAwesomeIcon.ExclamationCircle.ToIconChar(),
        NotificationType.Error => FontAwesomeIcon.TimesCircle.ToIconChar(),
        NotificationType.Info => FontAwesomeIcon.InfoCircle.ToIconChar(),
        _ => null,
    };

    /// <summary>Gets the default title of the notification.</summary>
    private string? DefaultTitle => this.Type switch
    {
        NotificationType.None => null,
        NotificationType.Success => NotificationType.Success.ToString(),
        NotificationType.Warning => NotificationType.Warning.ToString(),
        NotificationType.Error => NotificationType.Error.ToString(),
        NotificationType.Info => NotificationType.Info.ToString(),
        _ => null,
    };

    /// <summary>Gets the string for the initiator field.</summary>
    private string InitiatorString =>
        this.InitiatorPlugin is not { } initiatorPlugin
            ? NotificationConstants.DefaultInitiator
            : this.IsInitiatorUnloaded
                ? NotificationConstants.UnloadedInitiatorNameFormat.Format(initiatorPlugin.Name)
                : initiatorPlugin.Name;

    /// <summary>Gets the effective text to display when minimized.</summary>
    private string EffectiveMinimizedText => (this.MinimizedText ?? this.Content).ReplaceLineEndings(" ");

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ClearMaterializedIcon();
        this.underlyingNotification.Dispose();
        this.Dismiss = null;
        this.Click = null;
        this.DrawActions = null;
        this.InitiatorPlugin = null;
    }

    /// <inheritdoc/>
    public void DismissNow() => this.DismissNow(NotificationDismissReason.Programmatical);

    /// <summary>Dismisses this notification. Multiple calls will be ignored.</summary>
    /// <param name="reason">The reason of dismissal.</param>
    public void DismissNow(NotificationDismissReason reason)
    {
        if (this.hideEasing.IsRunning)
            return;

        this.hideEasing.Start();
        try
        {
            this.Dismiss?.Invoke(this, reason);
        }
        catch (Exception e)
        {
            Log.Error(
                e,
                $"{nameof(this.Dismiss)} error; notification is owned by {this.InitiatorPlugin?.Name ?? NotificationConstants.DefaultInitiator}");
        }
    }

    /// <summary>Updates animations.</summary>
    /// <returns><c>true</c> if the notification is over.</returns>
    public bool UpdateAnimations()
    {
        this.showEasing.Update();
        this.hideEasing.Update();
        this.progressEasing.Update();
        if (this.expandoEasing.IsRunning)
        {
            this.expandoEasing.Update();
            if (this.expandoEasing.IsDone)
                this.expandoEasing.Stop();
        }

        if (this.newProgress is { } newProgressValue)
        {
            if (Math.Abs(this.underlyingNotification.Progress - newProgressValue) > float.Epsilon)
            {
                this.progressBefore = this.ProgressEased;
                this.underlyingNotification.Progress = newProgressValue;
                this.progressEasing.Restart();
                this.progressEasing.Update();
            }

            this.newProgress = null;
        }

        if (this.newMinimized is { } newMinimizedValue)
        {
            if (this.underlyingNotification.Minimized != newMinimizedValue)
            {
                this.underlyingNotification.Minimized = newMinimizedValue;
                this.expandoEasing.Restart();
                this.expandoEasing.Update();
            }

            this.newMinimized = null;
        }

        return this.hideEasing.IsRunning && this.hideEasing.IsDone;
    }

    /// <summary>Draws this notification.</summary>
    /// <param name="maxWidth">The maximum width of the notification window.</param>
    /// <param name="offsetY">The offset from the bottom.</param>
    /// <returns>The height of the notification.</returns>
    public float Draw(float maxWidth, float offsetY)
    {
        var effectiveExpiry = this.EffectiveExpiry;
        if (!this.IsDismissed && DateTime.Now > effectiveExpiry)
            this.DismissNow(NotificationDismissReason.Timeout);

        var opacity =
            Math.Clamp(
                (float)(this.hideEasing.IsRunning
                            ? (this.hideEasing.IsDone ? 0 : 1f - this.hideEasing.Value)
                            : (this.showEasing.IsDone ? 1 : this.showEasing.Value)),
                0f,
                1f);
        if (opacity <= 0)
            return 0;

        var interfaceManager = Service<InterfaceManager>.Get();
        var unboundedWidth = ImGui.CalcTextSize(this.Content).X;
        float closeButtonHorizontalSpaceReservation;
        using (interfaceManager.IconFontHandle?.Push())
        {
            closeButtonHorizontalSpaceReservation = ImGui.CalcTextSize(FontAwesomeIcon.Times.ToIconString()).X;
            closeButtonHorizontalSpaceReservation += NotificationConstants.ScaledWindowPadding;
        }

        unboundedWidth = Math.Max(
            unboundedWidth,
            ImGui.CalcTextSize(this.Title ?? this.DefaultTitle ?? string.Empty).X);
        unboundedWidth = Math.Max(
            unboundedWidth,
            ImGui.CalcTextSize(this.InitiatorString).X);
        unboundedWidth = Math.Max(
            unboundedWidth,
            ImGui.CalcTextSize(this.CreatedAt.FormatAbsoluteDateTime()).X + closeButtonHorizontalSpaceReservation);
        unboundedWidth = Math.Max(
            unboundedWidth,
            ImGui.CalcTextSize(this.CreatedAt.FormatRelativeDateTime()).X + closeButtonHorizontalSpaceReservation);

        unboundedWidth += NotificationConstants.ScaledWindowPadding * 3;
        unboundedWidth += NotificationConstants.ScaledIconSize;

        var actionWindowHeight =
            // Content
            ImGui.GetTextLineHeight() +
            // Top and bottom padding
            (NotificationConstants.ScaledWindowPadding * 2);

        var width = Math.Min(maxWidth, unboundedWidth);

        var viewport = ImGuiHelpers.MainViewport;
        var viewportPos = viewport.WorkPos;
        var viewportSize = viewport.WorkSize;

        ImGui.PushID(this.Id.GetHashCode());
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(NotificationConstants.ScaledWindowPadding));
        unsafe
        {
            ImGui.PushStyleColor(
                ImGuiCol.WindowBg,
                *ImGui.GetStyleColorVec4(ImGuiCol.WindowBg) * new Vector4(
                    1f,
                    1f,
                    1f,
                    NotificationConstants.BackgroundOpacity));
        }

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(
            (viewportPos + viewportSize) -
            new Vector2(NotificationConstants.ScaledViewportEdgeMargin) -
            new Vector2(0, offsetY),
            ImGuiCond.Always,
            Vector2.One);
        ImGui.SetNextWindowSizeConstraints(
            new(width, actionWindowHeight),
            new(
                width,
                !this.underlyingNotification.Minimized || this.expandoEasing.IsRunning
                    ? float.MaxValue
                    : actionWindowHeight));
        ImGui.Begin(
            $"##NotifyMainWindow{this.Id}",
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoDecoration |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoFocusOnAppearing |
            ImGuiWindowFlags.NoDocking);

        this.DrawWindowBackgroundProgressBar();
        this.DrawTopBar(interfaceManager, width, actionWindowHeight);
        if (!this.underlyingNotification.Minimized && !this.expandoEasing.IsRunning)
        {
            this.DrawContentArea(width, actionWindowHeight);
        }
        else if (this.expandoEasing.IsRunning)
        {
            if (this.underlyingNotification.Minimized)
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity * (1f - (float)this.expandoEasing.Value));
            else
                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity * (float)this.expandoEasing.Value);
            this.DrawContentArea(width, actionWindowHeight);
            ImGui.PopStyleVar();
        }

        this.DrawExpiryBar(effectiveExpiry);

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var hovered = ImGui.IsWindowHovered();
        ImGui.End();

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(3);
        ImGui.PopID();

        if (windowPos.X <= ImGui.GetIO().MousePos.X
            && windowPos.Y <= ImGui.GetIO().MousePos.Y
            && ImGui.GetIO().MousePos.X < windowPos.X + windowSize.X
            && ImGui.GetIO().MousePos.Y < windowPos.Y + windowSize.Y)
        {
            if (!this.IsMouseHovered)
            {
                this.IsMouseHovered = true;
                this.MouseEnter.InvokeSafely(this);
            }

            if (this.HoverExtendDuration > TimeSpan.Zero)
                this.HoverRelativeToTime = DateTime.Now;

            if (hovered)
            {
                if (this.Click is null)
                {
                    if (this.UserDismissable && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        this.DismissNow(NotificationDismissReason.Manual);
                }
                else
                {
                    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
                        || ImGui.IsMouseClicked(ImGuiMouseButton.Right)
                        || ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
                        this.Click.InvokeSafely(this);
                }
            }
        }
        else if (this.IsMouseHovered)
        {
            this.IsMouseHovered = false;
            this.MouseLeave.InvokeSafely(this);
        }

        return windowSize.Y;
    }

    /// <inheritdoc/>
    public void ExtendBy(TimeSpan extension)
    {
        var newExpiry = DateTime.Now + extension;
        if (this.ExtendedExpiry < newExpiry)
            this.ExtendedExpiry = newExpiry;
    }

    /// <inheritdoc/>
    public void UpdateIcon()
    {
        if (this.IsDismissed)
            return;
        this.ClearMaterializedIcon();
        this.MaterializedIcon = (this.IconSource as INotificationIconSource.IInternal)?.Materialize();
    }

    /// <summary>Removes non-Dalamud invocation targets from events.</summary>
    public void RemoveNonDalamudInvocations()
    {
        var dalamudContext = AssemblyLoadContext.GetLoadContext(typeof(NotificationManager).Assembly);
        this.Dismiss = RemoveNonDalamudInvocationsCore(this.Dismiss);
        this.Click = RemoveNonDalamudInvocationsCore(this.Click);
        this.DrawActions = RemoveNonDalamudInvocationsCore(this.DrawActions);
        this.MouseEnter = RemoveNonDalamudInvocationsCore(this.MouseEnter);
        this.MouseLeave = RemoveNonDalamudInvocationsCore(this.MouseLeave);

        this.IsInitiatorUnloaded = true;
        this.UserDismissable = true;
        this.HoverExtendDuration = NotificationConstants.DefaultHoverExtendDuration;

        var newMaxExpiry = DateTime.Now + NotificationConstants.DefaultDisplayDuration;
        if (this.EffectiveExpiry > newMaxExpiry)
            this.HardExpiry = newMaxExpiry;

        return;

        T? RemoveNonDalamudInvocationsCore<T>(T? @delegate) where T : Delegate
        {
            if (@delegate is null)
                return null;

            foreach (var il in @delegate.GetInvocationList())
            {
                if (il.Target is { } target &&
                    AssemblyLoadContext.GetLoadContext(target.GetType().Assembly) != dalamudContext)
                {
                    @delegate = (T)Delegate.Remove(@delegate, il);
                }
            }

            return @delegate;
        }
    }

    private void ClearMaterializedIcon()
    {
        this.MaterializedIcon?.Dispose();
        this.MaterializedIcon = null;
    }

    private void DrawWindowBackgroundProgressBar()
    {
        var elapsed = (float)(((DateTime.Now - this.CreatedAt).TotalMilliseconds %
                               NotificationConstants.ProgressWaveLoopDuration) /
                              NotificationConstants.ProgressWaveLoopDuration);
        elapsed /= NotificationConstants.ProgressWaveIdleTimeRatio;

        var colorElapsed =
            elapsed < NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                ? elapsed / NotificationConstants.ProgressWaveLoopMaxColorTimeRatio
                : ((NotificationConstants.ProgressWaveLoopMaxColorTimeRatio * 2) - elapsed) /
                  NotificationConstants.ProgressWaveLoopMaxColorTimeRatio;

        elapsed = Math.Clamp(elapsed, 0f, 1f);
        colorElapsed = Math.Clamp(colorElapsed, 0f, 1f);
        colorElapsed = MathF.Sin(colorElapsed * (MathF.PI / 2f));

        var progress = Math.Clamp(this.ProgressEased, 0f, 1f);
        if (progress >= 1f)
            elapsed = colorElapsed = 0f;

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var rb = windowPos + windowSize;
        var midp = windowPos + windowSize with { X = windowSize.X * progress * elapsed };
        var rp = windowPos + windowSize with { X = windowSize.X * progress };

        ImGui.PushClipRect(windowPos, rb, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos,
            midp,
            ImGui.GetColorU32(
                Vector4.Lerp(
                    NotificationConstants.BackgroundProgressColorMin,
                    NotificationConstants.BackgroundProgressColorMax,
                    colorElapsed)));
        ImGui.GetWindowDrawList().AddRectFilled(
            midp with { Y = 0 },
            rp,
            ImGui.GetColorU32(NotificationConstants.BackgroundProgressColorMin));
        ImGui.PopClipRect();
    }

    private void DrawTopBar(InterfaceManager interfaceManager, float width, float height)
    {
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        var rtOffset = new Vector2(width, 0);
        using (interfaceManager.IconFontHandle?.Push())
        {
            ImGui.PushClipRect(windowPos, windowPos + windowSize with { Y = height }, false);
            if (this.UserDismissable)
            {
                if (this.DrawIconButton(FontAwesomeIcon.Times, rtOffset, height))
                    this.DismissNow(NotificationDismissReason.Manual);
                rtOffset.X -= height;
            }

            if (this.underlyingNotification.Minimized)
            {
                if (this.DrawIconButton(FontAwesomeIcon.ChevronDown, rtOffset, height))
                    this.Minimized = false;
            }
            else
            {
                if (this.DrawIconButton(FontAwesomeIcon.ChevronUp, rtOffset, height))
                    this.Minimized = true;
            }

            rtOffset.X -= height;
            ImGui.PopClipRect();
        }

        float relativeOpacity;
        if (this.expandoEasing.IsRunning)
        {
            relativeOpacity =
                this.underlyingNotification.Minimized
                    ? 1f - (float)this.expandoEasing.Value
                    : (float)this.expandoEasing.Value;
        }
        else
        {
            relativeOpacity = this.underlyingNotification.Minimized ? 0f : 1f;
        }

        if (this.IsMouseHovered)
            ImGui.PushClipRect(windowPos, windowPos + rtOffset with { Y = height }, false);
        else
            ImGui.PushClipRect(windowPos, windowPos + windowSize with { Y = height }, false);

        if (relativeOpacity > 0)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * relativeOpacity);
            ImGui.SetCursorPos(new(NotificationConstants.ScaledWindowPadding));
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.WhenTextColor);
            ImGui.TextUnformatted(
                this.IsMouseHovered
                    ? this.CreatedAt.FormatAbsoluteDateTime()
                    : this.CreatedAt.FormatRelativeDateTime());
            ImGui.PopStyleColor();
            ImGui.PopStyleVar();
        }

        if (relativeOpacity < 1)
        {
            rtOffset = new(width - NotificationConstants.ScaledWindowPadding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * (1f - relativeOpacity));

            var ltOffset = new Vector2(NotificationConstants.ScaledWindowPadding);
            this.DrawIcon(ltOffset, new(height - (2 * NotificationConstants.ScaledWindowPadding)));

            ltOffset.X = height;

            var agoText = this.CreatedAt.FormatRelativeDateTimeShort();
            var agoSize = ImGui.CalcTextSize(agoText);
            rtOffset.X -= agoSize.X;
            ImGui.SetCursorPos(rtOffset with { Y = NotificationConstants.ScaledWindowPadding });
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.WhenTextColor);
            ImGui.TextUnformatted(agoText);
            ImGui.PopStyleColor();

            rtOffset.X -= NotificationConstants.ScaledWindowPadding;

            ImGui.PushClipRect(
                windowPos + ltOffset with { Y = 0 },
                windowPos + rtOffset with { Y = height },
                true);
            ImGui.SetCursorPos(ltOffset with { Y = NotificationConstants.ScaledWindowPadding });
            ImGui.TextUnformatted(this.EffectiveMinimizedText);
            ImGui.PopClipRect();

            ImGui.PopStyleVar();
        }

        ImGui.PopClipRect();
    }

    private bool DrawIconButton(FontAwesomeIcon icon, Vector2 rt, float size)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        if (!this.IsMouseHovered)
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0f);
        ImGui.PushStyleColor(ImGuiCol.Button, 0);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.CloseTextColor);

        ImGui.SetCursorPos(rt - new Vector2(size, 0));
        var r = ImGui.Button(icon.ToIconString(), new(size));

        ImGui.PopStyleColor(2);
        if (!this.IsMouseHovered)
            ImGui.PopStyleVar();
        ImGui.PopStyleVar();
        return r;
    }

    private void DrawContentArea(float width, float actionWindowHeight)
    {
        var textColumnX = (NotificationConstants.ScaledWindowPadding * 2) + NotificationConstants.ScaledIconSize;
        var textColumnWidth = width - textColumnX - NotificationConstants.ScaledWindowPadding;
        var textColumnOffset = new Vector2(textColumnX, actionWindowHeight);

        this.DrawIcon(
            new(NotificationConstants.ScaledWindowPadding, actionWindowHeight),
            new(NotificationConstants.ScaledIconSize));

        textColumnOffset.Y += this.DrawTitle(textColumnOffset, textColumnWidth);
        textColumnOffset.Y += NotificationConstants.ScaledComponentGap;

        this.DrawContentBody(textColumnOffset, textColumnWidth);
    }

    private void DrawIcon(Vector2 minCoord, Vector2 size)
    {
        var maxCoord = minCoord + size;
        if (this.MaterializedIcon is not null)
        {
            this.MaterializedIcon.DrawIcon(minCoord, maxCoord, this.DefaultIconColor, this.InitiatorPlugin);
            return;
        }

        var defaultIconChar = this.DefaultIconChar;
        if (defaultIconChar is not null)
        {
            NotificationUtilities.DrawIconString(
                Service<NotificationManager>.Get().IconFontAwesomeFontHandle,
                defaultIconChar.Value,
                minCoord,
                maxCoord,
                this.DefaultIconColor);
            return;
        }

        TextureWrapTaskIconSource.DefaultMaterializedIcon.DrawIcon(
            minCoord,
            maxCoord,
            this.DefaultIconColor,
            this.InitiatorPlugin);
    }

    private float DrawTitle(Vector2 minCoord, float width)
    {
        ImGui.PushTextWrapPos(minCoord.X + width);

        ImGui.SetCursorPos(minCoord);
        if ((this.Title ?? this.DefaultTitle) is { } title)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.TitleTextColor);
            ImGui.TextUnformatted(title);
            ImGui.PopStyleColor();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BlameTextColor);
        ImGui.SetCursorPos(minCoord with { Y = ImGui.GetCursorPosY() });
        ImGui.TextUnformatted(this.InitiatorString);
        ImGui.PopStyleColor();

        ImGui.PopTextWrapPos();
        return ImGui.GetCursorPosY() - minCoord.Y;
    }

    private void DrawContentBody(Vector2 minCoord, float width)
    {
        ImGui.SetCursorPos(minCoord);
        ImGui.PushTextWrapPos(minCoord.X + width);
        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BodyTextColor);
        ImGui.TextUnformatted(this.Content);
        ImGui.PopStyleColor();
        ImGui.PopTextWrapPos();
        if (this.DrawActions is not null)
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + NotificationConstants.ScaledComponentGap);
            try
            {
                this.DrawActions.Invoke(this);
            }
            catch
            {
                // ignore
            }
        }
    }

    private void DrawExpiryBar(DateTime effectiveExpiry)
    {
        float barL, barR;
        if (this.IsDismissed)
        {
            var v = this.hideEasing.IsDone ? 0f : 1f - (float)this.hideEasing.Value;
            var midpoint = (this.prevProgressL + this.prevProgressR) / 2f;
            var length = (this.prevProgressR - this.prevProgressL) / 2f;
            barL = midpoint - (length * v);
            barR = midpoint + (length * v);
        }
        else if (this.HoverExtendDuration > TimeSpan.Zero && this.IsMouseHovered)
        {
            barL = 0f;
            barR = 1f;
            this.prevProgressL = barL;
            this.prevProgressR = barR;
        }
        else if (effectiveExpiry == DateTime.MaxValue)
        {
            if (this.ShowIndeterminateIfNoExpiry)
            {
                var elapsed = (float)(((DateTime.Now - this.CreatedAt).TotalMilliseconds %
                                       NotificationConstants.IndeterminateProgressbarLoopDuration) /
                                      NotificationConstants.IndeterminateProgressbarLoopDuration);
                barL = Math.Max(elapsed - (1f / 3), 0f) / (2f / 3);
                barR = Math.Min(elapsed, 2f / 3) / (2f / 3);
                barL = MathF.Pow(barL, 3);
                barR = 1f - MathF.Pow(1f - barR, 3);
                this.prevProgressL = barL;
                this.prevProgressR = barR;
            }
            else
            {
                this.prevProgressL = barL = 0f;
                this.prevProgressR = barR = 1f;
            }
        }
        else
        {
            barL = 1f - (float)((effectiveExpiry - DateTime.Now).TotalMilliseconds /
                                (effectiveExpiry - this.HoverRelativeToTime).TotalMilliseconds);
            barR = 1f;
            this.prevProgressL = barL;
            this.prevProgressR = barR;
        }

        barR = Math.Clamp(barR, 0f, 1f);

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        ImGui.PushClipRect(windowPos, windowPos + windowSize, false);
        ImGui.GetWindowDrawList().AddRectFilled(
            windowPos + new Vector2(
                windowSize.X * barL,
                windowSize.Y - NotificationConstants.ScaledExpiryProgressBarHeight),
            windowPos + windowSize with { X = windowSize.X * barR },
            ImGui.GetColorU32(this.DefaultIconColor));
        ImGui.PopClipRect();
    }
}
