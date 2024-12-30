using Dalamud.Configuration.Internal;

namespace Dalamud.Interface.Windowing.Persistence;

/// <summary>
/// Class handling persistence for window system windows.
/// </summary>
[ServiceManager.EarlyLoadedService]
internal class WindowSystemPersistence : IServiceType
{
    [ServiceManager.ServiceDependency]
    private readonly DalamudConfiguration config = Service<DalamudConfiguration>.Get();

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowSystemPersistence"/> class.
    /// </summary>
    [ServiceManager.ServiceConstructor]
    public WindowSystemPersistence()
    {
    }

    /// <summary>
    /// Gets the active window system preset.
    /// </summary>
    public PresetModel ActivePreset => this.config.DefaultUiPreset;

    /// <summary>
    /// Get or add a window to the active preset.
    /// </summary>
    /// <param name="id">The ID of the window.</param>
    /// <returns>The preset window instance, or null if the preset does not contain this window.</returns>
    public PresetModel.PresetWindow? GetWindow(uint id)
    {
        return this.ActivePreset.Windows.TryGetValue(id, out var window) ? window : null;
    }

    /// <summary>
    /// Persist the state of a window to the active preset.
    /// </summary>
    /// <param name="id">The ID of the window.</param>
    /// <param name="window">The preset window instance.</param>
    public void SaveWindow(uint id, PresetModel.PresetWindow window)
    {
        this.ActivePreset.Windows[id] = window;
        this.config.QueueSave();
    }
}
