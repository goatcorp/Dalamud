using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Classes;
using Dalamud.NativeUi.Enums;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// Implementation of the games ImageNode.
/// </summary>
internal unsafe class ImageNode : NodeBase<AtkImageNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImageNode"/> class.
    /// </summary>
    public ImageNode()
        : base(NodeType.Image)
    {
        this.PartsList = new PartsList();

        Node->PartsList = this.PartsList.InternalPartsList;
    }

    /// <summary>
    /// Gets the parts list for this node.
    /// </summary>
    public PartsList PartsList { get; }

    /// <summary>
    /// Gets or sets the PartId.
    /// </summary>
    public uint PartId
    {
        get => Node->PartId;
        set => Node->PartId = (ushort)value;
    }

    /// <summary>
    /// Gets or sets the wrap mode for the displayed image.
    /// </summary>
    public WrapMode WrapMode
    {
        get => (WrapMode)Node->WrapMode;
        set => Node->WrapMode = (byte)value;
    }

    /// <summary>
    /// Gets or sets the image node flags.
    /// </summary>
    public ImageNodeFlags ImageNodeFlags
    {
        get => Node->Flags;
        set => Node->Flags = value;
    }

    /// <summary>
    /// Sets a value indicating whether the texture should be auto-fit to the node.
    /// </summary>
    /// <remarks>
    /// Sets AutoFit ImageNodeFlag and Stretch WrapMode.
    /// </remarks>
    public bool FitTexture
    {
        set
        {
            if (value)
            {
                this.ImageNodeFlags = ImageNodeFlags.AutoFit;
                this.WrapMode = WrapMode.Stretch;
            }
        }
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
