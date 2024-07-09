namespace Dalamud.Game.Gui.NamePlate;

/// <summary>
/// An enum describing the string fields available in nameplate data. The <see cref="NamePlateKind"/> and various flags
/// determine which fields will actually be rendered.
/// </summary>
public enum NamePlateStringField
{
    /// <summary>
    /// The object's name.
    /// </summary>
    Name = 0,

    /// <summary>
    /// The object's title.
    /// </summary>
    Title = 50,

    /// <summary>
    /// The object's free company tag.
    /// </summary>
    FreeCompanyTag = 100,

    /// <summary>
    /// The object's status prefix.
    /// </summary>
    StatusPrefix = 150,

    /// <summary>
    /// The object's target suffix.
    /// </summary>
    TargetSuffix = 200,

    /// <summary>
    /// The object's level prefix.
    /// </summary>
    LevelPrefix = 250,
}
