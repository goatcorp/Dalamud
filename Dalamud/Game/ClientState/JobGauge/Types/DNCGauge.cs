namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory DNC job gauge.
/// </summary>
public unsafe class DNCGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.DancerGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DNCGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal DNCGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the number of feathers available.
    /// </summary>
    public byte Feathers => this.Struct->Feathers;

    /// <summary>
    /// Gets the amount of Espirit available.
    /// </summary>
    public byte Esprit => this.Struct->Esprit;

    /// <summary>
    /// Gets the number of steps completed for the current dance.
    /// </summary>
    public byte CompletedSteps => this.Struct->StepIndex;

    /// <summary>
    /// Gets all the steps in the current dance.
    /// </summary>
    public unsafe uint[] Steps
    {
        get
        {
            var arr = new uint[4];
            for (var i = 0; i < 4; i++)
                arr[i] = this.Struct->DanceSteps[i] + 15999u - 1;
            return arr;
        }
    }

    /// <summary>
    /// Gets the next step in the current dance.
    /// </summary>
    /// <returns>The next dance step action ID.</returns>
    public uint NextStep => 15999u + this.Struct->DanceSteps[this.Struct->StepIndex] - 1;

    /// <summary>
    /// Gets a value indicating whether the player is dancing or not.
    /// </summary>
    /// <returns><c>true</c> or <c>false</c>.</returns>
    public bool IsDancing => this.Struct->DanceSteps[0] != 0;
}
