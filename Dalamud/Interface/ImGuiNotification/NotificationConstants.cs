using System.Diagnostics;
using System.Numerics;

using Dalamud.Interface.Utility;

namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Constants for drawing notification windows.</summary>
public static class NotificationConstants
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

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultDisplayDuration = TimeSpan.FromSeconds(3);

    /// <summary>Default duration of the notification, after the mouse cursor leaves the notification window.</summary>
    public static readonly TimeSpan DefaultHoverExtendDuration = TimeSpan.FromSeconds(3);

    /// <summary>The string to show in place of this_plugin if the notification is shown by Dalamud.</summary>
    internal const string DefaultInitiator = "Dalamud";

    /// <summary>The size of the icon.</summary>
    internal const float IconSize = 32;

    /// <summary>The background opacity of a notification window.</summary>
    internal const float BackgroundOpacity = 0.82f;

    /// <summary>The duration of indeterminate progress bar loop in milliseconds.</summary>
    internal const float IndeterminateProgressbarLoopDuration = 2000f;

    /// <summary>The duration of the progress wave animation in milliseconds.</summary>
    internal const float ProgressWaveLoopDuration = 2000f;

    /// <summary>The time ratio of a progress wave loop where the animation is idle.</summary>
    internal const float ProgressWaveIdleTimeRatio = 0.5f;

    /// <summary>The time ratio of a non-idle portion of the progress wave loop where the color is the most opaque.
    /// </summary>
    internal const float ProgressWaveLoopMaxColorTimeRatio = 0.7f;

    /// <summary>Duration of show animation.</summary>
    internal static readonly TimeSpan ShowAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Duration of hide animation.</summary>
    internal static readonly TimeSpan HideAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Duration of progress change animation.</summary>
    internal static readonly TimeSpan ProgressChangeAnimationDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Duration of expando animation.</summary>
    internal static readonly TimeSpan ExpandoAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Text color for the when.</summary>
    internal static readonly Vector4 WhenTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the close button [X].</summary>
    internal static readonly Vector4 CloseTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the title.</summary>
    internal static readonly Vector4 TitleTextColor = new(1f, 1f, 1f, 1f);

    /// <summary>Text color for the name of the initiator.</summary>
    internal static readonly Vector4 BlameTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the body.</summary>
    internal static readonly Vector4 BodyTextColor = new(0.9f, 0.9f, 0.9f, 1f);

    /// <summary>Color for the background progress bar (determinate progress only).</summary>
    internal static readonly Vector4 BackgroundProgressColorMax = new(1f, 1f, 1f, 0.1f);

    /// <summary>Color for the background progress bar (determinate progress only).</summary>
    internal static readonly Vector4 BackgroundProgressColorMin = new(1f, 1f, 1f, 0.05f);

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

    /// <summary>Gets the relative time format strings.</summary>
    private static readonly (TimeSpan MinSpan, string FormatString)[] RelativeFormatStringsShort =
    {
        (TimeSpan.FromDays(1), "{0:%d}d"),
        (TimeSpan.FromHours(1), "{0:%h}h"),
        (TimeSpan.FromMinutes(1), "{0:%m}m"),
        (TimeSpan.FromSeconds(1), "{0:%s}s"),
        (TimeSpan.MinValue, "now"),
    };

    /// <summary>Gets the scaled padding of the window (dot(.) in the above diagram).</summary>
    internal static float ScaledWindowPadding => MathF.Round(16 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the distance from the right bottom border of the viewport
    /// to the right bottom border of a notification window.
    /// </summary>
    internal static float ScaledViewportEdgeMargin => MathF.Round(20 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled gap between two notification windows.</summary>
    internal static float ScaledWindowGap => MathF.Round(10 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled gap between components.</summary>
    internal static float ScaledComponentGap => MathF.Round(5 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the scaled size of the icon.</summary>
    internal static float ScaledIconSize => MathF.Round(IconSize * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the height of the expiry progress bar.</summary>
    internal static float ScaledExpiryProgressBarHeight => MathF.Round(3 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the string format of the initiator name field, if the initiator is unloaded.</summary>
    internal static string UnloadedInitiatorNameFormat => "{0} (unloaded)";

    /// <summary>Formats an instance of <see cref="DateTime"/> as a relative time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    internal static string FormatRelativeDateTime(this DateTime when)
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
    internal static string FormatAbsoluteDateTime(this DateTime when) => $"{when:G}";

    /// <summary>Formats an instance of <see cref="DateTime"/> as a relative time.</summary>
    /// <param name="when">When.</param>
    /// <returns>The formatted string.</returns>
    internal static string FormatRelativeDateTimeShort(this DateTime when)
    {
        var ts = DateTime.Now - when;
        foreach (var (minSpan, formatString) in RelativeFormatStringsShort)
        {
            if (ts < minSpan)
                continue;
            return string.Format(formatString, ts);
        }

        Debug.Assert(false, "must not reach here");
        return "???";
    }
}
