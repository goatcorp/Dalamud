using Dalamud.NativeUi.BaseTypes.Component;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.Nodes;

/// <summary>
/// A simplified implementation of a <see cref="ComponentNode{T,TU}"/>.
/// </summary>
/// <remarks>
/// This node should only be used to implement custom dynamic interactable elements.
/// You should <b>not</b> use this node for any kind of structural lay-outing.
/// </remarks>
internal class SimpleComponentNode : ComponentNode<AtkComponentBase, AtkUldComponentDataBase>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleComponentNode"/> class.
    /// </summary>
    public SimpleComponentNode()
        => this.InitializeComponentEvents();

    /// <inheritdoc/>
    public override ReadOnlySeString TextTooltip
    {
        get => this.CollisionNode.TextTooltip;
        set => this.CollisionNode.TextTooltip = value;
    }
}
