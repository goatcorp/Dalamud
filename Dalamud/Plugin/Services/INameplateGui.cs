using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.Model;

namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// This class handles interacting with native Nameplate update events and management.
/// </summary>
public interface INameplateGui
{
    /// <summary>
    /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
    /// </summary>
    /// <param name="nameplateObject">Provides some infos about the current updating Nameplate.</param>
    public delegate void OnNameplateUpdateDelegate(INameplateObject nameplateObject);
    
    /// <summary>
    /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
    /// </summary>
    abstract event OnNameplateUpdateDelegate OnNameplateUpdate;
}
