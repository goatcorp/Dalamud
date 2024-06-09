using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Nameplates.Model;

namespace Dalamud.Game.Gui.Nameplates;

/// <summary>
/// This class handles interacting with native Nameplate update events and management.
/// </summary>
public interface INameplatesGui
{
    /// <summary>
    /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
    /// </summary>
    /// <param name="eventArgs">Provides some infos about the current updating Nameplate.</param>
    public delegate void OnNameplateUpdateDelegate(INameplateObject eventArgs);
    
    /// <summary>
    /// Will be executed when the the Game wants to update the content of a nameplate with the details of the Player.
    /// </summary>
    abstract event OnNameplateUpdateDelegate OnNameplateUpdate;

    /// <summary>
    /// Tries to find a <see cref="GameObject"/> of the given type by a given <see cref="NameplateObject"/>.
    /// </summary>
    /// <typeparam name="T">The type you want to get.</typeparam>
    /// <param name="nameplateObject">The nameplate object to get from.</param>
    /// <returns>Returns a <see cref="GameObject"/> null if failed.</returns>
    abstract T? GetNameplateGameObject<T>(INameplateObject nameplateObject) where T : GameObject;

    /// <summary>
    /// Tries to find a <see cref="GameObject"/> of the given type by a given nameplate object pointer.
    /// </summary>
    /// <typeparam name="T">The type you want to get.</typeparam>
    /// <param name="nameplateObjectPtr">The nameplate object pointer to get from.</param>
    /// <returns>Returns a <see cref="GameObject"/> null if failed.</returns>
    abstract T? GetNameplateGameObject<T>(IntPtr nameplateObjectPtr) where T : GameObject;
}
