using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed adaptor to the native structs. Not intended for external use.
/// </summary>
internal unsafe class TimelineLabelSet : IDisposable
{
    private List<TimelineKeyFrame> internalKeyFrames = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineLabelSet"/> class.
    /// </summary>
    public TimelineLabelSet()
    {
        this.InternalLabelSet = (AtkTimelineLabelSet*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTimelineLabelSet), 8);

        this.InternalLabelSet->StartFrameIdx = 0;
        this.InternalLabelSet->EndFrameIdx = 0;
        this.InternalLabelSet->LabelKeyGroup.Type = AtkTimelineKeyGroupType.Label;
    }

    /// <summary>
    /// Gets or sets start frame id.
    /// </summary>
    public int StartFrameId
    {
        get => this.InternalLabelSet->StartFrameIdx;
        set => this.InternalLabelSet->StartFrameIdx = (ushort)value;
    }

    /// <summary>
    /// Gets or sets end frame id.
    /// </summary>
    public int EndFrameId
    {
        get => this.InternalLabelSet->EndFrameIdx;
        set => this.InternalLabelSet->EndFrameIdx = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the keyframe label sets.
    /// </summary>
    public List<TimelineKeyFrame> Labels
    {
        get => this.internalKeyFrames;
        set
        {
            this.internalKeyFrames = value;
            this.Resync();
        }
    }

    /// <summary>
    /// Gets pointer to the allocation timeline animation data.
    /// </summary>
    internal AtkTimelineLabelSet* InternalLabelSet { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        IMemorySpace.Free(this.InternalLabelSet);
        this.InternalLabelSet = null;
    }

    private void Resync()
    {
        ref var keyGroup = ref this.InternalLabelSet->LabelKeyGroup;

        // Free existing array, we will completely rebuild it
        if (keyGroup.KeyFrames is null)
        {
            IMemorySpace.Free(keyGroup.KeyFrames, (ulong)sizeof(AtkTimelineKeyFrame) * keyGroup.KeyFrameCount);
            keyGroup.KeyFrames = null;
        }

        // Allocate new array
        keyGroup.KeyFrames = (AtkTimelineKeyFrame*)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(AtkTimelineKeyFrame) * this.internalKeyFrames.Count), 8);

        var index = 0;
        foreach (var keyFrame in this.internalKeyFrames)
        {
            keyGroup.KeyFrames[index] = keyFrame;
            index++;
        }

        keyGroup.KeyFrameCount = (ushort)this.internalKeyFrames.Count;
    }
}
