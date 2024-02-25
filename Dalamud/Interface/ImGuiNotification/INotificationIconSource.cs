namespace Dalamud.Interface.ImGuiNotification;

/// <summary>Icon source for <see cref="INotification"/>.</summary>
/// <remarks>Plugins should NOT implement this interface.</remarks>
public interface INotificationIconSource : ICloneable, IDisposable
{
    /// <summary>The internal interface.</summary>
    internal interface IInternal : INotificationIconSource
    {
        /// <summary>Materializes the icon resource.</summary>
        /// <returns>The materialized resource.</returns>
        INotificationMaterializedIcon Materialize();
    }

    /// <inheritdoc cref="ICloneable.Clone"/>
    new INotificationIconSource Clone();

    /// <inheritdoc/>
    object ICloneable.Clone() => this.Clone();
}
