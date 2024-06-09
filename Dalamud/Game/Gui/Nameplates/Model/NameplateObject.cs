namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate object (mostly like player or NPC).
/// </summary>
/// <param name="pointer">The pointer for the nameplate object.</param>
/// <param name="info">The nameplate info for this nameplate object.</param>
internal class NameplateObject(IntPtr pointer, INameplateInfo info) : INameplateObject
{
    /// <inheritdoc/>
    public nint Pointer { get; } = pointer;

    /// <inheritdoc/>
    public INameplateInfo Nameplate { get; } = info;
}
