namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying addon inspector.
/// </summary>
internal class AddonInspectorWidget : IDataWindowWidget
{
    private UiDebug2.UiDebug2? addonInspector;
    
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "ai", "addoninspector" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Addon Inspector";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.addonInspector = new UiDebug2.UiDebug2();

        if (this.addonInspector is not null)
        {
            this.Ready = true;
        }
    }

    /// <inheritdoc/>
    public void Draw()
    {
        this.addonInspector?.Draw();
    }
}
