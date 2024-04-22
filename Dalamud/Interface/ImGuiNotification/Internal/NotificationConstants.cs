using System.Numerics;

using CheapLoc;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Notifications;
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

    /// <summary>The string to measure size of, to decide the width of notification windows.</summary>
    /// <remarks>Probably not worth localizing.</remarks>
    public const string NotificationWidthMeasurementString =
        "The width of this text will decide the width\n" +
        "of the notification window.";

    /// <summary>The ratio of maximum notification window width w.r.t. main viewport width.</summary>
    public const float MaxNotificationWindowWidthWrtMainViewportWidth = 2f / 3;

    /// <summary>The size of the icon.</summary>
    public const float IconSize = 32;

    /// <summary>The background opacity of a notification window.</summary>
    public const float BackgroundOpacity = 0.82f;

    /// <summary>The duration of indeterminate progress bar loop in milliseconds.</summary>
    public const float IndeterminateProgressbarLoopDuration = 2000f;

    /// <summary>The duration of indeterminate pie loop in milliseconds.</summary>
    /// <remarks>Note that this value is applicable when reduced motion configuration is on.</remarks>
    public const float IndeterminatePieLoopDuration = 8000f;

    /// <summary>The duration of the progress wave animation in milliseconds.</summary>
    public const float ProgressWaveLoopDuration = 2000f;

    /// <summary>The time ratio of a progress wave loop where the animation is idle.</summary>
    public const float ProgressWaveIdleTimeRatio = 0.5f;

    /// <summary>The time ratio of a non-idle portion of the progress wave loop where the color is the most opaque.
    /// </summary>
    public const float ProgressWaveLoopMaxColorTimeRatio = 0.7f;

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(7);

    /// <summary>Duration of show animation.</summary>
    public static readonly TimeSpan ShowAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Duration of hide animation.</summary>
    public static readonly TimeSpan HideAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Duration of progress change animation.</summary>
    public static readonly TimeSpan ProgressChangeAnimationDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Duration of expando animation.</summary>
    public static readonly TimeSpan ExpandoAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Text color for the rectangular border when the notification is focused.</summary>
    public static readonly Vector4 FocusBorderColor = new(0.4f, 0.4f, 0.4f, 1f);

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

    /// <summary>Color for the background progress bar (determinate progress only).</summary>
    public static readonly Vector4 BackgroundProgressColorMax = new(1f, 1f, 1f, 0.1f);

    /// <summary>Color for the background progress bar (determinate progress only).</summary>
    public static readonly Vector4 BackgroundProgressColorMin = new(1f, 1f, 1f, 0.05f);

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
    public static float ScaledExpiryProgressBarHeight => MathF.Round(3 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the thickness of the focus indicator rectangle.</summary>
    public static float FocusIndicatorThickness => MathF.Round(3 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the string to show in place of this_plugin if the notification is shown by Dalamud.</summary>
    public static string DefaultInitiator => Loc.Localize("NotificationConstants.DefaultInitiator", "Dalamud");

    /// <summary>Gets the string format of the initiator name field, if the initiator is unloaded.</summary>
    public static string UnloadedInitiatorNameFormat =>
        Loc.Localize("NotificationConstants.UnloadedInitiatorNameFormat", "{0} (unloaded)");

    /// <summary>Gets the color corresponding to the notification type.</summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The corresponding color.</returns>
    public static Vector4 ToColor(this NotificationType type) => type switch
    {
        NotificationType.None => ImGuiColors.DalamudWhite,
        NotificationType.Success => ImGuiColors.HealerGreen,
        NotificationType.Warning => ImGuiColors.DalamudOrange,
        NotificationType.Error => ImGuiColors.DalamudRed,
        NotificationType.Info => ImGuiColors.TankBlue,
        _ => ImGuiColors.DalamudWhite,
    };

    /// <summary>Gets the <see cref="FontAwesomeIcon"/> char value corresponding to the notification type.</summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The corresponding char, or null.</returns>
    public static char ToChar(this NotificationType type) => type switch
    {
        NotificationType.None => '\0',
        NotificationType.Success => FontAwesomeIcon.CheckCircle.ToIconChar(),
        NotificationType.Warning => FontAwesomeIcon.ExclamationCircle.ToIconChar(),
        NotificationType.Error => FontAwesomeIcon.TimesCircle.ToIconChar(),
        NotificationType.Info => FontAwesomeIcon.InfoCircle.ToIconChar(),
        _ => '\0',
    };

    /// <summary>Gets the localized title string corresponding to the notification type.</summary>
    /// <param name="type">The notification type.</param>
    /// <returns>The corresponding title.</returns>
    public static string? ToTitle(this NotificationType type) => type switch
    {
        NotificationType.None => null,
        NotificationType.Success => Loc.Localize("NotificationConstants.Title.Success", "Success"),
        NotificationType.Warning => Loc.Localize("NotificationConstants.Title.Warning", "Warning"),
        NotificationType.Error => Loc.Localize("NotificationConstants.Title.Error", "Error"),
        NotificationType.Info => Loc.Localize("NotificationConstants.Title.Info", "Info"),
        _ => null,
    };
}
