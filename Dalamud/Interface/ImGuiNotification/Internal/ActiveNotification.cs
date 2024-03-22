using System.Runtime.Loader;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Represents an active notification.</summary>
internal sealed partial class ActiveNotification : IActiveNotification
{
    private readonly Notification underlyingNotification;

    private readonly Easing showEasing;
    private readonly Easing hideEasing;
    private readonly Easing progressEasing;
    private readonly Easing expandoEasing;

    /// <summary>Whether to call <see cref="IDisposable.Dispose"/> on <see cref="DisposeInternal"/>.</summary>
    private bool hasIconTextureOwnership;

    /// <summary>Gets the time of starting to count the timer for the expiration.</summary>
    private DateTime lastInterestTime;

    /// <summary>Gets the extended expiration time from <see cref="ExtendBy"/>.</summary>
    private DateTime extendedExpiry;

    /// <summary>The plugin that initiated this notification.</summary>
    private LocalPlugin? initiatorPlugin;

    /// <summary>Whether <see cref="initiatorPlugin"/> has been unloaded.</summary>
    private bool isInitiatorUnloaded;

    /// <summary>The progress before for the progress bar animation with <see cref="progressEasing"/>.</summary>
    private float progressBefore;

    /// <summary>Used for calculating correct dismissal progressbar animation (left edge).</summary>
    private float prevProgressL;

    /// <summary>Used for calculating correct dismissal progressbar animation (right edge).</summary>
    private float prevProgressR;

    /// <summary>New progress value to be updated on next call to <see cref="UpdateOrDisposeInternal"/>.</summary>
    private float? newProgress;

    /// <summary>New minimized value to be updated on next call to <see cref="UpdateOrDisposeInternal"/>.</summary>
    private bool? newMinimized;

    /// <summary>Initializes a new instance of the <see cref="ActiveNotification"/> class.</summary>
    /// <param name="underlyingNotification">The underlying notification.</param>
    /// <param name="initiatorPlugin">The initiator plugin. Use <c>null</c> if originated by Dalamud.</param>
    public ActiveNotification(Notification underlyingNotification, LocalPlugin? initiatorPlugin)
    {
        this.underlyingNotification = underlyingNotification with { };
        this.initiatorPlugin = initiatorPlugin;
        this.showEasing = new InCubic(NotificationConstants.ShowAnimationDuration);
        this.hideEasing = new OutCubic(NotificationConstants.HideAnimationDuration);
        this.progressEasing = new InOutCubic(NotificationConstants.ProgressChangeAnimationDuration);
        this.expandoEasing = new InOutCubic(NotificationConstants.ExpandoAnimationDuration);
        this.CreatedAt = this.lastInterestTime = this.extendedExpiry = DateTime.Now;

        this.showEasing.Start();
        this.progressEasing.Start();
    }

    /// <inheritdoc/>
    public long Id { get; } = IActiveNotification.CreateNewId();

    /// <inheritdoc/>
    public DateTime CreatedAt { get; }

    /// <inheritdoc/>
    public string Content
    {
        get => this.underlyingNotification.Content;
        set => this.underlyingNotification.Content = value;
    }

    /// <inheritdoc/>
    public string? Title
    {
        get => this.underlyingNotification.Title;
        set => this.underlyingNotification.Title = value;
    }

    /// <inheritdoc/>
    public bool RespectUiHidden
    {
        get => this.underlyingNotification.RespectUiHidden;
        set => this.underlyingNotification.RespectUiHidden = value;
    }

    /// <inheritdoc/>
    public string? MinimizedText
    {
        get => this.underlyingNotification.MinimizedText;
        set => this.underlyingNotification.MinimizedText = value;
    }

    /// <inheritdoc/>
    public NotificationType Type
    {
        get => this.underlyingNotification.Type;
        set => this.underlyingNotification.Type = value;
    }

    /// <inheritdoc/>
    public INotificationIcon? Icon
    {
        get => this.underlyingNotification.Icon;
        set => this.underlyingNotification.Icon = value;
    }

    /// <inheritdoc/>
    public IDalamudTextureWrap? IconTexture
    {
        get => this.underlyingNotification.IconTexture;
        set => this.IconTextureTask = value is null ? null : Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task<IDalamudTextureWrap?>? IconTextureTask
    {
        get => this.underlyingNotification.IconTextureTask;
        set
        {
            // Do nothing if the value did not change.
            if (this.underlyingNotification.IconTextureTask == value)
                return;

            if (this.hasIconTextureOwnership)
            {
                _ = this.underlyingNotification.IconTextureTask?.ToContentDisposedTask(true);
                this.underlyingNotification.IconTextureTask = null;
                this.hasIconTextureOwnership = false;
            }

            this.underlyingNotification.IconTextureTask = value;
        }
    }

    /// <inheritdoc/>
    public DateTime HardExpiry
    {
        get => this.underlyingNotification.HardExpiry;
        set
        {
            if (this.underlyingNotification.HardExpiry == value)
                return;
            this.underlyingNotification.HardExpiry = value;
            this.lastInterestTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public TimeSpan InitialDuration
    {
        get => this.underlyingNotification.InitialDuration;
        set
        {
            this.underlyingNotification.InitialDuration = value;
            this.lastInterestTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public TimeSpan ExtensionDurationSinceLastInterest
    {
        get => this.underlyingNotification.ExtensionDurationSinceLastInterest;
        set
        {
            this.underlyingNotification.ExtensionDurationSinceLastInterest = value;
            this.lastInterestTime = DateTime.Now;
        }
    }

    /// <inheritdoc/>
    public DateTime EffectiveExpiry { get; private set; }

    /// <inheritdoc/>
    public NotificationDismissReason? DismissReason { get; private set; }

    /// <inheritdoc/>
    public bool ShowIndeterminateIfNoExpiry
    {
        get => this.underlyingNotification.ShowIndeterminateIfNoExpiry;
        set => this.underlyingNotification.ShowIndeterminateIfNoExpiry = value;
    }

    /// <inheritdoc/>
    public bool Minimized
    {
        get => this.newMinimized ?? this.underlyingNotification.Minimized;
        set => this.newMinimized = value;
    }

    /// <inheritdoc/>
    public bool UserDismissable
    {
        get => this.underlyingNotification.UserDismissable;
        set => this.underlyingNotification.UserDismissable = value;
    }

    /// <inheritdoc/>
    public float Progress
    {
        get => this.newProgress ?? this.underlyingNotification.Progress;
        set => this.newProgress = value;
    }

    private static bool ReducedMotions => Service<DalamudConfiguration>.Get().ReduceMotions ?? false;

    /// <summary>Gets the eased progress.</summary>
    private float ProgressEased
    {
        get
        {
            var underlyingProgress = this.underlyingNotification.Progress;
            if (Math.Abs(underlyingProgress - this.progressBefore) < 0.000001f || this.progressEasing.IsDone || ReducedMotions)
                return underlyingProgress;

            var state = ReducedMotions ? 1f : Math.Clamp((float)this.progressEasing.Value, 0f, 1f);
            return this.progressBefore + (state * (underlyingProgress - this.progressBefore));
        }
    }

    /// <summary>Gets the string for the initiator field.</summary>
    private string InitiatorString =>
        this.initiatorPlugin is not { } plugin
            ? NotificationConstants.DefaultInitiator
            : this.isInitiatorUnloaded
                ? NotificationConstants.UnloadedInitiatorNameFormat.Format(plugin.Name)
                : plugin.Name;

    /// <summary>Gets the effective text to display when minimized.</summary>
    private string EffectiveMinimizedText => (this.MinimizedText ?? this.Content).ReplaceLineEndings(" ");

    /// <inheritdoc/>
    public void DismissNow() => this.DismissNow(NotificationDismissReason.Programmatical);

    /// <summary>Dismisses this notification. Multiple calls will be ignored.</summary>
    /// <param name="reason">The reason of dismissal.</param>
    public void DismissNow(NotificationDismissReason reason)
    {
        if (this.DismissReason is not null)
            return;

        this.DismissReason = reason;
        this.hideEasing.Start();
        this.InvokeDismiss();
    }

    /// <inheritdoc/>
    public void ExtendBy(TimeSpan extension)
    {
        var newExpiry = DateTime.Now + extension;
        if (this.extendedExpiry < newExpiry)
            this.extendedExpiry = newExpiry;
    }

    /// <inheritdoc/>
    public void SetIconTexture(IDalamudTextureWrap? textureWrap) =>
        this.SetIconTexture(textureWrap, false);

    /// <inheritdoc/>
    public void SetIconTexture(IDalamudTextureWrap? textureWrap, bool leaveOpen) =>
        this.SetIconTexture(textureWrap is null ? null : Task.FromResult(textureWrap), leaveOpen);

    /// <inheritdoc/>
    public void SetIconTexture(Task<IDalamudTextureWrap?>? textureWrapTask) =>
        this.SetIconTexture(textureWrapTask, false);

    /// <inheritdoc/>
    public void SetIconTexture(Task<IDalamudTextureWrap?>? textureWrapTask, bool leaveOpen)
    {
        // If we're requested to replace the texture with the same texture, do nothing.
        if (this.underlyingNotification.IconTextureTask == textureWrapTask)
            return;

        if (this.DismissReason is not null)
        {
            if (!leaveOpen)
                textureWrapTask?.ToContentDisposedTask(true);
            return;
        }

        if (this.hasIconTextureOwnership)
            _ = this.underlyingNotification.IconTextureTask?.ToContentDisposedTask(true);

        this.hasIconTextureOwnership = !leaveOpen;
        this.underlyingNotification.IconTextureTask = textureWrapTask;
    }

    /// <summary>Removes non-Dalamud invocation targets from events.</summary>
    /// <remarks>
    /// This is done to prevent references of plugins being unloaded from outliving the plugin itself.
    /// Anything that can contain plugin-provided types and functions count, which effectively means that events and
    /// interface/object-typed fields need to be scrubbed.
    /// As a notification can be marked as non-user-dismissable, in which case after removing event handlers there will
    /// be no way to remove the notification, we force the notification to become user-dismissable, and reset the expiry
    /// to the default duration on unload.
    /// </remarks>
    internal void RemoveNonDalamudInvocations()
    {
        var dalamudContext = AssemblyLoadContext.GetLoadContext(typeof(NotificationManager).Assembly);
        this.Dismiss = RemoveNonDalamudInvocationsCore(this.Dismiss);
        this.Click = RemoveNonDalamudInvocationsCore(this.Click);
        this.DrawActions = RemoveNonDalamudInvocationsCore(this.DrawActions);

        if (this.Icon is { } previousIcon && !IsOwnedByDalamud(previousIcon.GetType()))
            this.Icon = null;

        // Clear the texture if we don't have the ownership.
        // The texture probably was owned by the plugin being unloaded in such case.
        if (!this.hasIconTextureOwnership)
            this.IconTextureTask = null;

        this.isInitiatorUnloaded = true;
        this.UserDismissable = true;
        this.ExtensionDurationSinceLastInterest = NotificationConstants.DefaultDuration;

        var newMaxExpiry = DateTime.Now + NotificationConstants.DefaultDuration;
        if (this.EffectiveExpiry > newMaxExpiry)
            this.HardExpiry = newMaxExpiry;

        return;

        bool IsOwnedByDalamud(Type t) => AssemblyLoadContext.GetLoadContext(t.Assembly) == dalamudContext;

        T? RemoveNonDalamudInvocationsCore<T>(T? @delegate) where T : Delegate
        {
            if (@delegate is null)
                return null;

            foreach (var il in @delegate.GetInvocationList())
            {
                if (il.Target is { } target && !IsOwnedByDalamud(target.GetType()))
                    @delegate = (T)Delegate.Remove(@delegate, il);
            }

            return @delegate;
        }
    }

    /// <summary>Updates the state of this notification, and release the relevant resource if this notification is no
    /// longer in use.</summary>
    /// <returns><c>true</c> if the notification is over and relevant resources are released.</returns>
    /// <remarks>Intended to be called from the main thread only.</remarks>
    internal bool UpdateOrDisposeInternal()
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

        if (!this.hideEasing.IsRunning || !this.hideEasing.IsDone)
            return false;

        this.DisposeInternal();
        return true;
    }

    /// <summary>Clears the resources associated with this instance of <see cref="ActiveNotification"/>.</summary>
    internal void DisposeInternal()
    {
        if (this.hasIconTextureOwnership)
        {
            _ = this.underlyingNotification.IconTextureTask?.ToContentDisposedTask(true);
            this.underlyingNotification.IconTextureTask = null;
            this.hasIconTextureOwnership = false;
        }

        this.Dismiss = null;
        this.Click = null;
        this.DrawActions = null;
        this.initiatorPlugin = null;
    }

    private void LogEventInvokeError(Exception exception, string message) =>
        Log.Error(
            exception,
            $"[{nameof(ActiveNotification)}:{this.initiatorPlugin?.Name ?? NotificationConstants.DefaultInitiator}] {message}");
}
