using System.Threading.Tasks;

using Dalamud.Interface.ImGuiNotification.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Serilog;

namespace Dalamud.Interface.ImGuiNotification;
/// <summary>Represents a blueprint for a notification.</summary>
public sealed record Notification : INotification
{
    /// <summary>
    /// Gets the default value for <see cref="InitialDuration"/> and <see cref="ExtensionDurationSinceLastInterest"/>.
    /// </summary>
    public static TimeSpan DefaultDuration => NotificationConstants.DefaultDuration;

    /// <inheritdoc/>
    public string Content { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string? Title { get; set; }

    /// <inheritdoc/>
    public string? MinimizedText { get; set; }

    /// <inheritdoc/>
    public NotificationType Type { get; set; } = NotificationType.None;

    /// <inheritdoc/>
    public INotificationIcon? Icon { get; set; }

    /// <inheritdoc/>
    public ISharedImmediateTexture? IconTexture { get; set; }

    /// <inheritdoc/>
    public DateTime HardExpiry { get; set; } = DateTime.MaxValue;

    /// <inheritdoc/>
    public TimeSpan InitialDuration { get; set; } = DefaultDuration;

    /// <inheritdoc/>
    public TimeSpan ExtensionDurationSinceLastInterest { get; set; } = DefaultDuration;

    /// <inheritdoc/>
    public bool ShowIndeterminateIfNoExpiry { get; set; } = true;

    /// <inheritdoc/>
    public bool RespectUiHidden { get; set; } = true;

    /// <inheritdoc/>
    public bool Minimized { get; set; } = true;

    /// <inheritdoc/>
    public bool UserDismissable { get; set; } = true;

    /// <inheritdoc/>
    public float Progress { get; set; } = 1f;
}
