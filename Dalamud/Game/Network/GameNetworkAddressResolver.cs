using Dalamud.Plugin.Services;

namespace Dalamud.Game.Network;

/// <summary>
/// The address resolver for the <see cref="GameNetwork"/> class.
/// </summary>
internal sealed class GameNetworkAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the ProcessZonePacketUp method.
    /// </summary>
    public IntPtr ProcessZonePacketUp { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.ProcessZonePacketUp = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 4C 89 64 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70"); // unnamed in cs
    }
}
