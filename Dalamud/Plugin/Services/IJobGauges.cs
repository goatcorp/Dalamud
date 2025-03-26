using Dalamud.Game.ClientState.JobGauge.Types;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class converts in-memory Job gauge data to structs.
/// </summary>
public interface IJobGauges
{
    /// <summary>
    /// Gets the address of the JobGauge data.
    /// </summary>
    public nint Address { get; }

    /// <summary>
    /// Get the JobGauge for a given job.
    /// </summary>
    /// <typeparam name="T">A JobGauge struct from ClientState.Structs.JobGauge.</typeparam>
    /// <returns>A JobGauge.</returns>
    public T Get<T>() where T : JobGaugeBase;
}
