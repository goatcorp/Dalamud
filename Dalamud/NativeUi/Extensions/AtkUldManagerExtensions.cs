using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.NativeUi.BaseTypes.Node;

using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.NativeUi.Extensions;

/// <summary>
/// Extension methods for AtkUldManager's.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Stylecop doesn't understand Extension Blocks, you cant prefix with 'this'.")]
internal static unsafe class AtkUldManagerExtensions
{
    extension(ref AtkUldManager manager) {
        /// <summary>
        /// Adds node and all children nodes to this UldManager's Object List.
        /// </summary>
        [OverloadResolutionPriority(1)]
        public void AddNodeToObjectList(NodeBase node)
        {
            if (!manager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized)) return;

            manager.AddNodeToObjectList(node.ResNode);

            foreach (var child in NodeBase.GetLocalChildren(node))
            {
                manager.AddNodeToObjectList(child.ResNode);
            }

            manager.UpdateDrawNodeList();
        }

        /// <summary>
        /// Adds just this node to the UldManagers Object List.
        /// </summary>
        [OverloadResolutionPriority(0)]
        public void AddNodeToObjectList(AtkResNode* newNode)
        {
            if (!manager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized)) return;
            if (newNode is null) return;

            // If the node is already in the object list, skip.
            if (manager.IsNodeInObjectList(newNode)) return;

            var oldSize = manager.Objects->NodeCount;
            var newSize = oldSize + 1;

            var newBuffer = (AtkResNode**)IMemorySpace.GetUISpace()->AlignedRealloc(manager.Objects->NodeList, (ulong)(newSize * 8), 0x10);
            newBuffer[newSize - 1] = newNode;

            manager.Objects->NodeList = newBuffer;
            manager.Objects->NodeCount = newSize;
        }

        /// <summary>
        /// Removes node and all children nodes from this UldManager's Object List
        /// </summary>
        public void RemoveNodeFromObjectList(NodeBase node)
        {
            if (!manager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized)) return;
            manager.RemoveNodeFromObjectList(node.ResNode);

            foreach (var child in NodeBase.GetLocalChildren(node))
            {
                manager.RemoveNodeFromObjectList(child.ResNode);
            }

            manager.UpdateDrawNodeList();
        }

        /// <summary>
        /// Removes just this node from the UldManagers Object List.
        /// </summary>
        public void RemoveNodeFromObjectList(AtkResNode* node)
        {
            if (!manager.ResourceFlags.HasFlag(AtkUldManagerResourceFlag.Initialized))
            {
                return;
            }

            if (node is null)
            {
                return;
            }

            // If the node isn't in the object list, skip.
            if (!manager.IsNodeInObjectList(node))
            {
                return;
            }

            var oldSize = manager.Objects->NodeCount;
            var newSize = oldSize - 1;
            var newBuffer = (AtkResNode**)IMemorySpace.GetUISpace()->Malloc((ulong)(newSize * 8), 8);

            var newIndex = 0;
            foreach (var index in Enumerable.Range(0, oldSize))
            {
                if (manager.Objects->NodeList[index] != node)
                {
                    newBuffer[newIndex] = manager.Objects->NodeList[index];
                    newIndex++;
                }
            }

            IMemorySpace.Free(manager.Objects->NodeList, (ulong)(oldSize * 8));
            manager.Objects->NodeList = newBuffer;
            manager.Objects->NodeCount = newSize;
        }

        private bool IsNodeInObjectList(AtkResNode* node)
        {
            foreach (var objectNode in manager.ObjectNodeSpan)
            {
                if (objectNode.Value == node)
                {
                    return true;
                }
            }

            return false;
        }

        private Span<Pointer<AtkResNode>> ObjectNodeSpan
            => new(manager.Objects->NodeList, manager.Objects->NodeCount);
    }
}
