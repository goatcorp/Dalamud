using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Dalamud.NativeUi.BaseTypes.Addon;
using Dalamud.NativeUi.BaseTypes.Component;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;
using Dalamud.NativeUi.Extensions;
using Dalamud.NativeUi.Nodes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// Double Docs Because Partial... (Linking).
/// </summary>
internal abstract unsafe partial class NodeBase
{
    private NodeBase? parentNode;

    /// <summary>
    /// Gets the list of the nodes managed children nodes.
    /// </summary>
    internal List<NodeBase> ChildNodes { get; } = [];

    /// <summary>
    /// Gets or sets the AtkUldManager that is managing this node.
    /// </summary>
    /// <remarks>
    /// This can be null.
    /// </remarks>
    internal AtkUldManager* ParentUldManager { get; set; }

    /// <summary>
    /// Gets the AtkUnitBase that currently owns this node.
    /// </summary>
    /// <remarks>
    /// This can be null.
    /// </remarks>
    internal AtkUnitBase* ParentAddon { get; private set; }

    /// <summary>
    /// Attaches this node to targetAddon's root node using targetPosition as the relative positioning.
    /// </summary>
    /// <param name="targetAddon">The target addon to attach to.</param>
    /// <param name="targetPosition">The nodes relative position to the targets RootNode.</param>
    [OverloadResolutionPriority(1)]
    public void AttachNode(NativeAddon? targetAddon, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformManagedAttach(targetAddon, targetPosition);

    /// <inheritdoc cref="AttachNode(NativeAddon?, NodePosition)"/>
    public void AttachNode(AtkUnitBase* targetAddon, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach(targetAddon is not null ? targetAddon->RootNode : null, targetPosition);

    /// <summary>
    /// Attaches this node to the targetNode node using targetPosition to determine where to insert the node relatively.
    /// </summary>
    /// <param name="targetNode">The target node to attach to.</param>
    /// <param name="targetPosition">The attachment point relative position to the target node.</param>
    [OverloadResolutionPriority(1)]
    public void AttachNode(NodeBase? targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformManagedAttach(targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkResNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach(targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkImageNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkTextNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkNineGridNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkCounterNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkCollisionNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkClippingMaskNode* targetNode, NodePosition targetPosition = NodePosition.AsLastChild)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <inheritdoc cref="AttachNode(NodeBase?, NodePosition)"/>
    public void AttachNode(AtkComponentNode* targetNode, NodePosition targetPosition = NodePosition.AfterAllSiblings)
        => this.PerformNativeAttach((AtkResNode*)targetNode, targetPosition);

    /// <summary>
    /// Detaches this node from the current tree, and removes native references to it.
    /// This is only intended to be used for very specific use cases.
    /// Generally speaking you probably want "Dispose" instead.
    /// </summary>
    /// <remarks>
    /// <em>Do not call this immediately before calling dispose!</em>
    /// </remarks>
    public void DetachNode()
    {
        ThreadSafety.AssertMainThread();
        if (this.ResNode is null) return;

        this.UnlinkFromNative();
        this.RemoveUldManagerObjectReferences();
        this.RemoveParentAddonReferences();
        this.RemoveParentNodeReferences();
    }

    /// <summary>
    /// Gets all children that are descendents and controlled by this node.
    /// This will not get any children that are inside a child component node,
    /// as that node is responsible for its own children.
    /// </summary>
    /// <param name="parent">Parent to iterate.</param>
    /// <returns>Enumerable containing all child nodes.</returns>
    internal static IEnumerable<NodeBase> GetLocalChildren(NodeBase parent)
    {
        if (parent is ComponentNode) yield break;

        foreach (var child in parent.ChildNodes)
        {
            yield return child;

            if (child is ComponentNode) continue;
            foreach (var childNode in GetLocalChildren(child))
            {
                yield return childNode;
            }
        }
    }

    private static IEnumerable<NodeBase> GetAllChildren(NodeBase parent)
    {
        foreach (var child in parent.ChildNodes)
        {
            yield return child;
            foreach (var childNode in GetAllChildren(child))
            {
                yield return childNode;
            }
        }
    }

    private void PerformManagedAttach(NativeAddon? targetAddon, NodePosition targetPosition = NodePosition.AsLastChild)
    {
        ThreadSafety.AssertMainThread();
        if (targetAddon is null) return;

        this.PerformNativeAttach(targetAddon.RootNode, targetPosition);

        this.parentNode = targetAddon.RootNode;
        this.parentNode.ChildNodes.Add(this);
    }

    private void PerformManagedAttach(NodeBase? targetNode, NodePosition targetPosition)
    {
        if (this == targetNode)
        {
            this.Log.Warning("Attempted to attach self to self, attach was aborted.");
            return;
        }

        ThreadSafety.AssertMainThread();
        if (targetNode is null) return;

        this.PerformNativeAttach(targetNode, targetPosition);

        this.parentNode = targetNode;
        this.parentNode.ChildNodes.Add(this);
    }

    private void PerformNativeAttach(AtkResNode* targetNode, NodePosition targetPosition)
    {
        if (this.ResNode == targetNode)
        {
            this.Log.Warning("Attempted to attach self to self, attach was aborted.");
            return;
        }

        ThreadSafety.AssertMainThread();
        if (targetNode is null) return;

        if (targetNode->GetNodeType() is NodeType.Component)
        {
            // If target is a ComponentNode,
            // then we don't ever wanna be a child of the ComponentNode itself,
            // we will want to be a sibling of the root node.
            // Therefore, redirect the target position to be siblings.
            targetPosition = targetPosition switch {
                NodePosition.AsLastChild => NodePosition.AfterAllSiblings,
                NodePosition.AsFirstChild => NodePosition.BeforeAllSiblings,
                _ => targetPosition,
            };

            // If however, we are using BeforeTarget or AfterTarget,
            // then we do want to attach to the ComponentNode
            // else, attach to its root node.
            var componentNode = targetNode->GetAsAtkComponentNode();
            if (componentNode is not null)
            {
                targetNode = targetPosition switch {
                    NodePosition.AfterTarget => targetNode,
                    NodePosition.BeforeTarget => targetNode,
                    NodePosition.AfterAllSiblings => componentNode->Component->UldManager.RootNode,
                    NodePosition.BeforeAllSiblings => componentNode->Component->UldManager.RootNode,
                    _ => throw new ArgumentOutOfRangeException(nameof(targetPosition), targetPosition, null),
                };
            }
        }

        NodeLinker.AttachNode(this, targetNode, targetPosition);
        this.UpdateParentAddon(targetNode);
        this.UpdateNative();
    }

    private void UnlinkFromNative()
    {
        NodeLinker.DetachNode(this.ResNode);
        ResNode->ParentNode = null;
        ResNode->NextSiblingNode = null;
        ResNode->PrevSiblingNode = null;
    }

    private void RemoveUldManagerObjectReferences()
    {
        // If UldManager is null, try again to get the UldManager.
        if (this.ParentUldManager is null)
        {
            this.ParentUldManager = this.GetUldManagerForNode(this);
        }

        // If we still can't get it, it doesn't exist.
        if (this.ParentUldManager is null) return;

        // Remove this node and all children from the UldManager's Objects List
        ParentUldManager->RemoveNodeFromObjectList(this);
        this.ParentUldManager = null;
    }

    private void RemoveParentAddonReferences()
    {
        // If ParentAddon is null, try again to get it from RaptureAtkUnitManager
        if (this.ParentAddon is null)
        {
            this.ParentAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(this);
        }

        // If it's still null, then it doesn't exist.
        if (this.ParentAddon is null)
        {
            // Ensure the children also know that they have no parents.
            foreach (var child in GetAllChildren(this))
            {
                child.ParentAddon = null;
            }

            return;
        }

        ParentAddon->UldManager.UpdateDrawNodeList();
        ParentAddon->UpdateCollisionNodeList(false);

        this.ParentAddon = null;

        foreach (var child in GetAllChildren(this))
        {
            child.ParentAddon = null;
        }
    }

    private void RemoveParentNodeReferences()
    {
        if (this.parentNode is null) return;

        this.parentNode.ChildNodes.Remove(this);
        this.parentNode = null;
    }

    private void UpdateNative()
    {
        if (this.ResNode is null) return;

        // Set this node and all children to dirty to have the
        // game recalc their visible location.
        this.MarkDirty();

        if (this.ParentUldManager is null)
        {
            this.ParentUldManager = this.GetUldManagerForNode(this);
        }

        if (this.ParentUldManager is not null)
        {
            ParentUldManager->AddNodeToObjectList(this);

            foreach (var child in GetAllChildren(this))
            {
                child.ParentUldManager = this.ParentUldManager;
            }

            if (this is TextNode { TextId: not 0 })
            {
                ParentUldManager->SetupText();
            }
        }

        if (this.ParentAddon is not null)
        {
            ParentAddon->UldManager.UpdateDrawNodeList();
            ParentAddon->UpdateCollisionNodeList(false);
        }
    }

    private void UpdateParentAddon(AtkResNode* node)
    {
        if (this.parentNode is not null && this.parentNode.ParentAddon is not null)
        {
            this.ParentAddon = this.parentNode.ParentAddon;
        }
        else if (this.ParentAddon is null)
        {
            var targetParentAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
            if (targetParentAddon is not null)
            {
                this.ParentAddon = targetParentAddon;
            }
        }

        if (this.ParentAddon is not null)
        {
            foreach (var child in GetAllChildren(this))
            {
                child.ParentAddon = this.ParentAddon;
            }
        }
    }

    private AtkUldManager* GetUldManagerForNode(AtkResNode* node)
    {
        if (node is null) return null;

        var targetNode = node;

        if (targetNode->GetNodeType() is NodeType.Component)
        {
            targetNode = targetNode->ParentNode;
        }

        // Try to get UldManager via the first parent that is a component
        while (targetNode is not null)
        {
            if (targetNode->GetNodeType() is NodeType.Component)
            {
                var componentNode = (AtkComponentNode*)targetNode;
                return &componentNode->Component->UldManager;
            }

            targetNode = targetNode->ParentNode;
        }

        // We failed to find a parent component, try to get a parent addon instead
        if (this.ParentAddon is null)
        {
            this.ParentAddon = RaptureAtkUnitManager.Instance()->GetAddonByNode(node);
        }

        if (this.ParentAddon is not null)
        {
            return &ParentAddon->UldManager;
        }

        return null;
    }
}
