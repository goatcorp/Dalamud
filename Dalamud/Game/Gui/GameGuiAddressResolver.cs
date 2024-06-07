namespace Dalamud.Game.Gui;

/// <summary>
/// The address resolver for the <see cref="GameGui"/> class.
/// </summary>
internal sealed class GameGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the base address of the native GuiManager class.
    /// </summary>
    public IntPtr BaseAddress { get; private set; }

    /// <summary>
    /// Gets the address of the native SetGlobalBgm method.
    /// </summary>
    public IntPtr SetGlobalBgm { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleItemHover method.
    /// </summary>
    public IntPtr HandleItemHover { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleItemOut method.
    /// </summary>
    public IntPtr HandleItemOut { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleActionHover method.
    /// </summary>
    public IntPtr HandleActionHover { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleActionOut method.
    /// </summary>
    public IntPtr HandleActionOut { get; private set; }

    /// <summary>
    /// Gets the address of the native HandleImm method.
    /// </summary>
    public IntPtr HandleImm { get; private set; }

    /// <summary>
    /// Gets the address of the native GetMatrixSingleton method.
    /// </summary>
    public IntPtr GetMatrixSingleton { get; private set; }

    /// <summary>
    /// Gets the address of the native ToggleUiHide method.
    /// </summary>
    public IntPtr ToggleUiHide { get; private set; }

    /// <summary>
    /// Gets the address of the native Utf8StringFromSequence method.
    /// </summary>
    public IntPtr Utf8StringFromSequence { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.SetGlobalBgm = sig.ScanText("4C 8B 15 ?? ?? ?? ?? 4D 85 D2 74 58");
        this.HandleItemHover = sig.ScanText("E8 ?? ?? ?? ?? 48 8B 5C 24 ?? 48 89 AE ?? ?? ?? ?? 48 89 AE ?? ?? ?? ??");
        this.HandleItemOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 4D");
        this.HandleActionHover = sig.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 83 F8 0F");
        this.HandleActionOut = sig.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 48 8B F9 4D 85 C0 74 1F");
        this.HandleImm = sig.ScanText("E8 ?? ?? ?? ?? 84 C0 75 10 48 83 FF 09");
        this.GetMatrixSingleton = sig.ScanText("E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 48 89 4c 24 ?? 4C 8D 4D ?? 4C 8D 44 24 ??");
        this.ToggleUiHide = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 B9 ?? ?? ?? ??");
        this.Utf8StringFromSequence = sig.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8D 41 22 66 C7 41 ?? ?? ?? 48 89 01 49 8B D8");
    }
}
