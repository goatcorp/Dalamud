using Dalamud.Game.NativeUi;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service api for providing devs with access to adding native ui elements to overlay addons.
/// </summary>
public interface INativeOverlay : IDalamudService
{
    /// <summary>
    /// Adds a node to a native addon on the specified layer.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    /// <param name="node">Pointer to the node to attach.</param>
    /// <param name="depthLayer">Which depth layer to attach to.</param>
    /// <returns>true when attaching was successful.</returns>
    bool AddNode(IOverlayNode node, int depthLayer);

    /// <summary>
    /// Removes a node from a native addon on the specified layer.
    /// Also disposes the node.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    /// <param name="node">Pointer to the node to remove.</param>
    /// <param name="depthLayer">Which depth layer to remove it from.</param>
    /// <returns>true when removing and disposing was successful.</returns>
    bool RemoveNode(IOverlayNode node, int depthLayer);
}
