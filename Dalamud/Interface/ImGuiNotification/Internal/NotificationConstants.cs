using System.Numerics;

using Dalamud.Interface.Utility;

namespace Dalamud.Interface.ImGuiNotification.Internal;

/// <summary>Constants for drawing notification windows.</summary>
internal static class NotificationConstants
{
    // .............................[..]
    // ..when.......................[XX]
    // ..                             ..
    // ..[i]..title title title title ..
    // ..     by this_plugin          ..
    // ..                             ..
    // ..     body body body body     ..
    // ..     some more wrapped body  ..
    // ..                             ..
    // ..     action buttons          ..
    // .................................

    /// <summary>The string to show in place of this_plugin if the notification is shown by Dalamud.</summary>
    public const string DefaultInitiator = "Dalamud";

    /// <summary>The size of the icon.</summary>
    public const float IconSize = 32;

    /// <summary>The background opacity of a notification window.</summary>
    public const float BackgroundOpacity = 0.82f;

    /// <summary>The duration of indeterminate progress bar loop in milliseconds.</summary>
    public const float IndeterminateProgressbarLoopDuration = 2000f;

    /// <summary>Duration of show animation.</summary>
    public static readonly TimeSpan ShowAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultDisplayDuration = TimeSpan.FromSeconds(3);

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultHoverExtendDuration = TimeSpan.FromSeconds(3);

    /// <summary>Duration of hide animation.</summary>
    public static readonly TimeSpan HideAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Duration of hide animation.</summary>
    public static readonly TimeSpan ProgressAnimationDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Text color for the when.</summary>
    public static readonly Vector4 WhenTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the close button [X].</summary>
    public static readonly Vector4 CloseTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the title.</summary>
    public static readonly Vector4 TitleTextColor = new(1f, 1f, 1f, 1f);

    /// <summary>Text color for the name of the initiator.</summary>
    public static readonly Vector4 BlameTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the body.</summary>
    public static readonly Vector4 BodyTextColor = new(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>Gets the relative time format strings.</summary>
    private static readonly (TimeSpan MinSpan, string? FormatString)[] RelativeFormatStrings =
    {
        (TimeSpan.FromDays(7), null),
        (TimeSpan.FromDays(2), "{0:%d} days ago"),
        (TimeSpan.FromDays(1), "yesterday"),
        (TimeSpan.FromHours(2), "{0:%h} hours ago"),
        (TimeSpan.FromHours(1), "an hour ago"),
        (TimeSpan.FromMinutes(2), "{0:%m} minutes ago"),
        (TimeSpan.FromMinutes(1), "a minute ago"),
        (TimeSpan.FromSeconds(2), "{0:%s} seconds ago"),
        (TimeSpan.FromSeconds(1), "a second ago"),
        (TimeSpan.MinValue, "just now"),
    };

    /// <summary>Gets the scaled padding of the window (dot(.) in the above diagram).</summary>
    public static float ScaledWindowPadding => MathF.Round(16 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the distance from the right bottom border of the viewport
    /// to the right bottom border of a notification window.
    /// </summary>
    public static float ScaledViewportEdgeMargin => MathF.Round(20 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled gap between two notification windows.</summary>
    public static float ScaledWindowGap => MathF.Round(10 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled gap between components.</summary>
    public static float ScaledComponentGap => MathF.Round(5 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled size of the icon.</summary>
    public static float ScaledIconSize => MathF.Round(IconSize * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the height of the expiry progress bar.</summary>
    public static float ScaledExpiryProgressBarHeight => MathF.Round(2 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the string format of the initiator name field, if the initiator is unloaded.</summary>
    public static string UnloadedInitiatorNameFormat => "{0} (unloaded)";

    /// <summary>Formats an instance of <see cref="DateTime"/> as a relative time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatRelativeDateTime(this DateTime when)
    {
        var ts = DateTime.Now - when;
        foreach (var (minSpan, formatString) in RelativeFormatStrings)
        {
            if (ts < minSpan)
                continue;
            if (formatString is null)
                break;
            return string.Format(formatString, ts);
        }

        return when.FormatAbsoluteDateTime();
    }

    /// <summary>Formats an instance of <see cref="DateTime"/> as an absolute time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    public static string FormatAbsoluteDateTime(this DateTime when) => $"{when:G}";
}
