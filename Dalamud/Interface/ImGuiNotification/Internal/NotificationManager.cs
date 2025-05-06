using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

using ImGuiNET;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Class handling notifications/toasts in ImGui.</summary>
[ServiceManager.EarlyLoadedService]
internal class NotificationManager : INotificationManager, IInternalDisposableService
{
    [ServiceManager.ServiceDependency]
    private readonly GameGui gameGui = Service<GameGui>.Get();

    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration configuration = Service<DalamudConfiguration>.Get();

    private readonly List<ActiveNotification> notifications = new();
    private readonly ConcurrentBag<ActiveNotification> pendingNotifications = new();

    private NotificationPositionChooser? positionChooser;

    [ServiceManager.ServiceConstructor]
    private NotificationManager(FontAtlasFactory fontAtlasFactory)
    {
        this.PrivateAtlas = fontAtlasFactory.CreateFontAtlas(
            nameof(NotificationManager),
            FontAtlasAutoRebuildMode.Async);
        this.IconAxisFontHandle =
            this.PrivateAtlas.NewGameFontHandle(new(GameFontFamily.Axis, NotificationConstants.IconSize));
        this.IconFontAwesomeFontHandle =
            this.PrivateAtlas.NewDelegateFontHandle(
                e => e.OnPreBuild(
                    tk => tk.AddFontAwesomeIconFont(new() { SizePx = NotificationConstants.IconSize })));
    }

    /// <summary>Gets the handle to AXIS fonts, sized for use as an icon.</summary>
    public IFontHandle IconAxisFontHandle { get; }

    /// <summary>Gets the handle to FontAwesome fonts, sized for use as an icon.</summary>
    public IFontHandle IconFontAwesomeFontHandle { get; }

    /// <summary>Gets the private atlas for use with notification windows.</summary>
    private IFontAtlas PrivateAtlas { get; }

    /// <summary>
    /// Calculate the width to be used to draw notifications.
    /// </summary>
    /// <returns>The width.</returns>
    public static float CalculateNotificationWidth()
    {
        var viewportSize = ImGuiHelpers.MainViewport.WorkSize;
        var width = ImGui.CalcTextSize(NotificationConstants.NotificationWidthMeasurementString).X;
        width += NotificationConstants.ScaledWindowPadding * 3;
        width += NotificationConstants.ScaledIconSize;
        return Math.Min(width, viewportSize.X * NotificationConstants.MaxNotificationWindowWidthWrtMainViewportWidth);
    }

    /// <summary>
    /// Check if notifications should scroll downwards on the screen, based on the anchor position.
    /// </summary>
    /// <param name="anchorPosition">Where notifications are anchored to.</param>
    /// <returns>A value indicating wether notifications should scroll downwards.</returns>
    public static bool ShouldScrollDownwards(Vector2 anchorPosition)
    {
        return anchorPosition.Y < 0.5f;
    }

    /// <summary>
    /// Choose the snap position for a notification based on the anchor position.
    /// </summary>
    /// <param name="anchorPosition">Where notifications are anchored to.</param>
    /// <returns>The snap position.</returns>
    public static NotificationSnapDirection ChooseSnapDirection(Vector2 anchorPosition)
    {
        if (anchorPosition.Y <= NotificationConstants.NotificationTopBottomSnapMargin)
            return NotificationSnapDirection.Top;

        if (anchorPosition.Y >= 1f - NotificationConstants.NotificationTopBottomSnapMargin)
            return NotificationSnapDirection.Bottom;

        if (anchorPosition.X <= 0.5f)
            return NotificationSnapDirection.Left;

        return NotificationSnapDirection.Right;
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        this.PrivateAtlas.Dispose();
        foreach (var n in this.pendingNotifications)
            n.DisposeInternal();
        foreach (var n in this.notifications)
            n.DisposeInternal();
        this.pendingNotifications.Clear();
        this.notifications.Clear();
    }

    /// <inheritdoc/>
    public IActiveNotification AddNotification(Notification notification)
    {
        var an = new ActiveNotification(notification, null);
        this.pendingNotifications.Add(an);
        return an;
    }

    /// <summary>Adds a notification originating from a plugin.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="plugin">The source plugin.</param>
    /// <returns>The added notification.</returns>
    public IActiveNotification AddNotification(Notification notification, LocalPlugin plugin)
    {
        var an = new ActiveNotification(notification, plugin);
        this.pendingNotifications.Add(an);
        return an;
    }

    /// <summary>Add a notification to the notification queue.</summary>
    /// <param name="content">The content of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="type">The type of the notification.</param>
    public void AddNotification(
        string content,
        string? title = null,
        NotificationType type = NotificationType.None) =>
        this.AddNotification(
            new()
            {
                Content = content,
                Title = title,
                Type = type,
            });

    /// <summary>Draw all currently queued notifications.</summary>
    public void Draw()
    {
        var height = 0f;
        var uiHidden = this.gameGui.GameUiHidden;

        while (this.pendingNotifications.TryTake(out var newNotification))
            this.notifications.Add(newNotification);

        var width = CalculateNotificationWidth();

        this.notifications.RemoveAll(static x => x.UpdateOrDisposeInternal());

        var scrollsDownwards = ShouldScrollDownwards(this.configuration.NotificationAnchorPosition);
        var snapDirection = ChooseSnapDirection(this.configuration.NotificationAnchorPosition);

        foreach (var tn in this.notifications)
        {
            if (uiHidden && tn.RespectUiHidden)
                continue;

            height += tn.Draw(width, height, this.configuration.NotificationAnchorPosition, snapDirection);
            height += scrollsDownwards ? -NotificationConstants.ScaledWindowGap : NotificationConstants.ScaledWindowGap;
        }

        this.positionChooser?.Draw();
    }

    /// <summary>
    /// Starts the position chooser for notifications. Will block the UI until the user makes a selection.
    /// </summary>
    public void StartPositionChooser()
    {
        this.positionChooser = new NotificationPositionChooser(this.configuration);
        this.positionChooser.SelectionMade += () => { this.positionChooser = null; };
    }
}

/// <summary>Plugin-scoped version of a <see cref="NotificationManager"/> service.</summary>
[PluginInterface]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<INotificationManager>]
#pragma warning restore SA1015
internal class NotificationManagerPluginScoped : INotificationManager, IInternalDisposableService
{
    private readonly LocalPlugin localPlugin;
    private readonly ConcurrentDictionary<IActiveNotification, int> notifications = new();

    [ServiceManager.ServiceDependency]
    private readonly NotificationManager notificationManagerService = Service<NotificationManager>.Get();

    [ServiceManager.ServiceConstructor]
    private NotificationManagerPluginScoped(LocalPlugin localPlugin) =>
        this.localPlugin = localPlugin;

    /// <inheritdoc/>
    public IActiveNotification AddNotification(Notification notification)
    {
        var an = this.notificationManagerService.AddNotification(notification, this.localPlugin);
        _ = this.notifications.TryAdd(an, 0);
        an.Dismiss += a => this.notifications.TryRemove(a.Notification, out _);
        return an;
    }

    /// <inheritdoc/>
    public void DisposeService()
    {
        while (!this.notifications.IsEmpty)
        {
            foreach (var n in this.notifications.Keys)
            {
                this.notifications.TryRemove(n, out _);
                ((ActiveNotification)n).RemoveNonDalamudInvocations();
            }
        }
    }
}
