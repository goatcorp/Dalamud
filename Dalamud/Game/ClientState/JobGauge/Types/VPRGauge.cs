using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

using Reloaded.Memory;

using DreadCombo = Dalamud.Game.ClientState.JobGauge.Enums.DreadCombo;

namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory VPR job gauge.
/// </summary>
public unsafe class VPRGauge : JobGaugeBase<ViperGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VPRGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal VPRGauge(IntPtr address)
        : base(address)
    {
    }
    
    /// <summary>
    /// How many uses of uncoiled fury the player has.
    /// </summary>
    public byte RattlingCoilStacks => Struct->RattlingCoilStacks;

    /// <summary>
    /// Tracks AnguineTribute stacks and gauge.
    /// </summary>
    public byte SerpentOffering => Struct->SerpentOffering;
    
    /// <summary>
    /// Allows the use of 1st, 2nd, 3rd, 4th generation and Ouroboros.
    /// </summary>
    public byte AnguineTribute => Struct->AnguineTribute;

    /// <summary>
    /// Keeps track of last Weaponskill used in DreadWinder/Pit of Dread combo.
    /// </summary>
    public DreadCombo DreadCombo => (DreadCombo)Struct->DreadCombo;
}
