using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.NativeUi.BaseTypes.Node;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Nodes.Layout;

/// <summary>
/// Abstract base class for nodes that are intended to help with laying out other nodes.
/// </summary>
internal abstract class LayoutListNode : ResNode
{
    private bool suppressRecalculateLayout;

    /// <summary>
    /// Gets get a readonly list of the contained nodes.
    /// </summary>
    public IReadOnlyList<NodeBase> Nodes => this.NodeList;

    /// <summary>
    /// Gets or sets a value indicating whether when true, will clip the nodes contents, preventing any contained nodes
    /// outside the area of this node from being visible.
    /// </summary>
    /// <remarks>
    /// If a node is being partially clipped, it will be un-interactable.
    /// </remarks>
    public bool ClipListContents
    {
        get => this.NodeFlags.HasFlag(NodeFlags.Clip);
        set
        {
            if (value)
            {
                this.AddNodeFlags(NodeFlags.Clip);
            }
            else
            {
                this.RemoveNodeFlags(NodeFlags.Clip);
            }
        }
    }

    /// <summary>
    /// Gets or sets spacing between items, does not apply to the first item.
    /// </summary>
    public float ItemSpacing { get; set; }

    /// <summary>
    /// Gets or sets spacing to apply before the first item.
    /// </summary>
    public float FirstItemSpacing { get; set; }

    /// <summary>
    /// Sets an init only collection of nodes, to add a predefined amount of nodes to the list.
    /// This is the preferred way of adding nodes.
    /// </summary>
    public ICollection<NodeBase> InitialNodes
    {
        set => this.AddNode(value);
    }

    /// <summary>
    /// Gets the list of nodes that this <see cref="LayoutListNode"/> is managing.
    /// </summary>
    protected List<NodeBase> NodeList { get; } = [];

    /// <summary>
    /// Get a readonly enumerable of the contained nodes of the specified type.
    /// </summary>
    /// <typeparam name="T">The NodeType to search for.</typeparam>
    /// <returns>An IEnumerable of Nodes.</returns>
    public IEnumerable<T> GetNodes<T>() where T : NodeBase => this.NodeList.OfType<T>();

    /// <summary>
    /// Recalculates the contained layout, and controller navigation values if applicable.
    /// </summary>
    public void RecalculateLayout()
    {
        if (this.suppressRecalculateLayout) return;

        foreach (var node in this.NodeList)
        {
            if (node is LayoutListNode subNode)
            {
                subNode.RecalculateLayout();
            }
        }

        this.OnRecalculateLayout();

        foreach (var node in this.NodeList)
        {
            if (node is LayoutListNode subNode)
            {
                subNode.RecalculateLayout();
            }
        }
    }

    /// <summary>
    /// Adds multiple nodes to the list. Added nodes are considered to be owned by the list.
    /// </summary>
    /// <param name="nodes">The nodes to add.</param>
    public virtual void AddNode(IEnumerable<NodeBase> nodes)
    {
        this.suppressRecalculateLayout = true;
        try
        {
            foreach (var node in nodes)
            {
                this.AddNode(node);
            }
        }
        finally
        {
            this.suppressRecalculateLayout = false;
        }

        this.RecalculateLayout();
    }

    /// <summary>
    /// Adds a single node to the list.
    /// </summary>
    /// <param name="node">Node to add.</param>
    /// <remarks>
    /// While this function accepts a nullable node, that's just for convenience, if the node is null it will not be added.
    /// </remarks>
    public virtual void AddNode(NodeBase? node)
    {
        if (node is null) return;

        this.NodeList.Add(node);

        node.AttachNode(this);

        this.RecalculateLayout();
    }

    /// <summary>
    /// Removes multiple nodes from the list. Removed nodes are disposed by the list.
    /// </summary>
    /// <param name="items">Nodes to remove.</param>
    public void RemoveNode(IEnumerable<NodeBase> items)
    {
        this.suppressRecalculateLayout = true;
        try
        {
            foreach (var node in items)
            {
                this.RemoveNode(node);
            }
        }
        finally
        {
            this.suppressRecalculateLayout = false;
        }

        this.RecalculateLayout();
    }

    /// <summary>
    /// Remove a single node from the list. Removed nodes are disposed by the list.
    /// </summary>
    /// <param name="node">Node to remove.</param>
    public virtual void RemoveNode(NodeBase node)
    {
        if (!this.NodeList.Contains(node)) return;

        this.NodeList.Remove(node);
        node.Dispose();

        this.RecalculateLayout();
    }

    /// <summary>
    /// Adds a dummy node to the list, a standard ResNode with no contents for spacing/positioning.
    /// </summary>
    /// <param name="size">The size of the dummy to add.</param>
    public void AddDummy(float size = 0.0f)
    {
        var dummyNode = new ResNode
        {
            Size = new Vector2(size, size),
        };

        this.AddNode(dummyNode);
    }

    /// <summary>
    /// Removes all nodes from the list. All nodes are disposed.
    /// </summary>
    public virtual void Clear()
    {
        this.suppressRecalculateLayout = true;
        try
        {
            foreach (var node in this.NodeList.ToList())
            {
                this.RemoveNode(node);
            }
        }
        finally
        {
            this.suppressRecalculateLayout = false;
        }

        this.RecalculateLayout();
    }

    /// <summary>
    /// Sorts the contained nodes using the provided comparison.
    /// </summary>
    /// <param name="comparison">Comparison method.</param>
    public void ReorderNodes(Comparison<NodeBase> comparison)
    {
        this.NodeList.Sort(comparison);
        this.RecalculateLayout();
    }

    /// <summary>
    /// Function that is invoked when this node needs to rebuild its layout.
    /// </summary>
    protected abstract void OnRecalculateLayout();

    /// <summary>
    /// Adjusts the nodes properties when <see cref="RecalculateLayout"/> is invoked.
    /// </summary>
    /// <param name="node">Node to adjust.</param>
    protected virtual void AdjustNode(NodeBase node)
    {
    }
}
