using System.Diagnostics.CodeAnalysis;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Extensions;

/// <summary>
/// Extension methods for AtkStage.
/// </summary>
[SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:Prefix local calls with this", Justification = "Stylecop doesn't understand Extension Blocks, you cant prefix with 'this'.")]
internal static unsafe class AtkStageExtensions
{
    extension(ref AtkStage atkStage) {

        /// <summary>
        /// Clears the target node from any focus entry in the AtkStage.FocusList.
        /// </summary>
        /// <param name="targetNode">The node address to search for.</param>
        public void ClearNodeFocus(AtkResNode* targetNode)
        {
            if (targetNode is null) return;

            foreach (ref var focusEntry in atkStage.AtkInputManager->FocusList)
            {
                // If this entry has no listener/addon, skip it
                if (focusEntry.AtkEventListener is null) continue;

                // If this entry has our target node
                if (focusEntry.AtkEventTarget == targetNode)
                {
                    // Clear the entry
                    focusEntry.AtkEventTarget = null;
                    focusEntry.FocusParam = 0;

                    // Clear the input managers focused node
                    atkStage.AtkInputManager->FocusedNode = null;

                    // Clear collision managers collision node
                    atkStage.AtkCollisionManager->IntersectingCollisionNode = null;

                    // Also remove this node from any additional focus nodes the addon might reference
                    var addon = (AtkUnitBase*)focusEntry.AtkEventListener;
                    foreach (ref var node in addon->AdditionalFocusableNodes)
                    {
                        if (node.Value == targetNode)
                        {
                            node = null;
                        }
                    }
                }
            }
        }
    }
}
