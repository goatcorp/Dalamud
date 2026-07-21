using Dalamud.NativeUi.Timelines;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// .
/// </summary>
internal abstract unsafe partial class NodeBase
{
    /// <summary>
    /// Gets this nodes timeline.
    /// </summary>
    public Timeline? Timeline { get; private set; }

    /// <summary>
    /// Adds a built timeline to this node.
    /// </summary>
    /// <remarks>
    /// Disposes the previously used timeline. <em>Potentially volatile when replacing an existing timeline</em>.
    /// </remarks>
    /// <param name="timeline">The timeline to add to this node.</param>
    public void AddTimeline(Timeline timeline)
    {
        this.Timeline?.Dispose();

        this.Timeline = timeline;
        ResNode->Timeline = timeline.InternalTimeline;
        timeline.OwnerNode = this.ResNode;
    }
}
