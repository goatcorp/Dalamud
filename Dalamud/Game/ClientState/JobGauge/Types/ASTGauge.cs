using System.Linq;

using Dalamud.Game.ClientState.JobGauge.Enums;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory AST job gauge.
/// </summary>
public unsafe class ASTGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.AstrologianGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ASTGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal ASTGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the currently drawn <see cref="CardType"/>.
    /// </summary>
    /// <returns>Currently drawn <see cref="CardType"/>.</returns>
    public CardType DrawnCard => (CardType)(this.Struct->Card & 0xF);

    /// <summary>
    /// Gets the currently drawn crown <see cref="CardType"/>.
    /// </summary>
    /// <returns>Currently drawn crown <see cref="CardType"/>.</returns>
    public CardType DrawnCrownCard => this.Struct->Card - this.DrawnCard;

    /// <summary>
    /// Gets the <see cref="SealType"/>s currently active.
    /// </summary>
    public SealType[] Seals => this.Struct->CurrentSeals.Select(seal => (SealType)seal).ToArray();

    /// <summary>
    /// Check if a <see cref="SealType"/> is currently active on the divination gauge.
    /// </summary>
    /// <param name="seal">The <see cref="SealType"/> to check for.</param>
    /// <returns>If the given Seal is currently divined.</returns>
    public unsafe bool ContainsSeal(SealType seal) => this.Seals.Contains(seal);
}
