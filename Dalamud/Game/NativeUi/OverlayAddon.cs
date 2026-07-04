using System.Collections.Generic;

using Dalamud.NativeUi.BaseTypes.Addon;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.NativeUi;

/// <summary>
/// NativeAddon Implementation for use as an overlay addon within dalamud.
/// </summary>
internal class OverlayAddon : NativeAddon
{
    private readonly List<IOverlayNode> attachedNodes = [];
    private readonly List<IOverlayNode> queuedNodes = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="OverlayAddon"/> class.
    /// </summary>
    public OverlayAddon()
    {
        this.IsOverlayAddon = true;
    }

    /// <summary>
    /// Attach a node to this overlay addon.
    /// If the addon isn't ready yet, the nodes will be attached when it is.
    /// </summary>
    /// <param name="node">Node to attach.</param>
    public unsafe void AttachNode(IOverlayNode node)
    {
        if (!this.IsOpen)
        {
            this.queuedNodes.Add(node);
            return;
        }

        if (!this.attachedNodes.Contains(node))
        {
            var nodePointer = node.GetAsAtkResNode();
            if (node.GetAsAtkResNode() is null) return;

            nodePointer->NodeId = (uint)this.attachedNodes.Count + 1;

            node.PerformAttach(this.InternalAddon);

            this.attachedNodes.Add(node);

            this.InternalAddon->UldManager.UpdateDrawNodeList();
            this.InternalAddon->UpdateCollisionNodeList(false);
        }
    }

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    /// <param name="node">Node to detach.</param>
    public unsafe void DetachNode(IOverlayNode node)
    {
        // If this node hasn't been attached yet, remove it from queue.
        this.queuedNodes.Remove(node);

        // If it has been attached, remove and actually detach it.
        if (this.attachedNodes.Remove(node))
        {
            node.PerformDetach(this.InternalAddon);

            this.InternalAddon->UldManager.UpdateDrawNodeList();
            this.InternalAddon->UpdateCollisionNodeList(false);
        }
    }

    /// <inheritdoc/>
    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        base.OnSetup(addon, atkValueSpan);

        foreach (var node in this.queuedNodes)
        {
            node.PerformAttach((nint)this.InternalAddon);
            this.attachedNodes.Add(node);
        }

        this.queuedNodes.Clear();

        this.InternalAddon->UldManager.UpdateDrawNodeList();
        this.InternalAddon->UpdateCollisionNodeList(false);
    }

    /// <inheritdoc/>
    protected override unsafe void OnUpdate(AtkUnitBase* addon)
    {
        base.OnUpdate(addon);

        foreach (var node in this.attachedNodes)
        {
            node.Update();
        }
    }

    /// <inheritdoc/>
    protected override unsafe void OnFinalize(AtkUnitBase* addon)
    {
        // Detach nodes before native tries to dispose them!
        // We will need to re-attach these nodes once the overlay is reopened.
        foreach (var node in this.attachedNodes)
        {
            node.PerformDetach(this.InternalAddon);
            this.queuedNodes.Add(node);
        }

        this.attachedNodes.Clear();

        // Update native state to ensure all draw lists have all nodes fully removed
        // Else the game will call .Destructor(true) on them.
        this.InternalAddon->UldManager.UpdateDrawNodeList();
        this.InternalAddon->UpdateCollisionNodeList(false);

        base.OnFinalize(addon);
    }
}
