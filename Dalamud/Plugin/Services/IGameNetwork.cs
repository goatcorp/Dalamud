using Dalamud.Game.Network;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles interacting with game network events.
/// </summary>
public interface IGameNetwork
{
    // TODO(v9): we shouldn't be passing pointers to the actual data here

    /// <summary>
    /// The delegate type of a network message event.
    /// </summary>
    /// <param name="dataPtr">The pointer to the raw data.</param>
    /// <param name="opCode">The operation ID code.</param>
    /// <param name="sourceActorId">The source actor ID.</param>
    /// <param name="targetActorId">The taret actor ID.</param>
    /// <param name="direction">The direction of the packed.</param>
    public delegate void OnNetworkMessageDelegate(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

    /// <summary>
    /// Event that is called when a network message is sent/received.
    /// </summary>
    public event OnNetworkMessageDelegate NetworkMessage;
}
