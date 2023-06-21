﻿namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Widget for displaying addon inspector.
/// </summary>
internal class AddonInspectorWidget : IDataWindowWidget
{
    private UiDebug? addonInspector;
    
    /// <inheritdoc/>
    public DataKind DataKind { get; init; } = DataKind.Addon_Inspector;

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.addonInspector = new UiDebug();

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
