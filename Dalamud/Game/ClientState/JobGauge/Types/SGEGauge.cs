namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// In-memory SGE job gauge.
/// </summary>
public unsafe class SGEGauge : JobGaugeBase<FFXIVClientStructs.FFXIV.Client.Game.Gauge.SageGauge>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SGEGauge"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal SGEGauge(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the amount of milliseconds elapsed until the next Addersgall is available.
    /// This counts from 0 to 20_000.
    /// </summary>
    public short AddersgallTimer => this.Struct->AddersgallTimer;

    /// <summary>
    /// Gets the amount of Addersgall available.
    /// </summary>
    public byte Addersgall => this.Struct->Addersgall;

    /// <summary>
    /// Gets the amount of Addersting available.
    /// </summary>
    public byte Addersting => this.Struct->Addersting;

    /// <summary>
    /// Gets a value indicating whether Eukrasia is activated.
    /// </summary>
    public bool Eukrasia => this.Struct->Eukrasia == 1;
}
