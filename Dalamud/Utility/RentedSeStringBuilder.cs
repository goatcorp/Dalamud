using Lumina.Text;

namespace Dalamud.Utility;

/// <summary>
/// Provides a temporarily rented <see cref="SeStringBuilder"/> from a shared pool.
/// </summary>
public readonly struct RentedSeStringBuilder() : IDisposable
{
    /// <summary>
    /// Gets the rented <see cref="SeStringBuilder"/> value from the shared pool.
    /// </summary>
    public SeStringBuilder Builder { get; } = SeStringBuilder.SharedPool.Get();

    /// <summary>
    /// Returns the rented <see cref="SeStringBuilder"/> to the shared pool.
    /// </summary>
    public void Dispose() => SeStringBuilder.SharedPool.Return(this.Builder);
}
