using Dalamud.Logging.Internal;
using Dalamud.NativeUi.BaseTypes.Node;
using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Component;

/// <summary>
/// Abstract implementation of the base class for all component nodes use in KamiToolKit.
/// </summary>
internal abstract unsafe partial class ComponentNode(NodeType nodeType) : NodeBase<AtkComponentNode>(nodeType)
{
    /// <summary>
    /// Gets the collision node used for this component node.
    /// </summary>
    public abstract CollisionNode CollisionNode { get; }

    /// <summary>
    /// Gets the base typed component pointer.
    /// </summary>
    public abstract AtkComponentBase* ComponentBase { get; }

    /// <summary>
    /// Gets the base typed uld data pointer.
    /// </summary>
    public abstract AtkUldComponentDataBase* DataBase { get; }

    /// <summary>
    /// Gets the cursor navigation info from the component.
    /// </summary>
    public ref AtkCursorNavigationInfo CursorNavInfo
        => ref this.ComponentBase->CursorNavigationInfo;

    /// <summary>
    /// Gets or sets the index of this nodes controller nav.
    /// </summary>
    public int NavIndex
    {
        get => this.CursorNavInfo.Index;
        set => this.CursorNavInfo.Index = (byte)value;
    }

    /// <summary>
    /// Gets or sets the index of the left direction nav.
    /// </summary>
    public int NavLeft
    {
        get => this.CursorNavInfo.LeftIndex;
        set => this.CursorNavInfo.LeftIndex = (byte)value;
    }

    /// <summary>
    /// Gets or sets the index of the right direction nav.
    /// </summary>
    public int NavRight
    {
        get => this.CursorNavInfo.RightIndex;
        set => this.CursorNavInfo.RightIndex = (byte)value;
    }

    /// <summary>
    /// Gets or sets the index of the up direction nav.
    /// </summary>
    public int NavUp
    {
        get => this.CursorNavInfo.UpIndex;
        set => this.CursorNavInfo.UpIndex = (byte)value;
    }

    /// <summary>
    /// Gets or sets the index of the down direction nav.
    /// </summary>
    public int NavDown
    {
        get => this.CursorNavInfo.DownIndex;
        set => this.CursorNavInfo.DownIndex = (byte)value;
    }

    /// <summary>
    /// Gets or sets the node used when focusing this element via controller.
    /// </summary>
    public NodeBase FocusNode
    {
        get => field ?? this.CollisionNode;
        protected set;
    }

    /// <summary>
    /// Gets logger to log component node events.
    /// </summary>
    protected override ModuleLog Log { get; } = new("ComponentNode");
}
