using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug.Browsing;

/// <summary>
/// A tree for an <see cref="AtkClippingMaskNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe class ClippingMaskNodeTree : ImageNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClippingMaskNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal ClippingMaskNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    /// <inheritdoc/>
    private protected override uint PartId => this.CmNode->PartId;

    /// <inheritdoc/>
    private protected override AtkUldPartsList* PartsList => this.CmNode->PartsList;

    private AtkClippingMaskNode* CmNode => (AtkClippingMaskNode*)this.Node;

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct(this.CmNode);

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false) => this.DrawTextureAndParts();
}
