namespace Dalamud.Utility;

/// <summary>
/// An extension of <see cref="IDisposable"/> which makes <see cref="IDisposable.Dispose"/> queue
/// <see cref="RealDispose"/> to be called at a later time.
/// </summary>
internal interface IDeferredDisposable : IDisposable
{
    /// <summary>Actually dispose the object.</summary>
    /// <remarks>Not to be called from the code that uses the end object.</remarks>
    void RealDispose();
}
