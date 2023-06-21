namespace Dalamud.Interface.Internal.Windows;

/// <summary>
/// Class representing a date window entry.
/// </summary>
internal interface IDataWindowWidget
{
    /// <summary>
    /// Gets the Data Kind for this data window module.
    /// </summary>
    DataKind DataKind { get; init; }
    
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
}
