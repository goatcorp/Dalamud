using System.Numerics;
using System.Runtime.Loader;
using System.Threading.Tasks;

using Dalamud.Game.Text;
using Dalamud.Interface.Animation;
using Dalamud.Interface.Animation.EasingFunctions;
using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Internal.Windows;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using ImGuiNET;

using Serilog;

namespace Dalamud.Interface.Internal.Notifications;

/// <summary>
/// Represents an active notification.
/// </summary>
internal sealed class ActiveNotification : IActiveNotification, IDisposable
{
    private readonly Easing showEasing;
    private readonly Easing hideEasing;

    private Notification underlyingNotification;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveNotification"/> class.
    /// </summary>
    /// <param name="underlyingNotification">The underlying notification.</param>
    /// <param name="initiatorPlugin">The initiator plugin. Use <c>null</c> if originated by Dalamud.</param>
    public ActiveNotification(Notification underlyingNotification, LocalPlugin? initiatorPlugin)
    {
        this.underlyingNotification = underlyingNotification with { };
        this.InitiatorPlugin = initiatorPlugin;
        this.showEasing = new InCubic(NotificationConstants.ShowAnimationDuration);
        this.hideEasing = new OutCubic(NotificationConstants.HideAnimationDuration);

        this.showEasing.Start();
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

    /// <summary>
    /// Gets the tick of creating this notification.
    /// </summary>
    public long CreatedAt { get; } = Environment.TickCount64;

    /// <inheritdoc/>
    public string Content => this.underlyingNotification.Content;

    /// <inheritdoc/>
    public string? Title => this.underlyingNotification.Title;

    /// <inheritdoc/>
    public NotificationType Type => this.underlyingNotification.Type;

    /// <inheritdoc/>
    public Func<Task<object>>? IconCreator => this.underlyingNotification.IconCreator;

    /// <inheritdoc/>
    public DateTime Expiry => this.underlyingNotification.Expiry;

    /// <inheritdoc/>
    public bool Interactible => this.underlyingNotification.Interactible;

    /// <inheritdoc/>
    public bool ClickIsDismiss => this.underlyingNotification.ClickIsDismiss;

    /// <inheritdoc/>
    public TimeSpan HoverExtendDuration => this.underlyingNotification.HoverExtendDuration;

    /// <inheritdoc/>
    public bool IsMouseHovered { get; private set; }

    /// <inheritdoc/>
    public bool IsDismissed => this.hideEasing.IsRunning;

    /// <summary>
    /// Gets or sets the plugin that initiated this notification.
    /// </summary>
    public LocalPlugin? InitiatorPlugin { get; set; }

    /// <summary>
    /// Gets or sets the icon of this notification.
    /// </summary>
    public Task<object>? IconTask { get; set; }

    /// <summary>
    /// Gets the default color of the notification.
    /// </summary>
    private Vector4 DefaultIconColor => this.Type switch
    {
        NotificationType.None => ImGuiColors.DalamudWhite,
        NotificationType.Success => ImGuiColors.HealerGreen,
        NotificationType.Warning => ImGuiColors.DalamudOrange,
        NotificationType.Error => ImGuiColors.DalamudRed,
        NotificationType.Info => ImGuiColors.TankBlue,
        _ => ImGuiColors.DalamudWhite,
    };

    /// <summary>
    /// Gets the default icon of the notification.
    /// </summary>
    private string? DefaultIconString => this.Type switch
    {
        NotificationType.None => null,
        NotificationType.Success => FontAwesomeIcon.CheckCircle.ToIconString(),
        NotificationType.Warning => FontAwesomeIcon.ExclamationCircle.ToIconString(),
        NotificationType.Error => FontAwesomeIcon.TimesCircle.ToIconString(),
        NotificationType.Info => FontAwesomeIcon.InfoCircle.ToIconString(),
        _ => null,
    };

    /// <summary>
    /// Gets the default title of the notification.
    /// </summary>
    private string? DefaultTitle => this.Type switch
    {
        NotificationType.None => null,
        NotificationType.Success => NotificationType.Success.ToString(),
        NotificationType.Warning => NotificationType.Warning.ToString(),
        NotificationType.Error => NotificationType.Error.ToString(),
        NotificationType.Info => NotificationType.Info.ToString(),
        _ => null,
    };

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ClearIconTask();
        this.underlyingNotification.IconCreator = null;
        this.Dismiss = null;
        this.Click = null;
        this.DrawActions = null;
        this.InitiatorPlugin = null;
    }

    /// <inheritdoc/>
    public Notification CloneNotification() => this.underlyingNotification with { };

    /// <inheritdoc/>
    public void DismissNow() => this.DismissNow(NotificationDismissReason.Programmatical);

    /// <summary>
    /// Dismisses this notification. Multiple calls will be ignored.
    /// </summary>
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

    /// <summary>
    /// Updates animations.
    /// </summary>
    /// <returns><c>true</c> if the notification is over.</returns>
    public bool UpdateAnimations()
    {
        this.showEasing.Update();
        this.hideEasing.Update();
        return this.hideEasing.IsRunning && this.hideEasing.IsDone;
    }

    /// <summary>
    /// Draws this notification.
    /// </summary>
    /// <param name="maxWidth">The maximum width of the notification window.</param>
    /// <param name="offsetY">The offset from the bottom.</param>
    /// <returns>The height of the notification.</returns>
    public float Draw(float maxWidth, float offsetY)
    {
        if (!this.IsDismissed
            && DateTime.Now > this.Expiry
            && (this.HoverExtendDuration <= TimeSpan.Zero || !this.IsMouseHovered))
        {
            this.DismissNow(NotificationDismissReason.Timeout);
        }

        var opacity =
            Math.Clamp(
                (float)(this.hideEasing.IsRunning
                            ? (this.hideEasing.IsDone ? 0 : 1f - this.hideEasing.Value)
                            : (this.showEasing.IsDone ? 1 : this.showEasing.Value)),
                0f,
                1f);
        if (opacity <= 0)
            return 0;

        var notificationManager = Service<NotificationManager>.Get();
        var interfaceManager = Service<InterfaceManager>.Get();
        var unboundedWidth = NotificationConstants.ScaledWindowPadding * 3;
        unboundedWidth += NotificationConstants.ScaledIconSize;
        unboundedWidth += Math.Max(
            Math.Max(
                ImGui.CalcTextSize(this.Title ?? this.DefaultTitle ?? string.Empty).X,
                ImGui.CalcTextSize(this.InitiatorPlugin?.Name ?? NotificationConstants.DefaultInitiator).X),
            ImGui.CalcTextSize(this.Content).X);

        var width = Math.Min(maxWidth, unboundedWidth);

        var viewport = ImGuiHelpers.MainViewport;
        var viewportPos = viewport.WorkPos;
        var viewportSize = viewport.WorkSize;

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(
            (viewportPos + viewportSize) -
            new Vector2(NotificationConstants.ScaledViewportEdgeMargin) -
            new Vector2(0, offsetY),
            ImGuiCond.Always,
            Vector2.One);
        ImGui.SetNextWindowSizeConstraints(new(width, 0), new(width, float.MaxValue));
        ImGui.PushID(this.Id.GetHashCode());
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(NotificationConstants.ScaledWindowPadding));
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
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

        ImGui.Begin(
            $"##NotifyWindow{this.Id}",
            ImGuiWindowFlags.AlwaysAutoResize |
            ImGuiWindowFlags.NoDecoration |
            (this.Interactible ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoInputs) |
            ImGuiWindowFlags.NoNav |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoFocusOnAppearing);

        var basePos = ImGui.GetCursorPos();
        this.DrawIcon(
            notificationManager,
            basePos,
            basePos + new Vector2(NotificationConstants.ScaledIconSize));
        basePos.X += NotificationConstants.ScaledIconSize + NotificationConstants.ScaledWindowPadding;
        width -= NotificationConstants.ScaledIconSize + (NotificationConstants.ScaledWindowPadding * 2);
        this.DrawTitle(basePos, basePos + new Vector2(width, 0));
        basePos.Y = ImGui.GetCursorPosY();
        this.DrawContentBody(basePos, basePos + new Vector2(width, 0));
        if (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            this.Click?.InvokeSafely(this);
            if (this.ClickIsDismiss)
                this.DismissNow(NotificationDismissReason.Manual);
        }

        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();

        ImGui.End();

        if (!this.IsDismissed)
            this.DrawCloseButton(interfaceManager, windowPos);

        ImGui.PopStyleColor();
        ImGui.PopStyleVar(2);
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
        }
        else if (this.IsMouseHovered)
        {
            if (this.HoverExtendDuration > TimeSpan.Zero)
            {
                var newExpiry = DateTime.Now + this.HoverExtendDuration;
                if (newExpiry > this.Expiry)
                    this.underlyingNotification.Expiry = newExpiry;
            }

            this.IsMouseHovered = false;
            this.MouseLeave.InvokeSafely(this);
        }

        return windowSize.Y;
    }

    /// <inheritdoc/>
    public void Update(INotification newNotification)
    {
        this.underlyingNotification.Content = newNotification.Content;
        this.underlyingNotification.Title = newNotification.Title;
        this.underlyingNotification.Type = newNotification.Type;
        this.underlyingNotification.IconCreator = newNotification.IconCreator;
        this.underlyingNotification.Expiry = newNotification.Expiry;
    }

    /// <inheritdoc/>
    public void UpdateIcon()
    {
        this.ClearIconTask();
        this.IconTask = this.IconCreator?.Invoke();
    }

    /// <summary>
    /// Removes non-Dalamud invocation targets from events.
    /// </summary>
    public void RemoveNonDalamudInvocations()
    {
        var dalamudContext = AssemblyLoadContext.GetLoadContext(typeof(NotificationManager).Assembly);
        this.Dismiss = RemoveNonDalamudInvocationsCore(this.Dismiss);
        this.Click = RemoveNonDalamudInvocationsCore(this.Click);
        this.DrawActions = RemoveNonDalamudInvocationsCore(this.DrawActions);
        this.MouseEnter = RemoveNonDalamudInvocationsCore(this.MouseEnter);
        this.MouseLeave = RemoveNonDalamudInvocationsCore(this.MouseLeave);

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

    private void ClearIconTask()
    {
        _ = this.IconTask?.ContinueWith(
            r =>
            {
                if (r.IsCompletedSuccessfully && r.Result is IDisposable d)
                    d.Dispose();
            });
        this.IconTask = null;
    }

    private void DrawIcon(NotificationManager notificationManager, Vector2 minCoord, Vector2 maxCoord)
    {
        string? iconString = null;
        IFontHandle? fontHandle = null;
        IDalamudTextureWrap? iconTexture = null;
        switch (this.IconTask?.IsCompletedSuccessfully is true ? this.IconTask.Result : null)
        {
            case IDalamudTextureWrap wrap:
                iconTexture = wrap;
                break;
            case SeIconChar icon:
                iconString = string.Empty + (char)icon;
                fontHandle = notificationManager.IconAxisFontHandle;
                break;
            case FontAwesomeIcon icon:
                iconString = icon.ToIconString();
                fontHandle = notificationManager.IconFontAwesomeFontHandle;
                break;
            default:
                iconString = this.DefaultIconString;
                fontHandle = notificationManager.IconFontAwesomeFontHandle;
                break;
        }

        if (string.IsNullOrWhiteSpace(iconString))
        {
            var dam = Service<DalamudAssetManager>.Get();
            if (this.InitiatorPlugin is null)
            {
                iconTexture = dam.GetDalamudTextureWrap(DalamudAsset.LogoSmall);
            }
            else
            {
                if (!Service<PluginImageCache>.Get().TryGetIcon(
                        this.InitiatorPlugin,
                        this.InitiatorPlugin.Manifest,
                        this.InitiatorPlugin.IsThirdParty,
                        out iconTexture) || iconTexture is null)
                {
                    iconTexture = this.InitiatorPlugin switch
                    {
                        { IsDev: true } => dam.GetDalamudTextureWrap(DalamudAsset.DevPluginIcon),
                        { IsThirdParty: true } => dam.GetDalamudTextureWrap(DalamudAsset.ThirdInstalledIcon),
                        _ => dam.GetDalamudTextureWrap(DalamudAsset.InstalledIcon),
                    };
                }
            }
        }

        if (iconTexture is not null)
        {
            var size = iconTexture.Size;
            if (size.X > maxCoord.X - minCoord.X)
                size *= (maxCoord.X - minCoord.X) / size.X;
            if (size.Y > maxCoord.Y - minCoord.Y)
                size *= (maxCoord.Y - minCoord.Y) / size.Y;
            var pos = ((minCoord + maxCoord) - size) / 2;
            ImGui.SetCursorPos(pos);
            ImGui.Image(iconTexture.ImGuiHandle, size);
        }
        else if (fontHandle is not null)
        {
            using (fontHandle.Push())
            {
                var size = ImGui.CalcTextSize(iconString);
                var pos = ((minCoord + maxCoord) - size) / 2;
                ImGui.SetCursorPos(pos);
                ImGui.PushStyleColor(ImGuiCol.Text, this.DefaultIconColor);
                ImGui.TextUnformatted(iconString);
                ImGui.PopStyleColor();
            }
        }
    }

    private void DrawTitle(Vector2 minCoord, Vector2 maxCoord)
    {
        ImGui.PushTextWrapPos(maxCoord.X);

        ImGui.SetCursorPos(minCoord);
        if ((this.Title ?? this.DefaultTitle) is { } title)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.TitleTextColor);
            ImGui.TextUnformatted(title);
            ImGui.PopStyleColor();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.BlameTextColor);
        ImGui.SetCursorPos(minCoord with { Y = ImGui.GetCursorPosY() });
        ImGui.TextUnformatted(this.InitiatorPlugin?.Name ?? NotificationConstants.DefaultInitiator);
        ImGui.PopStyleColor();

        ImGui.PopTextWrapPos();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + NotificationConstants.ScaledComponentGap);
    }

    private void DrawCloseButton(InterfaceManager interfaceManager, Vector2 screenCoord)
    {
        using (interfaceManager.IconFontHandle?.Push())
        {
            var str = FontAwesomeIcon.Times.ToIconString();
            var size = NotificationConstants.ScaledCloseButtonMinSize;
            var textSize = ImGui.CalcTextSize(str);
            size = Math.Max(size, Math.Max(textSize.X, textSize.Y));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleColor(ImGuiCol.Button, 0);
            ImGui.PushStyleColor(ImGuiCol.Text, NotificationConstants.CloseTextColor);

            // ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowPos(screenCoord, ImGuiCond.Always, new(1, 0));
            ImGui.SetNextWindowSizeConstraints(new(size), new(size));
            ImGui.Begin(
                $"##CloseButtonWindow{this.Id}",
                ImGuiWindowFlags.AlwaysAutoResize |
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoNav |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoFocusOnAppearing);

            if (ImGui.Button(str, new(size)))
                this.DismissNow();

            ImGui.End();
            ImGui.PopStyleColor(2);
            ImGui.PopStyleVar(4);
        }
    }

    private void DrawContentBody(Vector2 minCoord, Vector2 maxCoord)
    {
        ImGui.SetCursorPos(minCoord);
        ImGui.PushTextWrapPos(maxCoord.X);
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
}
