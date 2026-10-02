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
    /// <param name="node">Instance of your implementation of <see cref="IOverlayNode">.</param>
    /// <param name="depthLayer">Which depth layer to attach to.</param>
    void AddNode(IOverlayNode node, int depthLayer);

    /// <summary>
    /// Removes a node from a native addon on the specified layer.
    /// Also disposes the node.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    /// <param name="node">Instance of your implementation of <see cref="IOverlayNode">.</param>
    /// <param name="depthLayer">Which depth layer to remove it from.</param>
    void RemoveNode(IOverlayNode node, int depthLayer);
}
