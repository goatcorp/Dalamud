namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Possible notification types.</summary>
public enum NotificationType
{
    /// <summary>No special type.</summary>
    None,

    /// <summary>Type indicating success.</summary>
    Success,

    /// <summary>Type indicating a warning.</summary>
    Warning,

    /// <summary>Type indicating an error.</summary>
    Error,

    /// <summary>Type indicating generic information.</summary>
    Info,
}
