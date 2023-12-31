using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Notifications;

/// <summary>
/// Class handling notifications/toasts in ImGui.
/// Ported from https://github.com/patrickcjk/imgui-notify.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class NotificationManager : IServiceType
{
    /// <summary>
    /// Value indicating the bottom-left X padding.
    /// </summary>
    internal const float NotifyPaddingX = 20.0f;

    /// <summary>
    /// Value indicating the bottom-left Y padding.
    /// </summary>
    internal const float NotifyPaddingY = 20.0f;

    /// <summary>
    /// Value indicating the Y padding between each message.
    /// </summary>
    internal const float NotifyPaddingMessageY = 10.0f;

    /// <summary>
    /// Value indicating the fade-in and out duration.
    /// </summary>
    internal const int NotifyFadeInOutTime = 500;

    /// <summary>
    /// Value indicating the default time until the notification is dismissed.
    /// </summary>
    internal const int NotifyDefaultDismiss = 3000;

    /// <summary>
    /// Value indicating the maximum opacity.
    /// </summary>
    internal const float NotifyOpacity = 0.82f;

    /// <summary>
    /// Value indicating default window flags for the notifications.
    /// </summary>
    internal const ImGuiWindowFlags NotifyToastFlags =
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing;

    /// <summary>
    /// Value indicating interactible window flags for the notifications.
    /// </summary>
    internal const ImGuiWindowFlags NotifyToastFlagsInteractible =
        ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoDecoration |
        ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoFocusOnAppearing;

    private readonly List<Notification> notifications = new();

    [ServiceManager.ServiceConstructor]
    private NotificationManager()
    {
    }

    /// <summary>
    /// Add a notification to the notification queue.
    /// </summary>
    /// <param name="content">The content of the notification.</param>
    /// <param name="title">The title of the notification.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="msDelay">The time the notification should be displayed for.</param>
    /// <param name="persistent">Whether the notification is persistent.</param>
    /// <param name="interactible">Whether the notification is interactible.</param>
    /// <returns>The new notification.</returns>
    public Notification AddNotification(
        string content,
        string? title = null,
        NotificationType type = NotificationType.None,
        uint msDelay = NotifyDefaultDismiss,
        bool persistent = false,
        bool interactible = false)
    {
        var n = new Notification
        {
            Content = content,
            Title = title,
            NotificationType = type,
            DurationMs = msDelay,
            Persistent = persistent,
            Interactible = interactible,
        };

        Service<Framework>.GetAsync().ContinueWith(_ => this.notifications.Add(n));
        return n;
    }

    /// <summary>
    /// Draw all currently queued notifications.
    /// </summary>
    public void Draw()
    {
        var viewportSize = ImGuiHelpers.MainViewport.Size;
        var height = 0f;

        for (var i = 0; i < this.notifications.Count; i++)
        {
            var tn = this.notifications.ElementAt(i);

            if (tn.GetPhase() == Notification.Phase.Expired)
            {
                this.notifications.RemoveAt(i);
                i--;
                continue;
            }

            var opacity = tn.GetFadePercent();

            var iconColor = tn.Color;
            iconColor.W = opacity;

            var windowName = $"##NOTIFY{i}";

            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.SetNextWindowBgAlpha(opacity);
            ImGui.SetNextWindowPos(ImGuiHelpers.MainViewport.Pos + new Vector2(viewportSize.X - NotifyPaddingX, viewportSize.Y - NotifyPaddingY - height), ImGuiCond.Always, Vector2.One);
            ImGui.Begin(windowName, tn.Interactible ? NotifyToastFlagsInteractible : NotifyToastFlags);

            ImGui.PushTextWrapPos(viewportSize.X / 3.0f);

            var wasTitleRendered = false;

            if (!tn.Icon.IsNullOrEmpty())
            {
                wasTitleRendered = true;
                ImGui.PushFont(InterfaceManager.IconFont);
                ImGui.TextColored(iconColor, tn.Icon);
                ImGui.PopFont();
            }

            var textColor = ImGuiColors.DalamudWhite;
            textColor.W = opacity;

            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            if (!tn.Title.IsNullOrEmpty())
            {
                if (!tn.Icon.IsNullOrEmpty())
                {
                    ImGui.SameLine();
                }

                ImGui.TextUnformatted(tn.Title);
                wasTitleRendered = true;
            }
            else if (!tn.DefaultTitle.IsNullOrEmpty())
            {
                if (!tn.Icon.IsNullOrEmpty())
                {
                    ImGui.SameLine();
                }

                ImGui.TextUnformatted(tn.DefaultTitle);
                wasTitleRendered = true;
            }

            if (wasTitleRendered && !tn.Content.IsNullOrEmpty())
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5.0f);
            }

            if (!tn.Content.IsNullOrEmpty())
            {
                if (wasTitleRendered)
                {
                    ImGui.Separator();
                }

                ImGui.TextUnformatted(tn.Content);
            }

            ImGui.PushID(i);
            tn.InvokeDraw();
            ImGui.PopID();

            ImGui.PopStyleColor();

            ImGui.PopTextWrapPos();

            height += ImGui.GetWindowHeight() + NotifyPaddingMessageY;

            ImGui.End();
        }
    }

    /// <summary>
    /// Container class for notifications.
    /// </summary>
    internal sealed class Notification : IDisposable
    {
        /// <summary>
        /// Called upon drawing the notification entry.
        /// </summary>
        internal event Action<Notification>? Draw;

        /// <summary>
        /// Possible notification phases.
        /// </summary>
        internal enum Phase
        {
            /// <summary>
            /// Phase indicating fade-in.
            /// </summary>
            FadeIn,

            /// <summary>
            /// Phase indicating waiting until fade-out.
            /// </summary>
            Wait,

            /// <summary>
            /// Phase indicating fade-out.
            /// </summary>
            FadeOut,

            /// <summary>
            /// Phase indicating that the notification has expired.
            /// </summary>
            Expired,
        }

        /// <summary>
        /// Gets the type of the notification.
        /// </summary>
        internal NotificationType NotificationType { get; init; }

        /// <summary>
        /// Gets the title of the notification.
        /// </summary>
        internal string? Title { get; init; }

        /// <summary>
        /// Gets or sets the content of the notification.
        /// </summary>
        internal string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the notification is interactible.
        /// </summary>
        internal bool Interactible { get; init; }

        /// <summary>
        /// Gets or sets the duration of the notification in milliseconds.
        /// </summary>
        internal uint DurationMs { get; set; }

        /// <summary>
        /// Gets a value indicating whether this notification is persistent.
        /// </summary>
        internal bool Persistent { get; init; }

        /// <summary>
        /// Gets the manual dismissal time of the notification.
        /// </summary>
        internal DateTime ForceDismissOn { get; private set; } = DateTime.MaxValue;

        /// <summary>
        /// Gets the creation time of the notification.
        /// </summary>
        internal DateTime CreationTime { get; init; } = DateTime.Now;

        /// <summary>
        /// Gets the default color of the notification.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="NotificationType"/> is set to an out-of-range value.</exception>
        internal Vector4 Color => this.NotificationType switch
        {
            NotificationType.None => ImGuiColors.DalamudWhite,
            NotificationType.Success => ImGuiColors.HealerGreen,
            NotificationType.Warning => ImGuiColors.DalamudOrange,
            NotificationType.Error => ImGuiColors.DalamudRed,
            NotificationType.Info => ImGuiColors.TankBlue,
            _ => throw new ArgumentOutOfRangeException(),
        };

        /// <summary>
        /// Gets the icon of the notification.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="NotificationType"/> is set to an out-of-range value.</exception>
        internal string? Icon => this.NotificationType switch
        {
            NotificationType.None => null,
            NotificationType.Success => FontAwesomeIcon.CheckCircle.ToIconString(),
            NotificationType.Warning => FontAwesomeIcon.ExclamationCircle.ToIconString(),
            NotificationType.Error => FontAwesomeIcon.TimesCircle.ToIconString(),
            NotificationType.Info => FontAwesomeIcon.InfoCircle.ToIconString(),
            _ => throw new ArgumentOutOfRangeException(),
        };

        /// <summary>
        /// Gets the default title of the notification.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="NotificationType"/> is set to an out-of-range value.</exception>
        internal string? DefaultTitle => this.NotificationType switch
        {
            NotificationType.None => null,
            NotificationType.Success => NotificationType.Success.ToString(),
            NotificationType.Warning => NotificationType.Warning.ToString(),
            NotificationType.Error => NotificationType.Error.ToString(),
            NotificationType.Info => NotificationType.Info.ToString(),
            _ => throw new ArgumentOutOfRangeException(),
        };

        /// <summary>
        /// Gets the elapsed time since creating the notification.
        /// </summary>
        internal TimeSpan ElapsedTime => DateTime.Now - this.CreationTime;

        /// <inheritdoc/>
        void IDisposable.Dispose() => this.Dismiss(TimeSpan.FromMilliseconds(this.DurationMs));

        /// <summary>
        /// Dismisses this notification.
        /// </summary>
        /// <param name="wait">The wait time before starting dismissal fade-out.</param>
        internal void Dismiss(TimeSpan wait = default)
        {
            if (this.ForceDismissOn != DateTime.MaxValue)
                return;
            this.ForceDismissOn = DateTime.Now + wait;
        }

        /// <summary>
        /// Gets the phase of the notification.
        /// </summary>
        /// <returns>The phase of the notification.</returns>
        internal Phase GetPhase()
        {
            if (this.ForceDismissOn != DateTime.MaxValue)
            {
                var elapsed = (int)(DateTime.Now - this.ForceDismissOn).TotalMilliseconds;

                return elapsed switch
                {
                    > NotifyFadeInOutTime => Phase.Expired,
                    > 0 => Phase.FadeOut,
                    _ => Phase.Wait,
                };
            }
            else
            {
                var elapsed = (int)this.ElapsedTime.TotalMilliseconds;

                if (!this.Persistent)
                {
                    if (elapsed > NotifyFadeInOutTime + this.DurationMs + NotifyFadeInOutTime)
                        return Phase.Expired;
                    if (elapsed > NotifyFadeInOutTime + this.DurationMs)
                        return Phase.FadeOut;
                }

                if (elapsed > NotifyFadeInOutTime)
                    return Phase.Wait;
                return Phase.FadeIn;
            }
        }

        /// <summary>
        /// Gets the opacity of the notification.
        /// </summary>
        /// <returns>The opacity, in a range from 0 to 1.</returns>
        internal float GetFadePercent()
        {
            if (this.ForceDismissOn != DateTime.MaxValue)
            {
                var elapsedSinceDismiss = (int)(DateTime.Now - this.ForceDismissOn).TotalMilliseconds;
                var fadeOutProgress = Math.Clamp(1f - ((float)elapsedSinceDismiss / NotifyFadeInOutTime), 0f, 1f);
                var elapsedSinceCreate = this.ElapsedTime.TotalMilliseconds;
                var fadeInProgress = Math.Clamp((float)elapsedSinceCreate / NotifyFadeInOutTime, 0f, 1f);
                return Math.Min(fadeOutProgress, fadeInProgress) * NotifyOpacity;
            }
            else
            {
                var phase = this.GetPhase();
                var elapsed = this.ElapsedTime.TotalMilliseconds;

                if (phase == Phase.FadeIn)
                {
                    return (float)elapsed / NotifyFadeInOutTime * NotifyOpacity;
                }
                else if (phase == Phase.FadeOut)
                {
                    return (1.0f - (((float)elapsed - NotifyFadeInOutTime - this.DurationMs) /
                                    NotifyFadeInOutTime)) * NotifyOpacity;
                }

                return 1.0f * NotifyOpacity;
            }
        }

        /// <summary>
        /// Invokes <see cref="Draw"/>.
        /// </summary>
        internal void InvokeDraw() => this.Draw?.Invoke(this);
    }
}
