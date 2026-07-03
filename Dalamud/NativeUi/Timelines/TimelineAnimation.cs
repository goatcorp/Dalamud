using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed class representing a AtkTimelineAnimation.
/// </summary>
internal unsafe class TimelineAnimation : IDisposable
{
    private List<TimelineKeyFrame> internalKeyFrames = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineAnimation"/> class.
    /// </summary>
    public TimelineAnimation()
    {
        this.InternalAnimation = (AtkTimelineAnimation*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTimelineAnimation), 8);

        this.InternalAnimation->StartFrameIdx = 0;
        this.InternalAnimation->EndFrameIdx = 0;

        foreach (ref var value in this.InternalAnimation->KeyGroups)
        {
            value.Type = AtkTimelineKeyGroupType.None;
        }
    }

    /// <summary>
    /// Gets or sets the starting frame index for this animation.
    /// </summary>
    /// <remarks>
    /// Must be less than <see cref="EndFrameId"/>.
    /// </remarks>
    public int StartFrameId
    {
        get => this.InternalAnimation->StartFrameIdx;
        set => this.InternalAnimation->StartFrameIdx = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the ending frame index for this animation.
    /// </summary>
    /// <remarks>
    /// Must be greater than <see cref="StartFrameId"/>.
    /// </remarks>
    public int EndFrameId
    {
        get => this.InternalAnimation->EndFrameIdx;
        set => this.InternalAnimation->EndFrameIdx = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the keyframes used.
    /// </summary>
    /// <remarks>
    /// Use <see cref="TimelineBuilder"/> to more easily edit keyframes.
    /// </remarks>
    public List<TimelineKeyFrame> KeyFrames
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
    internal AtkTimelineAnimation* InternalAnimation { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.InternalAnimation is null) return;

        foreach (ref var spanGroup in this.InternalAnimation->KeyGroups)
        {
            IMemorySpace.Free(spanGroup.KeyFrames);
            spanGroup.KeyFrames = null;
            spanGroup.KeyFrameCount = 0;
        }

        IMemorySpace.Free(this.InternalAnimation);
        this.InternalAnimation = null;
    }

    private void Resync()
    {
        foreach (var keyFrameSet in this.internalKeyFrames.GroupBy(frame => frame.GroupSelector))
        {
            ref var keyFrameGroup = ref this.InternalAnimation->KeyGroups[(int)keyFrameSet.Key];
            keyFrameGroup.Type = keyFrameSet.First().GroupType;

            if (keyFrameGroup.KeyFrames is not null)
            {
                IMemorySpace.Free(keyFrameGroup.KeyFrames, (ulong)sizeof(AtkTimelineKeyFrame) * keyFrameGroup.KeyFrameCount);
                keyFrameGroup.KeyFrames = null;
            }

            keyFrameGroup.KeyFrames = (AtkTimelineKeyFrame*)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(AtkTimelineKeyFrame) * keyFrameSet.Count()), 8);

            var index = 0;
            foreach (var keyframe in keyFrameSet)
            {
                keyFrameGroup.KeyFrames[index] = keyframe;
                index++;
            }

            keyFrameGroup.KeyFrameCount = (ushort)keyFrameSet.Count();
        }
    }
}
