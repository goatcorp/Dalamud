namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate object (mostly like player or NPC).
/// </summary>
/// <param name="pointer">The pointer for the nameplate object.</param>
public class NameplateObject(IntPtr pointer)
{
    /// <summary>
    /// Gets the pointer for the nameplate object.
    /// </summary>
    public IntPtr Pointer { get; } = pointer;
}
