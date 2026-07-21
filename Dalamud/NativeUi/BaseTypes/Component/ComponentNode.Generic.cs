using Dalamud.NativeUi.Nodes;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Component;

/// <summary>
/// Generic Implementation of the games ComponentNode as a base class for use in KTK.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <typeparam name="TU">The component uld data type.</typeparam>
internal abstract unsafe class ComponentNode<T, TU> : ComponentNode where T : unmanaged, ICreatable<T> where TU : unmanaged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentNode{T, TU}"/> class.
    /// </summary>
    protected ComponentNode()
        : base(NodeType.Component)
    {
        Node->Component = (AtkComponentBase*)IMemorySpace.GetUISpace()->Create<T>();
        Node->Component->UldManager.ComponentData = (AtkUldComponentDataBase*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldComponentDataBase), 8);

        this.RegisterVirtualTable();

        this.ComponentBase->Initialize();

        this.CollisionNode = new CollisionNode
        {
            NodeId = 1,
            LinkedComponent = this.ComponentBase,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.HasCollision |
                        NodeFlags.RespondToMouse | NodeFlags.Focusable | NodeFlags.EmitsEvents | NodeFlags.Fill,
        };

        this.FocusNode = this.CollisionNode;

        this.CollisionNode.ResNode->ParentNode = this.ResNode;
        this.CollisionNode.ParentUldManager = &((AtkComponentBase*)this.Component)->UldManager;

        this.ChildNodes.Add(this.CollisionNode);

        this.ComponentBase->OwnerNode = this.Node;
        this.ComponentBase->ComponentFlags = 1;

        ref var uldManager = ref this.ComponentBase->UldManager;

        uldManager.Objects = (AtkUldObjectInfo*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldObjectInfo), 8);
        ref var objects = ref uldManager.Objects;
        uldManager.ObjectCount = 1;

        this.SetInternalComponentType(ComponentType.Base);

        objects->NodeList = (AtkResNode**)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkResNode*), 8);
        objects->NodeList[0] = this.CollisionNode;
        objects->NodeCount = 1;
        objects->Id = 1000;

        uldManager.InitializeResourceRendererManager();
        uldManager.RootNode = this.CollisionNode;

        uldManager.UpdateDrawNodeList();
        uldManager.ResourceFlags = AtkUldManagerResourceFlag.Initialized | AtkUldManagerResourceFlag.ArraysAllocated;
        uldManager.LoadedState = AtkLoadState.Loaded;

        this.AddNodeFlags(NodeFlags.EmitsEvents);
    }

    /// <inheritdoc/>>
    public sealed override CollisionNode CollisionNode { get; }

    /// <inheritdoc/>>
    public sealed override AtkComponentBase* ComponentBase
        => this.Node->Component;

    /// <summary>
    /// Gets the typed component.
    /// </summary>
    public T* Component
        => (T*)this.ComponentBase;

    /// <inheritdoc/>>
    public sealed override AtkUldComponentDataBase* DataBase
        => Node->Component->UldManager.ComponentData;

    /// <summary>
    /// Gets the typed uld data.
    /// </summary>
    public TU* Data => (TU*)this.DataBase;

    /// <summary>
    /// Gets or sets a value indicating whether the component is in an enabled state. Default is enabled.
    /// </summary>
    public virtual bool IsEnabled
    {
        get => this.NodeFlags.HasFlag(NodeFlags.Enabled);
        set
        {
            if (this.IsEnabled != value)
            {
                this.ComponentBase->SetEnabledState(value);
            }
        }
    }

    /// <summary>
    /// Implicit conversion to AtkEventListener for seamless game interop.
    /// </summary>
    /// <param name="node">Node to expose the AtkListener for.</param>
    public static implicit operator AtkEventListener*(ComponentNode<T, TU> node)
        => &node.ComponentBase->AtkEventListener;

    /// <summary>
    /// Implicit conversion to the components type for seamless game interop.
    /// </summary>
    /// <param name="node">Node to expose the component of.</param>
    public static implicit operator T*(ComponentNode<T, TU> node)
        => node.Component;

    /// <summary>
    /// Implicit conversion to the components uld data type for seamless game interop.
    /// </summary>
    /// <param name="node">Node to expose the data of.</param>
    public static implicit operator TU*(ComponentNode<T, TU> node)
        => node.Data;

    /// <summary>
    /// Sets this node as focused using the <see cref="ComponentNode.FocusNode"/> property.
    /// </summary>
    public void SetFocus()
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByNode(this);
        if (addon is null) return;

        AtkStage.Instance()->AtkInputManager->SetFocus(this.FocusNode, addon, 0);
    }

    /// <summary>
    /// Sets the AtkUldComponent's internal type.
    /// </summary>
    /// <param name="type">The actual type for this component.</param>
    protected void SetInternalComponentType(ComponentType type)
    {
        var componentInfo = (AtkUldComponentInfo*)this.ComponentBase->UldManager.Objects;

        componentInfo->ComponentType = type;
    }

    /// <summary>
    /// Performs post-construction initialization of components based on their actual created type.
    /// </summary>
    /// <remarks>
    /// The game does a bunch of its own magic here to wire things up for us.
    /// </remarks>
    protected void InitializeComponentEvents()
    {
        this.ComponentBase->InitializeFromComponentData(this.DataBase);
        this.ComponentBase->Setup();
        this.ComponentBase->SetEnabledState(true);
    }

    /// <inheritdoc />
    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        this.CollisionNode.Size = this.Size;
        this.ComponentBase->UldManager.RootNodeHeight = (ushort)this.Height;
        this.ComponentBase->UldManager.RootNodeWidth = (ushort)this.Width;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (disposing && !this.IsDisposed)
        {
            try
            {
                if (!isNativeDestructor && this.Node is not null && Node->Component is not null)
                {
                    Node->Component->Deinitialize();
                    Node->Component->Dtor(1);
                    Node->Component = null;
                }
            }
            catch (Exception e)
            {
                this.Log.Error(e, "Exception occured during ComponentNode dispose.");
            }
            finally
            {
                base.Dispose(disposing, isNativeDestructor);
            }
        }
    }
}
