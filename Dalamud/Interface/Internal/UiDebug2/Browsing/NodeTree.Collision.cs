using FFXIVClientStructs.FFXIV.Component.GUI;

using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkCollisionNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe class CollisionNodeTree : ResNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollisionNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal CollisionNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct((AtkCollisionNode*)this.Node);
}
