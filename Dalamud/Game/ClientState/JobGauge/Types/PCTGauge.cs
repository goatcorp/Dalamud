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
    /// Tracks use of subjective pallete
    /// </summary>
    public byte PalleteGauge => Struct->PalleteGauge;

    /// <summary>
    /// Number of paint the player has.
    /// </summary>
    public byte Paint => Struct->Paint;
    
    /// <summary>
    /// Creature Motif Stack
    /// </summary>
    public bool CreatureMotifDrawn => Struct->CreatureMotifDrawn;

    /// <summary>
    /// Weapon Motif Stack
    /// </summary>
    public bool WeaponMotifDrawn => Struct->WeaponMotifDrawn;

    /// <summary>
    /// Landscape Motif Stack
    /// </summary>
    public bool LandscapeMotifDrawn => Struct->LandscapeMotifDrawn;

    /// <summary>
    /// Moogle Portrait Stack
    /// </summary>
    public bool MooglePortraitReady => Struct->MooglePortraitReady;
    
    /// <summary>
    /// Madeen Portrait Stack
    /// </summary>
    public bool MadeenPortraitReady => Struct->MadeenPortraitReady;

    /// <summary>
    /// Which creature flags are present.
    /// </summary>
    public CreatureFlags CreatureFlags => (CreatureFlags)Struct->CreatureFlags;

    /// <summary>
    /// Which canvas flags are present.
    /// </summary>
    public CanvasFlags CanvasFlags => (CanvasFlags)Struct->CanvasFlags;
}
