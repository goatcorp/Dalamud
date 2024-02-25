namespace Dalamud.Game.Gui.ContextMenu;

/// <summary>
/// <see cref="ContextMenu"/> memory address resolver.
/// </summary>
internal class ContextMenuAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the RaptureAtkModule.OpenAddonByAgent member function.
    /// This is called whenever an agent requests an addon to be opened.
    /// We only check if addons ContextMenu or AddonContextSub are requested and mark which agent is opening the addon.
    /// Only AgentInventoryContext and AgentContext open these two.
    /// </summary>
    public nint ContextAddonOpenByAgent { get; private set; }
    
    /// <summary>
    /// Gets the address of the AddonContextMenu.vf72 member function.
    /// This is called whenever a menu item is clicked on.
    /// </summary>
    public nint ContextMenuOnVf72 { get; private set; }

    /// <summary>
    /// Gets the address of the RaptureAtkModule.OpenAddon member function.
    /// We call this whenever we need to open a submenu.
    /// </summary>
    public nint RaptureAtkModuleOpenAddon { get; private set; }

    /// <summary>
    /// Scan for and setup any configured address pointers.
    /// </summary>
    /// <param name="sig">The signature scanner to facilitate setup.</param>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.ContextAddonOpenByAgent = sig.ScanText("E8 ?? ?? ?? ?? 0F B7 C0 48 83 C4 60");
        this.ContextMenuOnVf72 = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 56 57 41 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 80 B9");
        this.RaptureAtkModuleOpenAddon = sig.ScanText("E8 ?? ?? ?? ?? 66 89 46 50");
    }
}
