namespace Dalamud.Game.ClientState.JobGauge.Types;

/// <summary>
/// Base job gauge class.
/// </summary>
/// <typeparam name="T">The underlying FFXIVClientStructs type.</typeparam>
public unsafe class JobGaugeBase<T> : JobGaugeBase where T : unmanaged
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JobGaugeBase{T}"/> class.
    /// </summary>
    /// <param name="address">Address of the job gauge.</param>
    internal JobGaugeBase(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets an unsafe struct pointer of this job gauge.
    /// </summary>
    private protected T* Struct => (T*)this.Address;
}
