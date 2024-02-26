using System.Collections.Concurrent;
using System.Collections.Generic;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.IoC.Internal;
using Dalamud.Plugin.Internal.Types;
using Dalamud.Plugin.Services;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Class handling notifications/toasts in ImGui.</summary>
[InterfaceVersion("1.0")]
[ServiceManager.EarlyLoadedService]
internal class NotificationManager : INotificationManager, IServiceType, IDisposable
{
    private readonly List<ActiveNotification> notifications = new();
    private readonly ConcurrentBag<ActiveNotification> pendingNotifications = new();

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

    private IFontAtlas PrivateAtlas { get; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.PrivateAtlas.Dispose();
        foreach (var n in this.pendingNotifications)
            n.Dispose();
        foreach (var n in this.notifications)
            n.Dispose();
        this.pendingNotifications.Clear();
        this.notifications.Clear();
    }

    /// <inheritdoc/>
    public IActiveNotification AddNotification(Notification notification, bool disposeNotification = true)
    {
        using var disposer = disposeNotification ? notification : null;
        var an = new ActiveNotification(notification, null);
        this.pendingNotifications.Add(an);
        return an;
    }

    /// <summary>Adds a notification originating from a plugin.</summary>
    /// <param name="notification">The notification.</param>
    /// <param name="disposeNotification">Dispose <paramref name="notification"/> when this function returns.</param>
    /// <param name="plugin">The source plugin.</param>
    /// <returns>The added notification.</returns>
    /// <remarks><paramref name="disposeNotification"/> will be honored even on exceptions.</remarks>
    public IActiveNotification AddNotification(Notification notification, bool disposeNotification, LocalPlugin plugin)
    {
        using var disposer = disposeNotification ? notification : null;
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
            },
            true);

    /// <summary>Draw all currently queued notifications.</summary>
    public void Draw()
    {
        var viewportSize = ImGuiHelpers.MainViewport.WorkSize;
        var height = 0f;

        while (this.pendingNotifications.TryTake(out var newNotification))
            this.notifications.Add(newNotification);

        var maxWidth = Math.Max(320 * ImGuiHelpers.GlobalScale, viewportSize.X / 3);

        this.notifications.RemoveAll(
            static x =>
            {
                if (!x.UpdateAnimations())
                    return false;

                x.Dispose();
                return true;
            });
        foreach (var tn in this.notifications)
            height += tn.Draw(maxWidth, height) + NotificationConstants.ScaledWindowGap;
    }
}

/// <summary>Plugin-scoped version of a <see cref="NotificationManager"/> service.</summary>
[PluginInterface]
[InterfaceVersion("1.0")]
[ServiceManager.ScopedService]
#pragma warning disable SA1015
[ResolveVia<INotificationManager>]
#pragma warning restore SA1015
internal class NotificationManagerPluginScoped : INotificationManager, IServiceType, IDisposable
{
    private readonly LocalPlugin localPlugin;
    private readonly ConcurrentDictionary<IActiveNotification, int> notifications = new();

    [ServiceManager.ServiceDependency]
    private readonly NotificationManager notificationManagerService = Service<NotificationManager>.Get();

    [ServiceManager.ServiceConstructor]
    private NotificationManagerPluginScoped(LocalPlugin localPlugin) =>
        this.localPlugin = localPlugin;

    /// <inheritdoc/>
    public IActiveNotification AddNotification(Notification notification, bool disposeNotification = true)
    {
        var an = this.notificationManagerService.AddNotification(notification, disposeNotification, this.localPlugin);
        _ = this.notifications.TryAdd(an, 0);
        an.Dismiss += (a, unused) => this.notifications.TryRemove(an, out _);
        return an;
    }

    /// <inheritdoc/>
    public void Dispose()
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
