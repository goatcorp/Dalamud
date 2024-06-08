using Dalamud.Game.Gui.Nameplates.Model;

namespace Dalamud.Game.Gui.Nameplates.EventArgs;

/// <summary>
/// Event arguments for <see cref="INameplatesGui.OnNameplateUpdate"/>.
/// </summary>
/// <param name="nameplateInfo">dddd.</param>
/// <param name="nameplateObject">dedd.</param>
public class NameplateUpdateEventArgs(NameplateInfo nameplateInfo, NameplateObject nameplateObject)
{
    /// <summary>
    /// Gets an object that represents the nameplate object (mostly like the player or NPC).
    /// </summary>
    public NameplateObject NameplateObject { get; } = nameplateObject;

    /// <summary>
    /// Gets an object that holds some infos about the nameplate.
    /// </summary>
    public NameplateInfo NameplateInfo { get; } = nameplateInfo;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="NameplateInfo"/> has been changed.
    /// <br/>Set this to <see cref="T:true"/> if you changes to <see cref="NameplateInfo"/> should take affect.
    /// </summary>
    public bool HasChanged { get; set; }
}
