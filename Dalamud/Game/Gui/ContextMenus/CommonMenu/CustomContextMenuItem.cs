using System;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenus.CommonMenu;

/// <summary>
/// A custom context menu item
/// </summary>
public abstract class CustomContextMenuItem<T> : BaseContextMenuItem
    where T : Delegate {
    internal IntPtr Agent { get; set; }

    /// <summary>
    /// The name of the context item to be shown for English clients.
    /// </summary>
    public SeString NameEnglish { get; set; }

    /// <summary>
    /// The name of the context item to be shown for Japanese clients.
    /// </summary>
    public SeString NameJapanese { get; set; }

    /// <summary>
    /// The name of the context item to be shown for French clients.
    /// </summary>
    public SeString NameFrench { get; set; }

    /// <summary>
    /// The name of the context item to be shown for German clients.
    /// </summary>
    public SeString NameGerman { get; set; }

    /// <summary>
    /// The action to perform when this item is clicked.
    /// </summary>
    public T Action { get; set; }

    /// <summary>
    /// Create a new context menu item.
    /// </summary>
    /// <param name="name">the English name of the item, copied to other languages</param>
    /// <param name="action">the action to perform on click</param>
    internal CustomContextMenuItem(SeString name, T action) {
        this.NameEnglish = name;
        this.NameJapanese = name;
        this.NameFrench = name;
        this.NameGerman = name;

        this.Action = action;
    }
}
