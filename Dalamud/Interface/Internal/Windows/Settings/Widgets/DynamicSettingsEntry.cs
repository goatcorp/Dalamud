namespace Dalamud.Interface.Internal.Windows.Settings.Widgets;

/// <summary>
/// A SettingsEntry designed for dynamic events that need to be evaluated once on every frame.
/// </summary>
public class DynamicSettingsEntry : SettingsEntry
{
    private Action drawAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="DynamicSettingsEntry"/> class.
    /// </summary>
    /// <param name="drawAction">The action to draw when called.</param>
    public DynamicSettingsEntry(Action drawAction)
    {
        this.drawAction = drawAction;
    }

    /// <inheritdoc/>
    public override void Load()
    {
        // ignore
    }

    /// <inheritdoc/>
    public override void Save()
    {
        // ignore;
    }

    /// <inheritdoc/>
    public override void Draw()
    {
        this.drawAction.Invoke();
    }
}
