using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// A menu item that can be added to a context menu.
/// </summary>
public sealed record MenuItem
{
    /// <summary>
    /// Gets or sets the display name of the menu item.
    /// </summary>
    public SeString Name { get; set; } = SeString.Empty;

    /// <summary>
    /// Gets or sets the callback to be invoked when the menu item is clicked.
    /// </summary>
    public Action<MenuItemClickedArgs>? OnClicked { get; set; }

    /// <summary>
    /// Gets or sets the priority (or order) with which the menu item should be displayed in descending order.
    /// Priorities below 0 will be displayed above the native menu items.
    /// Other priorities will be displayed below the native menu items.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is enabled.
    /// Disabled items will be faded and cannot be clicked on.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is a submenu.
    /// This value is purely visual. Submenu items will have an arrow to its right.
    /// </summary>
    public bool IsSubmenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is a return item.
    /// This value is purely visual. Return items will have a back arrow to its left.
    /// If both <see cref="IsSubmenu"/> and <see cref="IsReturn"/> are true, the return arrow will take precedence.
    /// </summary>
    public bool IsReturn { get; set; }

    /// <summary>
    /// Sets the name with a prefixed letter.
    /// The prefix is a boxed letter icon with the specified color.
    /// This can be used for adding a prefix to a <see cref="MenuItem"/>.
    /// </summary>
    /// <param name="name">The name to be prefixed.</param>
    /// <param name="prefix">The character to prefix the name with.</param>
    /// <param name="colorKey">The color to make the prefix letter.</param>
    /// <returns>Itself.</returns>
    /// <exception cref="ArgumentException"><paramref name="prefix"/> must be an uppercase letter.</exception>
    public MenuItem WithPrefixedName(SeString name, char prefix = 'D', ushort colorKey = 539)
    {
        if (!char.IsAsciiLetterUpper(prefix))
            throw new ArgumentException("Prefix must be an uppercase letter", nameof(prefix));

        return this.WithPrefixedName(name, SeIconChar.BoxedLetterA + prefix - 'A', colorKey);
    }

    /// <summary>
    /// Sets the name with a prefixed icon.
    /// The prefix can be any icon character with the specified color.
    /// This can be used for adding a prefix to a <see cref="MenuItem"/>.
    /// </summary>
    /// <param name="name">The name to be prefixed.</param>
    /// <param name="prefix">The icon to prefix the name with.</param>
    /// <param name="colorKey">The color to make the prefix icon.</param>
    /// <returns>Itself.</returns>
    public MenuItem WithPrefixedName(SeString name, SeIconChar prefix, ushort colorKey)
    {
        this.Name = new SeStringBuilder()
            .AddUiForeground($"{prefix.ToIconString()} ", colorKey)
            .Append(name)
            .Build();
        return this;
    }
}
