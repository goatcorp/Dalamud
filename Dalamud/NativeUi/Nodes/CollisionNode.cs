using Dalamud.NativeUi.BaseTypes.Node;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games CollisionNode.
/// </summary>
internal unsafe class CollisionNode() : NodeBase<AtkCollisionNode>(NodeType.Collision)
{
    /// <summary>
    /// Gets or sets the collision type.
    /// </summary>
    public CollisionType CollisionType
    {
        get => Node->CollisionType;
        set => Node->CollisionType = value;
    }

    /// <summary>
    /// Gets or sets the uses.
    /// </summary>
    /// <remarks>
    /// Unknown what this is actually doing.
    /// </remarks>
    public uint Uses
    {
        get => Node->Uses;
        set => Node->Uses = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the linked component.
    /// </summary>
    public AtkComponentBase* LinkedComponent
    {
        get => Node->LinkedComponent;
        set => Node->LinkedComponent = value;
    }
}
