using System.Numerics;

using Dalamud.Interface.Utility;

namespace Dalamud.Interface.Internal.Notifications;

/// <summary>
/// Constants for drawing notification windows.
/// </summary>
internal static class NotificationConstants
{
    // ..............................[X]
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

    /// <summary>Duration of show animation.</summary>
    public static readonly TimeSpan ShowAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultDisplayDuration = TimeSpan.FromSeconds(3);

    /// <summary>Default duration of the notification.</summary>
    public static readonly TimeSpan DefaultHoverExtendDuration = TimeSpan.FromSeconds(3);

    /// <summary>Duration of hide animation.</summary>
    public static readonly TimeSpan HideAnimationDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>Text color for the close button [X].</summary>
    public static readonly Vector4 CloseTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the title.</summary>
    public static readonly Vector4 TitleTextColor = new(1f, 1f, 1f, 1f);

    /// <summary>Text color for the name of the initiator.</summary>
    public static readonly Vector4 BlameTextColor = new(0.8f, 0.8f, 0.8f, 1f);

    /// <summary>Text color for the body.</summary>
    public static readonly Vector4 BodyTextColor = new(0.9f, 0.9f, 0.9f, 1f);

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
    
    /// <summary>Gets the scaled size of the close button.</summary>
    public static float ScaledCloseButtonMinSize => MathF.Round(16 * ImGuiHelpers.GlobalScale);

    /// <summary>Gets the height of the expiry progress bar.</summary>
    public static float ScaledExpiryProgressBarHeight => MathF.Round(2 * ImGuiHelpers.GlobalScale);
}
