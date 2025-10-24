using FFXIVClientStructs.FFXIV.Client.Game.Gauge;

using Reloaded.Memory;

using DreadCombo = Dalamud.Game.ClientState.JobGauge.Enums.DreadCombo;
using SerpentCombo = Dalamud.Game.ClientState.JobGauge.Enums.SerpentCombo;

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
    /// Gets how many uses of uncoiled fury the player has.
    /// </summary>
    public byte RattlingCoilStacks => this.Struct->RattlingCoilStacks;

    /// <summary>
    /// Gets Serpent Offering stacks and gauge.
    /// </summary>
    public byte SerpentOffering => this.Struct->SerpentOffering;
    
    /// <summary>
    /// Gets value indicating the use of 1st, 2nd, 3rd, 4th generation and Ouroboros.
    /// </summary>
    public byte AnguineTribute => this.Struct->AnguineTribute;

    /// <summary>
    /// Gets the last Weaponskill used in DreadWinder/Pit of Dread combo.
    /// </summary>
    public DreadCombo DreadCombo => (DreadCombo)this.Struct->DreadCombo;

    /// <summary>
    /// Gets current ability for Serpent's Tail.
    /// </summary>
    public SerpentCombo SerpentCombo => (SerpentCombo)this.Struct->SerpentCombo;
}
