using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Represents the native data of a single keyframe and an adaptor to the native struct. Not intended for external use.
/// </summary>
internal abstract class TimelineKeyFrame
{
    /// <summary>
    /// Gets or sets which sub index this keyframe belongs to.
    /// </summary>
    public KeyFrameGroupType GroupSelector { get; set; }

    /// <summary>
    /// Gets or sets which main index this keyframe belongs to.
    /// </summary>
    public AtkTimelineKeyGroupType GroupType { get; set; }

    /// <summary>
    /// Gets or sets the speed start.
    /// </summary>
    /// <remarks>
    /// Unknown what this is actually doing.
    /// </remarks>
    public float SpeedStart { get; set; } = 0.0f;

    /// <summary>
    /// Gets or sets the speed end.
    /// </summary>
    /// <remarks>
    /// Unknown what this is actually doing.
    /// </remarks>
    public float SpeedEnd { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the frame index that this keyframe represents.
    /// </summary>
    public required int FrameIndex { get; set; }

    /// <summary>
    /// Gets or sets value blending mode.
    /// </summary>
    public AtkTimelineInterpolation Interpolation { get; set; } = AtkTimelineInterpolation.Linear;

    /// <summary>
    /// Gets or sets the actual data this keyframe is wrapping.
    /// </summary>
    public AtkTimelineKeyValue Value { get; set; }

    /// <summary>
    /// Conversion operator for native interop.
    /// </summary>
    /// <param name="frame">Frame to convert.</param>
    public static implicit operator AtkTimelineKeyFrame(TimelineKeyFrame frame) => new()
    {
        Interpolation = frame.Interpolation,
        SpeedCoefficient1 = frame.SpeedStart,
        SpeedCoefficient2 = frame.SpeedEnd,
        FrameIdx = (ushort)frame.FrameIndex,
        Value = frame.Value,
    };
}
