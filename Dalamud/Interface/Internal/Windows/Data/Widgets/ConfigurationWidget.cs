using Dalamud.Configuration.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying configuration info.
/// </summary>
internal class ConfigurationWidget : IDataWindowWidget
{
    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "config", "configuration" };
    
    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Configuration"; 

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var config = Service<DalamudConfiguration>.Get();
        Util.ShowObject(config);
    }
}
