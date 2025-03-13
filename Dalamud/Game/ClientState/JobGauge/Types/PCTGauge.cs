using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

using CanvasFlags = Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags;
using CreatureFlags = Dalamud.Game.ClientState.JobGauge.Enums.CreatureFlags;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-Memory PCT job gauge.
/// </summary>
public unsafe class PCTGauge : JobGaugeBase<PictomancerGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PCTGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal PCTGauge(IntPtr address) 
        : base(address)
    {
    }

    /// <summary>
    /// Gets the use of subjective pallete.
    /// </summary>
    public byte PalleteGauge => Struct->PalleteGauge;

    /// <summary>
    /// Gets the amount of paint the player has.
    /// </summary>
    public byte Paint => Struct->Paint;
    
    /// <summary>
    /// Gets a value indicating whether or not a creature motif is drawn.
    /// </summary>
    public bool CreatureMotifDrawn => Struct->CreatureMotifDrawn;

    /// <summary>
    /// Gets a value indicating whether or not a weapon motif is drawn.
    /// </summary>
    public bool WeaponMotifDrawn => Struct->WeaponMotifDrawn;

    /// <summary>
    /// Gets a value indicating whether or not a landscape motif is drawn.
    /// </summary>
    public bool LandscapeMotifDrawn => Struct->LandscapeMotifDrawn;

    /// <summary>
    /// Gets a value indicating whether or not a moogle portrait is ready.
    /// </summary>
    public bool MooglePortraitReady => Struct->MooglePortraitReady;
    
    /// <summary>
    /// Gets a value indicating whether or not a madeen portrait is ready.
    /// </summary>
    public bool MadeenPortraitReady => Struct->MadeenPortraitReady;

    /// <summary>
    /// Gets which creature flags are present.
    /// </summary>
    public CreatureFlags CreatureFlags => (CreatureFlags)Struct->CreatureFlags;

    /// <summary>
    /// Gets which canvas flags are present.
    /// </summary>
    public CanvasFlags CanvasFlags => (CanvasFlags)Struct->CanvasFlags;
}
