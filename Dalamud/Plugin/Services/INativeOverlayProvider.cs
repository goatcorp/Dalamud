using Dalamud.Game.NativeUi;

namespace Dalamud.Plugin.Services;

/// <summary>
/// Service api for providing devs with access to adding native ui elements to overlay addons.
/// </summary>
public interface INativeOverlay : IDalamudService
{
    /// <summary>
    /// Adds a node to a native addon on the specified layer.
    /// Invokes the nodes implemented <see cref="IOverlayNode.PerformAttach"/> method.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    /// <param name="node">Instance of your implementation of <see cref="IOverlayNode">.</param>
    /// <param name="depthLayer">Which depth layer to attach to. Minimum: 0, Maximum: 12. (Lower is below the standard UI).</param>
    void AddNode(IOverlayNode node, int depthLayer);

    /// <summary>
    /// Removes a node from a native addon on the specified layer.
    /// Invokes the nodes implemented <see cref="IOverlayNode.PerformDetach"/> method.
    /// </summary>
    /// <remarks>
    /// Must be called from the games main thread.
    /// </remarks>
    /// <param name="node">Instance of your implementation of <see cref="IOverlayNode">.</param>
    /// <param name="depthLayer">Which depth layer to attach to. Minimum: 0, Maximum: 12. (Lower is below the standard UI).</param>
    void RemoveNode(IOverlayNode node, int depthLayer);
}
