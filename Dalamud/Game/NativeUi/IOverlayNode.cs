using Dalamud.Game.NativeWrapper;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.Game.NativeUi;

/// <summary>
/// Interface for use with <see cref="INativeOverlayProvider"/>.
/// </summary>
public interface IOverlayNode
{
    /// <summary>
    /// Gets the address of the allocated node. Typically, an AtkResNode*.
    /// </summary>
    nint NodePointer { get; }

    /// <summary>
    /// Callback to have you actually attach your node to this addon.
    /// You should allocate your node inside this callback, and attach your node to the root node of the provided addon.
    /// </summary>
    /// <remarks>
    /// Your node's nodeId will be set to an appropriate value prior to this call.
    /// </remarks>
    /// <param name="atkUnitBase">Pointer to AtkUnitBase.</param>
    void PerformAttach(AtkUnitBasePtr atkUnitBase);

    /// <summary>
    /// Callback to have you actually detach your node from this addon.
    /// You should dispose/free your node inside this callback, and detach your node from the root node of the provided addon.
    /// </summary>
    /// <param name="atkUnitBase">Pointer to AtkUnitBase.</param>
    void PerformDetach(AtkUnitBasePtr atkUnitBase);

    /// <summary>
    /// Function that is invoked when the overlay addon updates.
    /// </summary>
    void Update();

    /// <summary>
    /// Internal helper method to get the container pointer easier.
    /// </summary>
    /// <returns>AtkResNode Pointer.</returns>
    internal unsafe AtkResNode* GetAsAtkResNode()
        => (AtkResNode*)this.NodePointer;
}
