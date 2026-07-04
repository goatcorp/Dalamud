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

    /// <summary>
    /// Initializes a new instance of the <see cref="OverlayAddon"/> class.
    /// </summary>
    public OverlayAddon()
    {
        this.IsOverlayAddon = true;
    }

    /// <summary>
    /// Attach a node to this overlay addon.
    /// </summary>
    /// <param name="node">Node to attach.</param>
    /// <returns>true if the node was attached.</returns>
    public unsafe bool AttachNode(IOverlayNode node)
    {
        if (!this.attachedNodes.Contains(node))
        {
            this.attachedNodes.Add(node);

            var nodePointer = node.GetAsAtkResNode();
            if (nodePointer is null) return false;

            nodePointer->NodeId = (uint)this.attachedNodes.Count + 1;
            node.PerformAttach((nint)this.InternalAddon);

            this.InternalAddon->UldManager.UpdateDrawNodeList();
            this.InternalAddon->UpdateCollisionNodeList(false);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the specified node.
    /// </summary>
    /// <param name="node">Node to detach.</param>
    /// <returns>true if the node was removed.</returns>
    public unsafe bool DetachNode(IOverlayNode node)
    {
        if (this.attachedNodes.Remove(node))
        {
            node.PerformDetach();

            this.InternalAddon->UldManager.UpdateDrawNodeList();
            this.InternalAddon->UpdateCollisionNodeList(false);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        base.OnSetup(addon, atkValueSpan);

        foreach (var node in this.attachedNodes)
        {
            node.PerformAttach((nint)this.InternalAddon);
        }

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
            node.PerformDetach();
        }

        this.InternalAddon->UldManager.UpdateDrawNodeList();
        this.InternalAddon->UpdateCollisionNodeList(false);

        base.OnFinalize(addon);
    }
}
