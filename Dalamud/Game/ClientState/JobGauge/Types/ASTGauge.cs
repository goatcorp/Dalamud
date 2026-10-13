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
    [Obsolete("Use Card1, Card2 and Card3 individually.")]
    public CardType[] DrawnCards => this.Struct->CurrentCards.Select(card => (CardType)card).ToArray();

    /// <summary>
    /// Gets the type of the first drawn card.
    /// </summary>
    public CardType Card1 => (CardType)this.Struct->Card1;

    /// <summary>
    /// Gets the type of the second drawn card.
    /// </summary>
    public CardType Card2 => (CardType)this.Struct->Card2;

    /// <summary>
    /// Gets the type of the third drawn card.
    /// </summary>
    public CardType Card3 => (CardType)this.Struct->Card3;

    /// <summary>
    /// Gets the currently drawn crown <see cref="CardType"/>.
    /// </summary>
    /// <returns>Currently drawn crown <see cref="CardType"/>.</returns>
    public CardType DrawnCrownCard => (CardType)this.Struct->CurrentArcana;

    /// <summary>
    /// Gets the currently active draw type <see cref="DrawType"/>.
    /// </summary>
    /// <returns>Currently active draw type.</returns>
    public DrawType ActiveDraw => (DrawType)this.Struct->CurrentDraw;
}
