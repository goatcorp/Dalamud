using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Wrapper around an AtkTimelineAnimation array. Not intended for external use.
/// </summary>
internal unsafe class TimelineAnimationArray : IDisposable
{
    private List<TimelineAnimation> timelineAnimations = [];

    /// <summary>
    /// Gets the number of timeline animations.
    /// </summary>
    public uint Count { get; private set; }

    /// <summary>
    /// Gets or sets the timeline animations used.
    /// </summary>
    public List<TimelineAnimation> Animations
    {
        get => this.timelineAnimations;
        set
        {
            this.timelineAnimations = value;
            this.Resync();
        }
    }

    /// <summary>
    /// Gets the pointer to the allocated timeline animation array data.
    /// </summary>
    internal AtkTimelineAnimation* InternalTimelineArray { get; private set; } = null;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var animation in this.timelineAnimations)
        {
            animation.Dispose();
        }

        IMemorySpace.Free(this.InternalTimelineArray, (ulong)sizeof(AtkTimelineAnimation) * this.Count);
        this.InternalTimelineArray = null;
    }

    private void Resync()
    {
        // Free existing array, we will completely rebuild it
        if (this.InternalTimelineArray is not null)
        {
            IMemorySpace.Free(this.InternalTimelineArray, (ulong)sizeof(AtkTimelineKeyFrame) * this.Count);
            this.InternalTimelineArray = null;
        }

        // Allocate new array
        this.InternalTimelineArray = (AtkTimelineAnimation*)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(AtkTimelineAnimation) * this.timelineAnimations.Count), 8);

        // Copy all Animations into it
        foreach (var index in Enumerable.Range(0, this.timelineAnimations.Count))
        {
            this.InternalTimelineArray[index] = *this.timelineAnimations[index].InternalAnimation;
        }

        this.Count = (uint)this.timelineAnimations.Count;
    }
}
