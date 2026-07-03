using System.Collections.Generic;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Timelines;

/// <summary>
/// Managed adaptor for native data. Not intended for external use.
/// </summary>
internal unsafe class TimelineLabelSetArray : IDisposable
{
    private List<TimelineLabelSet> labelSets = [];

    /// <summary>
    /// Gets the number of label sets that exist.
    /// </summary>
    public uint Count { get; private set; }

    /// <summary>
    /// Gets or sets the label sets.
    /// </summary>
    public List<TimelineLabelSet> LabelSets
    {
        get => this.labelSets;
        set
        {
            this.labelSets = value;
            this.Resync();
        }
    }

    /// <summary>
    /// Gets the pointer to the allocated label set array data.
    /// </summary>
    internal AtkTimelineLabelSet* InternalLabelSetArray { get; private set; } = null;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var labelSet in this.labelSets)
        {
            labelSet.Dispose();
        }

        IMemorySpace.Free(this.InternalLabelSetArray, (ulong)sizeof(AtkTimelineLabelSet) * this.Count);
        this.InternalLabelSetArray = null;
    }

    private void Resync()
    {
        // Free existing array, we will completely rebuild it
        if (this.InternalLabelSetArray is not null)
        {
            IMemorySpace.Free(this.InternalLabelSetArray, (ulong)sizeof(AtkTimelineLabelSet) * this.Count);
            this.InternalLabelSetArray = null;
        }

        // Allocate new array
        this.InternalLabelSetArray = (AtkTimelineLabelSet*)IMemorySpace.GetUISpace()->Malloc((ulong)(sizeof(AtkTimelineLabelSet) * this.labelSets.Count), 8);

        // Copy all Animations into it
        foreach (var index in Enumerable.Range(0, this.labelSets.Count))
        {
            this.InternalLabelSetArray[index] = *this.labelSets[index].InternalLabelSet;
        }

        this.Count = (uint)this.labelSets.Count;
    }
}
