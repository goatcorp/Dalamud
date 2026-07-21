using System.Collections.Generic;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed adaptor for native structs. Not intended for external use.
/// </summary>
internal unsafe class TimelineResource : IDisposable
{
    private readonly TimelineAnimationArray animationArray;
    private readonly TimelineLabelSetArray labelsArray;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineResource"/> class.
    /// </summary>
    public TimelineResource()
    {
        this.InternalResource = (AtkTimelineResource*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTimelineResource), 8);

        this.InternalResource->Id = 2;
        this.InternalResource->AnimationCount = 0;
        this.InternalResource->LabelSetCount = 0;

        this.animationArray = new TimelineAnimationArray();
        this.InternalResource->Animations = this.animationArray.InternalTimelineArray;

        this.labelsArray = new TimelineLabelSetArray();
        this.InternalResource->LabelSets = this.labelsArray.InternalLabelSetArray;
    }

    /// <summary>
    /// Gets or sets the animation keyframes.
    /// </summary>
    public List<TimelineAnimation> Animations
    {
        get => this.animationArray.Animations;
        set
        {
            this.animationArray.Animations = value;
            this.InternalResource->Animations = this.animationArray.InternalTimelineArray;
            this.InternalResource->AnimationCount = (ushort)this.animationArray.Count;
        }
    }

    /// <summary>
    /// Gets or sets the animation label sets.
    /// </summary>
    public List<TimelineLabelSet> LabelSets
    {
        get => this.labelsArray.LabelSets;
        set
        {
            this.labelsArray.LabelSets = value;
            this.InternalResource->LabelSets = this.labelsArray.InternalLabelSetArray;
            this.InternalResource->LabelSetCount = (ushort)this.labelsArray.Count;
        }
    }

    /// <summary>
    /// Gets or sets the internal resource id.
    /// </summary>
    public int Id
    {
        get => (int)this.InternalResource->Id;
        set => this.InternalResource->Id = (uint)value;
    }

    /// <summary>
    /// Gets the pointer to the allocated timeline resource data.
    /// </summary>
    internal AtkTimelineResource* InternalResource { get; private set; }

    /// <inheritdoc />
    public void Dispose()
    {
        this.animationArray.Dispose();
        this.labelsArray.Dispose();

        IMemorySpace.Free(this.InternalResource);
        this.InternalResource = null;
    }
}
