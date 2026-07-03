using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Game;
using Dalamud.Logging.Internal;
using Dalamud.NativeUi.Extensions;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// Abstract base class for all native ui nodes.
/// </summary>
internal abstract unsafe partial class NodeBase : IDisposable
{
    /// <summary>
    /// Base id used for any custom nodes, all allocated nodes will have an id equal to or greater than this value.
    /// </summary>
    internal const uint NodeIdBase = 100_000_000;

    /// <summary>
    /// Pinned AtkResNode.Destroy function for use in the replaced virtual table.
    /// </summary>
    private AtkResNode.Delegates.Destroy destroyFunction = null!;

    /// <summary>
    /// Pointer to the original virtual table for this node.
    /// </summary>
    private AtkResNode.AtkResNodeVirtualTable* originalVirtualTable;

    /// <summary>
    /// Pointer to the modified virtual table for this node.
    /// </summary>
    private AtkResNode.AtkResNodeVirtualTable* modifiedVirtualTable;

    /// <summary>
    /// Finalizes an instance of the <see cref="NodeBase"/> class.
    /// Finalizer invocation from GC, this should only happen when a node is created with new but not attached to native.
    /// </summary>
    ~NodeBase()
    {
        this.Log.Warning("Finalizer has disposed node {nodeType}, node was allocated but not attached to native.", this.GetType());
        this.Dispose(false, false);
    }

    /// <summary>
    /// Gets or sets the current node id offset, this is a positive incrementing value, and never gets decremented.
    /// </summary>
    internal static uint CurrentOffset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node is a addon root node.
    /// Used for leak detection, as addon root nodes are indirectly owned by an addon,
    /// and their attach state can't be tracked the same way.
    /// </summary>
    internal bool IsAddonRootNode { get; set; }

    /// <summary>
    /// Gets the nodes memory address as a AtkResNode pointer.
    /// This is base type of the contained node, all nodes can be represented as this base type.
    /// </summary>
    internal abstract AtkResNode* ResNode { get; }

    /// <summary>
    /// Gets module log instance for NodeBase.
    /// </summary>
    /// <remarks>
    /// This is virtual so that ComponentNode can override it for more accurate logging.
    /// </remarks>
    protected virtual ModuleLog Log { get; } = new("NodeBase");

    /// <summary>
    /// Gets a value indicating whether this instance has already been disposed.
    /// </summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Implicit operator to convert this instance to a AtkResNode* for cleaner interop.
    /// </summary>
    /// <param name="node">The node instance to extract the AtkResNode* from.</param>
    public static implicit operator AtkResNode*(NodeBase node) => node.ResNode;

    /// <summary>
    /// Implicit operator to convert this instance to a AtkEventTarget* for cleaner interop.
    /// </summary>
    /// <param name="node">The node instance to extract the AtkEventTarget* from.</param>
    public static implicit operator AtkEventTarget*(NodeBase node) => &node.ResNode->AtkEventTarget;

    /// <summary>
    /// Disposes this instance. Has double dispose guards.
    /// </summary>
    /// <remarks>
    /// Must be invoked from the main game thread.
    /// </remarks>
    public void Dispose()
    {
        try
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (Service<Framework>.Get().IsFrameworkUnloading)
            {
                return;
            }

            if (!ThreadSafety.IsMainThread)
            {
                return;
            }

            this.IsDisposed = true;

            foreach (var child in this.ChildNodes.ToList())
            {
                child.Dispose();
            }

            this.ChildNodes.Clear();

            this.UnregisterTooltipEvents();

            AtkStage.Instance()->ClearNodeFocus(this.ResNode);

            this.DetachNode();

            this.Timeline?.Dispose();
            this.ResNode->Timeline = null;

            this.Dispose(true, false);
            GC.SuppressFinalize(this);
        }
        catch (Exception e)
        {
            this.Log.Error(e, "Exception occurred while disposing node {GetType} with address [{ResNode:X8}].", this.GetType().Name, $"0x{(nint)this.ResNode:X8}");
        }
    }

    /// <summary>
    /// Dispose associated resources. If a resource modifies native state directly guard it with isNativeDestructor.
    /// </summary>
    /// <param name="disposing">
    /// Indicates if this specific call should dispose resources or not. This protects against double dispose,
    /// or incorrectly manipulating native state too many times.
    /// </param>
    /// <param name="isNativeDestructor">
    /// Indicates if the dispose call should try to completely clean up all resources,
    /// or if it should only clean up managed resources. When false, be sure to only dispose
    /// resources that exist in managed spaces, as the game has already cleaned up everything else.
    /// </param>
    protected virtual void Dispose(bool disposing, bool isNativeDestructor)
    {
        // Dispose of managed resources that must be disposed regardless of how dispose is invoked
        this.DisposeEvents();
    }

    /// <summary>
    /// Replaces the nodes entire virtual table to ensure that C#'s managed space gets notified of the games unmanaged node dtor.
    /// </summary>
    protected void BuildVirtualTable()
    {
        // Back up original destructor pointer
        this.originalVirtualTable = this.ResNode->VirtualTable;

        // Overwrite virtual table with a custom copy,
        // Note: Currently there are only 2 virtual functions, but there's no harm in copying more for if they ever add more virtual functions to the game.
        this.modifiedVirtualTable = (AtkResNode.AtkResNodeVirtualTable*)IMemorySpace.GetUISpace()->Malloc(0x8 * 4, 8);
        NativeMemory.Copy(this.ResNode->VirtualTable, this.modifiedVirtualTable, 0x8 * 4);
        this.ResNode->VirtualTable = this.modifiedVirtualTable;

        // Pin managed function to virtual table entry
        this.destroyFunction = this.Destroy;

        // Replace native destructor with
        this.modifiedVirtualTable->Destroy = (delegate* unmanaged<AtkResNode*, bool, void>)Marshal.GetFunctionPointerForDelegate(this.destroyFunction);
    }

    /// <summary>
    /// Pinned managed function that is used to replace the native virtual tables dtor function pointer.
    /// </summary>
    /// <param name="thisPtr">The pointer to the node instance.</param>
    /// <param name="free">Free flags, these are provided by the game, generally expect this to be 1 / true.</param>
    protected void Destroy(AtkResNode* thisPtr, bool free)
    {
        this.Dispose(true, true);

        this.originalVirtualTable->Destroy(thisPtr, free);

        IMemorySpace.Free(this.modifiedVirtualTable, 0x8 * 4);
        this.modifiedVirtualTable = null;

        // Suppress GC here as at this point the node is considered fully disposed.
        GC.SuppressFinalize(this);

        this.IsDisposed = true;
    }

    /// <summary>
    /// Invokes the original games destroy function without calling back to the native disposal method.
    /// </summary>
    /// <remarks>
    /// To be invoked from NodeBase.Dispose(bool, bool).
    /// This is intended to be used from <see cref="NodeBase"/> after the managed disposal functions have been invoked.
    /// </remarks>
    /// <param name="thisPtr">The pointer to the node instance.</param>
    /// <param name="free">Free flags, these are provided by the game, generally expect this to be 1 / true.</param>
    protected void OriginalDestroy(AtkResNode* thisPtr, bool free)
    {
        this.originalVirtualTable->Destroy(thisPtr, free);

        IMemorySpace.Free(this.modifiedVirtualTable, 0x8 * 4);
        this.modifiedVirtualTable = null;

        // Suppress GC here as at this point the node is considered fully disposed.
        GC.SuppressFinalize(this);

        this.IsDisposed = true;
    }
}
