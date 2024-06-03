using System.Numerics;

using Dalamud.Interface.ImGuiNotification.EventArgs;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Represents an active notification.</summary>
internal sealed partial class ActiveNotification : INotificationDismissArgs
{
    /// <inheritdoc/>
    public event Action<INotificationDismissArgs>? Dismiss;

    /// <inheritdoc/>
    IActiveNotification INotificationDismissArgs.Notification => this;

    /// <inheritdoc/>
    NotificationDismissReason INotificationDismissArgs.Reason =>
        this.DismissReason
        ?? throw new InvalidOperationException("DismissReason must be set before using INotificationDismissArgs");

    private void InvokeDismiss()
    {
        try
        {
            this.Dismiss?.Invoke(this);
        }
        catch (Exception e)
        {
            this.LogEventInvokeError(e, $"{nameof(this.Dismiss)} error");
        }
    }
}

/// <summary>Represents an active notification.</summary>
internal sealed partial class ActiveNotification : INotificationClickArgs
{
    /// <inheritdoc/>
    public event Action<INotificationClickArgs>? Click;

    /// <inheritdoc/>
    IActiveNotification INotificationClickArgs.Notification => this;

    private void InvokeClick()
    {
        try
        {
            this.Click?.Invoke(this);
        }
        catch (Exception e)
        {
            this.LogEventInvokeError(e, $"{nameof(this.Click)} error");
        }
    }
}

/// <summary>Represents an active notification.</summary>
internal sealed partial class ActiveNotification : INotificationDrawArgs
{
    private Vector2 drawActionArgMinCoord;
    private Vector2 drawActionArgMaxCoord;

    /// <inheritdoc/>
    public event Action<INotificationDrawArgs>? DrawActions;

    /// <inheritdoc/>
    IActiveNotification INotificationDrawArgs.Notification => this;

    /// <inheritdoc/>
    Vector2 INotificationDrawArgs.MinCoord => this.drawActionArgMinCoord;

    /// <inheritdoc/>
    Vector2 INotificationDrawArgs.MaxCoord => this.drawActionArgMaxCoord;

    private void InvokeDrawActions(Vector2 minCoord, Vector2 maxCoord)
    {
        this.drawActionArgMinCoord = minCoord;
        this.drawActionArgMaxCoord = maxCoord;
        try
        {
            this.DrawActions?.Invoke(this);
        }
        catch (Exception e)
        {
            this.LogEventInvokeError(e, $"{nameof(this.DrawActions)} error; event registration cancelled");
        }
    }
}
