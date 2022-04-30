using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus;

/// <summary>
/// A native context menu item.
/// </summary>
public sealed class NativeContextMenuItem : BaseContextMenuItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NativeContextMenuItem"/> class.
    /// </summary>
    /// <param name="action">action.</param>
    /// <param name="name">name.</param>
    /// <param name="enabled">enabled.</param>
    /// <param name="isSubMenu">isSubMenu.</param>
    internal NativeContextMenuItem(byte action, SeString name, bool enabled, bool isSubMenu)
    {
        this.Name = name;
        this.InternalAction = action;
        this.Enabled = enabled;
        this.IsSubMenu = isSubMenu;
    }

    /// <summary>
    /// Gets the action code to be used in the context menu agent for this item.
    /// </summary>
    public byte InternalAction { get; }

    /// <summary>
    /// Gets or sets the name of the context item.
    /// </summary>
    public SeString Name { get; set; }
}
