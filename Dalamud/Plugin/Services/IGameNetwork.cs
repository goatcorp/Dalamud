using Dalamud.Game.Network;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class handles interacting with game network events.
/// </summary>
/// <remarks>
/// <para>
/// <b>DEPRECATED:</b> This interface passes raw unmanaged pointers which are unsafe for the following reasons:
/// </para>
/// <list type="bullet">
/// <item><description>No bounds checking on pointer arithmetic - can cause access violations</description></item>
/// <item><description>No lifetime management - pointer may be freed while plugin holds it</description></item>
/// <item><description>No size information - callers must guess packet boundaries</description></item>
/// <item><description>Async usage can cause use-after-free vulnerabilities</description></item>
/// </list>
/// <para>
/// <b>Migration Guide:</b>
/// </para>
/// <list type="bullet">
/// <item><description>For market board data: Use the MarketBoard observable services</description></item>
/// <item><description>For duty finder: Use DutyFinder observable services</description></item>
/// <item><description>For custom packets: Create typed hooks using <c>Hook&lt;T&gt;</c> with proper packet structures</description></item>
/// </list>
/// <para>
/// See the Dalamud developer documentation for detailed migration instructions.
/// </para>
/// </remarks>
[Obsolete("Will be removed in a future release. Use packet handler hooks instead. See XML documentation for migration guide.", true)]
public interface IGameNetwork : IDalamudService
{
    /// <summary>
    /// The delegate type of a network message event.
    /// </summary>
    /// <param name="dataPtr">
    /// The pointer to the raw data. WARNING: This pointer has no lifetime guarantees
    /// and must not be stored or used asynchronously.
    /// </param>
    /// <param name="opCode">The operation ID code.</param>
    /// <param name="sourceActorId">The source actor ID.</param>
    /// <param name="targetActorId">The target actor ID.</param>
    /// <param name="direction">The direction of the packet.</param>
    public delegate void OnNetworkMessageDelegate(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction);

    /// <summary>
    /// Event that is called when a network message is sent/received.
    /// </summary>
    /// <remarks>
    /// WARNING: The dataPtr passed to handlers is only valid during the synchronous
    /// execution of the handler. Do not store the pointer or use it in async contexts.
    /// </remarks>
    public event OnNetworkMessageDelegate NetworkMessage;
}
