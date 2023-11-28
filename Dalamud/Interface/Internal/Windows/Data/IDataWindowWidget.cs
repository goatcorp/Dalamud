using System.Linq;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Class representing a date window entry.
/// </summary>
internal interface IDataWindowWidget
{
    /// <summary>
    /// Gets the command strings that can be used to open the data window directly to this module.
    /// </summary>
    string[]? CommandShortcuts { get; init; }
    
    /// <summary>
    /// Gets the display name for this module.
    /// </summary>
    string DisplayName { get; init; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this data window module is ready.
    /// </summary>
    bool Ready { get; protected set; }

    /// <summary>
    /// Loads the necessary data for this data window module.
    /// </summary>
    void Load();
    
    /// <summary>
    /// Draws this data window module.
    /// </summary>
    void Draw();

    /// <summary>
    /// Helper method to check if this widget should be activated by the input command.
    /// </summary>
    /// <param name="command">The command being run.</param>
    /// <returns>true if this module should be activated by the input command.</returns>
    bool IsWidgetCommand(string command) => this.CommandShortcuts?.Any(shortcut => string.Equals(shortcut, command, StringComparison.InvariantCultureIgnoreCase)) ?? false;
}
