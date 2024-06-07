using System.Linq;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory MNK job gauge.
/// </summary>
public unsafe class MNKGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.MonkGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MNKGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal MNKGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of Chakra available.
    /// </summary>
    public byte Chakra => this.Struct->Chakra;

    /// <summary>
    /// Gets the types of Beast Chakra available.
    /// </summary>
    /// <remarks>
    /// This will always return an array of size 3, inactive Beast Chakra are represented by <see cref="BeastChakra.NONE"/>.
    /// </remarks>
    public BeastChakra[] BeastChakra => this.Struct->BeastChakra.Select(c => (BeastChakra)c).ToArray();

    /// <summary>
    /// Gets the types of Nadi available.
    /// </summary>
    public Nadi Nadi => (Nadi)this.Struct->Nadi;

    /// <summary>
    /// Gets the time remaining that Blitz is active.
    /// </summary>
    public ushort BlitzTimeRemaining => this.Struct->BlitzTimeRemaining;
}
