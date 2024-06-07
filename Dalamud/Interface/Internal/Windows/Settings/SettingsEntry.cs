namespace Dalamud.Interface.Internal.Windows.Settings;

/// <summary>
/// Basic, drawable settings entry.
/// </summary>
public abstract class SettingsEntry
{
    /// <summary>
    /// Gets or sets the public, searchable name of this settings entry.
    /// </summary>
    public string? Name { get; protected set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not this entry is valid.
    /// </summary>
    public virtual bool IsValid { get; protected set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether or not this entry is visible.
    /// </summary>
    public virtual bool IsVisible { get; protected set; } = true;

    /// <summary>
    /// Gets the ID of this settings entry, used for ImGui uniqueness.
    /// </summary>
    protected Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Load this setting.
    /// </summary>
    public abstract void Load();

    /// <summary>
    /// Save this setting.
    /// </summary>
    public abstract void Save();

    /// <summary>
    /// Draw this setting control.
    /// </summary>
    public abstract void Draw();

    /// <summary>
    /// Function to be called when the tab is opened.
    /// </summary>
    public virtual void OnOpen()
    {
        // ignored
    }
    
    /// <summary>
    /// Function to be called when the tab is closed.
    /// </summary>
    public virtual void OnClose()
    {
        // ignored
    }
}
