using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed adaptor for native structs. Not intended for external use.
/// </summary>
internal class TimelineLabelSetKeyFrame : TimelineKeyFrame
{
    private AtkTimelineLabel data;

    /// <summary>
    /// Gets or sets the timeline jump behavior.
    /// </summary>
    public AtkTimelineJumpBehavior JumpBehavior
    {
        get => this.data.JumpBehavior;
        set
        {
            this.data.JumpBehavior = value;
            this.UpdateValue();
        }
    }

    /// <summary>
    /// Gets or sets the timelines label id.
    /// </summary>
    public int LabelId
    {
        get => this.data.LabelId;
        set
        {
            this.data.LabelId = (ushort)value;
            this.UpdateValue();
        }
    }

    /// <summary>
    /// Gets or sets the id that will be jumped to on completion.
    /// </summary>
    public int JumpLabelId
    {
        get => this.data.JumpLabelId;
        set
        {
            this.data.JumpLabelId = (byte)value;
            this.UpdateValue();
        }
    }

    private void UpdateValue()
    {
        this.Value = new AtkTimelineKeyValue
        {
            Label = this.data,
        };

        this.GroupType = AtkTimelineKeyGroupType.Label;
        this.SpeedEnd = 0.0f;
        this.Interpolation = AtkTimelineInterpolation.None;
        this.GroupSelector = KeyFrameGroupType.TextLabel;
    }
}
