namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate object (mostly like player or NPC).
/// </summary>
public interface INameplateObject
{
    /// <summary>
    /// Gets the pointer for the nameplate object.
    /// </summary>
    INameplateInfo Nameplate { get; }

    /// <summary>
    /// Gets the nameplate info for this object.
    /// </summary>
    nint Pointer { get; }
}
