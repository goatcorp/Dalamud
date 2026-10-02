namespace Dalamud.Game.Addon.Events;

/// <summary>
/// Class that represents a addon event handle.
/// </summary>
internal class AddonEventHandle : IAddonEventHandle
{
    /// <inheritdoc/>
    public uint ParamKey { get; init; }

    /// <inheritdoc/>
    public string AddonName { get; init; } = "NullAddon";

    /// <inheritdoc/>
    public AddonEventType EventType { get; init; }

    /// <inheritdoc/>
    public Guid EventGuid { get; init; }

    /// <summary>
    /// Gets the EventController this handle is registered in.
    /// </summary>
    internal PluginEventController EventController { get; init; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.EventController.RemoveEvent(this);
    }
}
