using Dalamud.Utility;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.BaseTypes.Node;

/// <summary>
/// Generic Base class for all nodes used in KamiToolKit.
/// This is not intended for external use. If you need base type use <see cref="NodeBase"/>.
/// </summary>
/// <typeparam name="T">The actual node type to allocate and initialize.</typeparam>
internal abstract unsafe class NodeBase<T> : NodeBase where T : unmanaged, ICreatable<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeBase{T}"/> class.
    /// </summary>
    /// <param name="nodeType">The actual node type to allocate and initialize.</param>
    protected NodeBase(NodeType nodeType)
    {
        ThreadSafety.AssertMainThread();

        Log.Verbose("Creating new node {Name}", this.GetType().Name);
        this.Node = IMemorySpace.GetUISpace()->Create<T>();

        if (this.ResNode is null)
        {
            throw new Exception($"Unable to allocate memory for {typeof(T)}");
        }

        // todo: maybe use a static data share?
        // KamiToolKitLibrary.AllocatedNodes?.TryAdd((nint)this.Node, this.GetType().Name);

        this.BuildVirtualTable();

        ResNode->Type = nodeType;
        ResNode->NodeId = NodeIdBase + CurrentOffset++;
        ResNode->ToggleVisibility(true);
    }

    /// <summary>
    /// Gets the typed inner node pointer for this instance.
    /// </summary>
    public T* Node { get; private set; }

    /// <summary>
    /// Gets the generic typed node contained by this instance.
    /// </summary>
    internal sealed override AtkResNode* ResNode
        => (AtkResNode*)this.Node;

    /// <summary>
    /// Implicit operator to seamlessly cast this instance with the contained node type pointer.
    /// </summary>
    /// <param name="node">This instance of the node.</param>
    public static implicit operator T*(NodeBase<T> node) => node.Node;

    /// <inheritdoc />
    protected override void Dispose(bool disposing, bool isNativeDestructor)
    {
        if (disposing && !this.IsDisposed)
        {
            try
            {
                base.Dispose(disposing, isNativeDestructor);
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception occurred while disposing node {GetType} with address [{ResNode:X8}].", this.GetType().Name, $"0x{(nint)this.ResNode:X8}");
            }
            finally
            {
                if (!isNativeDestructor)
                {
                    this.OriginalDestroy(this, true);
                }

                // todo: maybe use a static data share?
                // KamiToolKitLibrary.AllocatedNodes?.Remove((nint)this.Node, out _);

                this.Node = null;
            }
        }
    }
}
