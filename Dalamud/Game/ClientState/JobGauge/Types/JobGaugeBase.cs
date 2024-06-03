namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// Base job gauge class.
/// </summary>
public abstract unsafe class JobGaugeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobGaugeBase"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal JobGaugeBase(IntPtr address)
    {
        this.Address = address;
    }

    /// <summary>
    /// Gets the address of this job gauge in memory.
    /// </summary>
    public IntPtr Address { get; }
}
