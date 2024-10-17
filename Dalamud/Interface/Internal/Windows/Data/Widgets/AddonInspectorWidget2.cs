namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying addon inspector.
/// </summary>
internal class AddonInspectorWidget2 : IDataWindowWidget
{
    private UiDebug2.UiDebug2? addonInspector2;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = ["ai2", "addoninspector2"];

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Addon Inspector v2 (Testing)";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.addonInspector2 = new UiDebug2.UiDebug2();

        if (this.addonInspector2 is not null)
        {
            this.Ready = true;
        }
    }

    /// <inheritdoc/>
    public void Draw()
    {
        this.addonInspector2?.Draw();
    }
}
