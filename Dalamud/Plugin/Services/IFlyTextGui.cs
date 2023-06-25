using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Plugin.Services;

/// <summary>
/// This class facilitates interacting with and creating native in-game "fly text".
/// </summary>
public interface IFlyTextGui
{
    /// <summary>
    /// The delegate defining the type for the FlyText event.
    /// </summary>
    /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
    /// <param name="val1">Value1 passed to the native flytext function.</param>
    /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
    /// <param name="text1">Text1 passed to the native flytext function.</param>
    /// <param name="text2">Text2 passed to the native flytext function.</param>
    /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
    /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
    /// <param name="damageTypeIcon">Damage Type Icon ID passed to the native flytext function. Displayed next to damage values to denote damage type.</param>
    /// <param name="yOffset">The vertical offset to place the flytext at. 0 is default. Negative values result
    /// in text appearing higher on the screen. This does not change where the element begins to fade.</param>
    /// <param name="handled">Whether this flytext has been handled. If a subscriber sets this to true, the FlyText will not appear.</param>
    public delegate void OnFlyTextCreatedDelegate(
        ref FlyTextKind kind,
        ref int val1,
        ref int val2,
        ref SeString text1,
        ref SeString text2,
        ref uint color,
        ref uint icon,
        ref uint damageTypeIcon,
        ref float yOffset,
        ref bool handled);
    
    /// <summary>
    /// The FlyText event that can be subscribed to.
    /// </summary>
    public event OnFlyTextCreatedDelegate? FlyTextCreated;

    /// <summary>
    /// Displays a fly text in-game on the local player.
    /// </summary>
    /// <param name="kind">The FlyTextKind. See <see cref="FlyTextKind"/>.</param>
    /// <param name="actorIndex">The index of the actor to place flytext on. Indexing unknown. 1 places flytext on local player.</param>
    /// <param name="val1">Value1 passed to the native flytext function.</param>
    /// <param name="val2">Value2 passed to the native flytext function. Seems unused.</param>
    /// <param name="text1">Text1 passed to the native flytext function.</param>
    /// <param name="text2">Text2 passed to the native flytext function.</param>
    /// <param name="color">Color passed to the native flytext function. Changes flytext color.</param>
    /// <param name="icon">Icon ID passed to the native flytext function. Only displays with select FlyTextKind.</param>
    /// <param name="damageTypeIcon">Damage Type Icon ID passed to the native flytext function. Displayed next to damage values to denote damage type.</param>
    public void AddFlyText(FlyTextKind kind, uint actorIndex, uint val1, uint val2, SeString text1, SeString text2, uint color, uint icon, uint damageTypeIcon);
}
