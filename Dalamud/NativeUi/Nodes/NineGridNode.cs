using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Classes;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games NineGridNode.
/// </summary>
internal unsafe class NineGridNode : NodeBase<AtkNineGridNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NineGridNode"/> class.
    /// </summary>
    public NineGridNode()
        : base(NodeType.NineGrid)
    {
        this.PartsList = new PartsList();

        Node->PartsList = this.PartsList.InternalPartsList;
    }

    /// <summary>
    /// Gets the parts list used for this node.
    /// </summary>
    public PartsList PartsList { get; }

    /// <summary>
    /// Sets the collection of parts to use for this node.
    /// </summary>
    public ICollection<Part> Parts
    {
        set => this.PartsList.Add(value.ToArray());
    }

    /// <summary>
    /// Gets or sets the PartId.
    /// </summary>
    public uint PartId
    {
        get => Node->PartId;
        set => Node->PartId = value;
    }

    /// <summary>
    /// Gets or sets the texture offsets used for fitting the ninegrid texture.
    /// </summary>
    public Vector4 Offsets
    {
        get => new(Node->TopOffset, Node->BottomOffset, Node->LeftOffset, Node->RightOffset);
        set
        {
            Node->TopOffset = (short)value.X;
            Node->BottomOffset = (short)value.Y;
            Node->LeftOffset = (short)value.Z;
            Node->RightOffset = (short)value.W;
        }
    }

    /// <summary>
    /// Gets or sets the top offset for the texture.
    /// </summary>
    public float TopOffset
    {
        get => Node->TopOffset;
        set => Node->TopOffset = (short)value;
    }

    /// <summary>
    /// Gets or sets the bottom offset for the texture.
    /// </summary>
    public float BottomOffset
    {
        get => Node->BottomOffset;
        set => Node->BottomOffset = (short)value;
    }

    /// <summary>
    /// Gets or sets the left offset for the texture.
    /// </summary>
    public float LeftOffset
    {
        get => Node->LeftOffset;
        set => Node->LeftOffset = (short)value;
    }

    /// <summary>
    /// Gets or sets the right offset for the texture.
    /// </summary>
    public float RightOffset
    {
        get => Node->RightOffset;
        set => Node->RightOffset = (short)value;
    }

    /// <summary>
    /// Gets or sets the blend mode.
    /// </summary>
    public uint BlendMode
    {
        get => Node->BlendMode;
        set => Node->BlendMode = value;
    }

    /// <summary>
    /// Gets or sets the parts render type.
    /// </summary>
    public byte PartsRenderType
    {
        get => Node->PartsTypeRenderType;
        set => Node->PartsTypeRenderType = value;
    }

    /// <summary>
    /// Adds a single part.
    /// </summary>
    /// <param name="part">Part to add.</param>
    public void AddPart(Part part)
        => this.PartsList.Add(part);

    /// <summary>
    /// Adds multiple parts.
    /// </summary>
    /// <param name="parts">Parts to add.</param>
    public void AddPart(params Part[] parts)
        => this.PartsList.Add(parts);

    /// <inheritdoc />
    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (disposing && !this.IsDisposed)
        {
            if (!isNativeDestructor)
            {
                this.PartsList.Dispose();
                Node->PartsList = null;
            }

            base.Dispose(disposing, isNativeDestructor);
        }
    }
}
