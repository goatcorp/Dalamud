namespace Dalamud;

/// <summary>
/// Marker class for service types.
/// </summary>
public interface IServiceType
{
}

/// <summary><see cref="IDisposable"/>, but for <see cref="IServiceType"/>.</summary>
/// <remarks>Use this to prevent services from accidentally being disposed by plugins or <c>using</c> clauses.</remarks>
internal interface IInternalDisposableService : IServiceType
{
    /// <summary>Disposes the service.</summary>
    void DisposeService();
}

/// <summary>An <see cref="IInternalDisposableService"/> which happens to be public and needs to expose
/// <see cref="IDisposable.Dispose"/>.</summary>
internal interface IPublicDisposableService : IInternalDisposableService, IDisposable
{
    /// <summary>Marks that only <see cref="IInternalDisposableService.DisposeService"/> should respond,
    /// while suppressing <see cref="IDisposable.Dispose"/>.</summary>
    void MarkDisposeOnlyFromService();
}
