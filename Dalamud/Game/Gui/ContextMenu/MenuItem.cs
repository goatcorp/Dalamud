using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
///  Interface representing a menu item to be added to a context menu.
/// </summary>
public interface IMenuItem
{
    /// <summary>
    /// The default prefix used if no specific preset is specified.
    /// </summary>
    public const SeIconChar DalamudDefaultPrefix = SeIconChar.BoxedLetterD;

    /// <summary>
    /// The default prefix color used if no specific preset is specified.
    /// </summary>
    public const ushort DalamudDefaultPrefixColor = 539;

    /// <summary>
    /// Gets or sets the display name of the menu item.
    /// </summary>
    SeString Name { get; set; }

    /// <summary>
    /// Gets or sets the prefix attached to the beginning of <see cref="Name"/>.
    /// </summary>
    SeIconChar? Prefix { get; set; }

    /// <summary>
    /// Sets the character to prefix the <see cref="Name"/> with. Will be converted into a fancy boxed letter icon. Must be an uppercase letter.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> must be an uppercase letter.</exception>
    char? PrefixChar { set; }

    /// <summary>
    /// Gets or sets the color of the <see cref="Prefix"/>. Specifies a <see cref="UIColor"/> row id.
    /// </summary>
    ushort PrefixColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the dev wishes to intentionally use the default prefix symbol and color.
    /// </summary>
    bool UseDefaultPrefix { get; set; }

    /// <summary>
    /// Gets or sets the callback to be invoked when the menu item is clicked.
    /// </summary>
    Action<IMenuItemClickedArgs>? OnClicked { get; set; }

    /// <summary>
    /// Gets or sets the priority (or order) with which the menu item should be displayed in descending order.
    /// Priorities below 0 will be displayed above the native menu items.
    /// Other priorities will be displayed below the native menu items.
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is enabled.
    /// Disabled items will be faded and cannot be clicked on.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is a submenu.
    /// This value is purely visual. Submenu items will have an arrow to its right.
    /// </summary>
    bool IsSubmenu { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu item is a return item.
    /// This value is purely visual. Return items will have a back arrow to its left.
    /// If both <see cref="IsSubmenu"/> and <see cref="IsReturn"/> are true, the return arrow will take precedence.
    /// </summary>
    bool IsReturn { get; set; }
}

/// <summary>
/// A menu item that can be added to a context menu.
/// </summary>
public sealed record MenuItem : IMenuItem
{
    /// <inheritdoc/>
    public SeString Name { get; set; } = SeString.Empty;

    /// <inheritdoc/>
    public SeIconChar? Prefix { get; set; }

    /// <inheritdoc/>
    public char? PrefixChar
    {
        set
        {
            if (value is { } prefix)
            {
                if (!char.IsAsciiLetterUpper(prefix))
                    throw new ArgumentException("Prefix must be an uppercase letter", nameof(value));

                this.Prefix = SeIconChar.BoxedLetterA + prefix - 'A';
            }
            else
            {
                this.Prefix = null;
            }
        }
    }

    /// <inheritdoc/>
    public ushort PrefixColor { get; set; }
    
    /// <inheritdoc/>
    public bool UseDefaultPrefix { get; set; }

    /// <inheritdoc/>
    public Action<IMenuItemClickedArgs>? OnClicked { get; set; }

    /// <inheritdoc/>
    public int Priority { get; set; }

    /// <inheritdoc/>
    public bool IsEnabled { get; set; } = true;

    /// <inheritdoc/>
    public bool IsSubmenu { get; set; }

    /// <inheritdoc/>
    public bool IsReturn { get; set; }
}
