using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

using static Dalamud.Interface.Internal.UiDebug2.Utility.Gui;
using static Dalamud.Utility.Util;

namespace Dalamud.Interface.Internal.UiDebug2.Browsing;

/// <summary>
/// A tree for an <see cref="AtkCounterNode"/> that can be printed and browsed via ImGui.
/// </summary>
internal unsafe partial class CounterNodeTree : ResNodeTree
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CounterNodeTree"/> class.
    /// </summary>
    /// <param name="node">The node to create a tree for.</param>
    /// <param name="addonTree">The tree representing the containing addon.</param>
    internal CounterNodeTree(AtkResNode* node, AddonTree addonTree)
        : base(node, addonTree)
    {
    }

    private AtkCounterNode* CntNode => (AtkCounterNode*)this.Node;

    /// <inheritdoc/>
    private protected override void PrintNodeObject() => ShowStruct(this.CntNode);

    /// <inheritdoc/>
    private protected override void PrintFieldsForNodeType(bool isEditorOpen = false)
    {
        if (!isEditorOpen)
        {
            PrintFieldValuePairs(("Text", new ReadOnlySeStringSpan(((AtkCounterNode*)this.Node)->NodeText.AsSpan()).ToMacroString()));
        }
    }
}
