using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Helper class for linking nodes to other nodes using <see cref="NodePosition"/> relative positioning.
/// </summary>
internal static unsafe class NodeLinker
{
    /// <summary>
    /// Detaches the provided node from <b>all</b> links, forwards, backwards, up, down, left, right, b, a, select, start.
    /// </summary>
    /// <param name="node">The node to detach from all links.</param>
    public static void DetachNode(AtkResNode* node)
    {
        if (node is null) return;

        var parentNode = node->ParentNode;

        if (parentNode != null && parentNode->ChildNode == node)
        {
            parentNode->ChildNode = node->PrevSiblingNode != null
                                        ? node->PrevSiblingNode
                                        : node->NextSiblingNode;

            if (parentNode->GetNodeType() is not NodeType.Component && parentNode->ChildCount > 0)
            {
                parentNode->ChildCount--;
            }
        }

        if (node->PrevSiblingNode != null)
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;

        if (node->NextSiblingNode != null)
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
    }

    /// <summary>
    /// Attaches a native ui node to another native ui node, using position as a relative positioning.
    /// </summary>
    /// <param name="node">The node you want to add to the target.</param>
    /// <param name="attachTargetNode">The target node that will be attached to.</param>
    /// <param name="position">The relative position to attach the new node to.</param>
    /// <exception cref="ArgumentOutOfRangeException">Throws when NodePosition provided is invalid.</exception>
    internal static void AttachNode(AtkResNode* node, AtkResNode* attachTargetNode, NodePosition position)
    {
        switch (position)
        {
            case NodePosition.BeforeTarget:
                EmplaceBefore(node, attachTargetNode);
                break;

            case NodePosition.AfterTarget:
                EmplaceAfter(node, attachTargetNode);
                break;

            case NodePosition.BeforeAllSiblings:
                EmplaceBeforeSiblings(node, attachTargetNode);
                break;

            case NodePosition.AfterAllSiblings:
                EmplaceAfterSiblings(node, attachTargetNode);
                break;

            case NodePosition.AsLastChild:
                EmplaceAsLastChild(node, attachTargetNode);
                break;

            case NodePosition.AsFirstChild:
                EmplaceAsFirstChild(node, attachTargetNode);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(position), position, null);
        }
    }

    private static void EmplaceBefore(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        node->ParentNode = attachTargetNode->ParentNode;

        // Target node is the head of the nodelist, we will be the new head.
        if (attachTargetNode->NextSiblingNode is null)
        {
            attachTargetNode->ParentNode->ChildNode = node;
        }

        // We have a node that will be before us
        if (attachTargetNode->NextSiblingNode is not null)
        {
            attachTargetNode->NextSiblingNode->PrevSiblingNode = node;
            node->NextSiblingNode = attachTargetNode->NextSiblingNode;
        }

        attachTargetNode->NextSiblingNode = node;
        node->PrevSiblingNode = attachTargetNode;

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component)
        {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceAfter(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        node->ParentNode = attachTargetNode->ParentNode;

        // We have a node that will be after us
        if (attachTargetNode->PrevSiblingNode is not null)
        {
            attachTargetNode->PrevSiblingNode->NextSiblingNode = node;
            node->PrevSiblingNode = attachTargetNode->PrevSiblingNode;
        }

        attachTargetNode->PrevSiblingNode = node;
        node->NextSiblingNode = attachTargetNode;

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component)
        {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceBeforeSiblings(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        var current = attachTargetNode;
        var previous = current;

        while (current is not null)
        {
            previous = current;
            current = current->NextSiblingNode;
        }

        if (previous is not null)
        {
            EmplaceBefore(node, previous);
        }

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component)
        {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceAfterSiblings(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        var current = attachTargetNode;
        var previous = current;

        while (current is not null)
        {
            previous = current;
            current = current->PrevSiblingNode;
        }

        if (previous is not null)
        {
            EmplaceAfter(node, previous);
        }

        if (attachTargetNode->ParentNode->GetNodeType() is not NodeType.Component)
        {
            attachTargetNode->ParentNode->ChildCount++;
        }
    }

    private static void EmplaceAsLastChild(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        // If the child list is empty
        if (attachTargetNode->ChildNode is null && attachTargetNode->GetNodeType() is not NodeType.Component)
        {
            if (attachTargetNode->GetNodeType() is not NodeType.Component)
            {
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else
            {
                node->ParentNode = attachTargetNode;
            }
        }
        else // Else Add to the List
        {
            var currentNode = attachTargetNode->ChildNode;
            while (currentNode is not null && currentNode->PrevSiblingNode != null)
            {
                currentNode = currentNode->PrevSiblingNode;
            }

            node->ParentNode = attachTargetNode;
            node->NextSiblingNode = currentNode;

            if (currentNode is not null)
            {
                currentNode->PrevSiblingNode = node;
            }

            if (attachTargetNode->GetNodeType() is not NodeType.Component)
            {
                attachTargetNode->ChildCount++;
            }
        }
    }

    private static void EmplaceAsFirstChild(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        // If the child list is empty
        if (attachTargetNode->ChildNode is null && attachTargetNode->ChildCount is 0)
        {
            if (attachTargetNode->GetNodeType() is not NodeType.Component)
            {
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else
            {
                node->ParentNode = attachTargetNode;
            }
        }
        else // Else Add to the List as the First Child
        {
            if (attachTargetNode->GetNodeType() is not NodeType.Component)
            {
                attachTargetNode->ChildNode->NextSiblingNode = node;
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else
            {
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                node->ParentNode = attachTargetNode;
            }
        }
    }
}
