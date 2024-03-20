namespace Dalamud.Game.Gui.Toast;

/// <summary>
/// An address resolver for the <see cref="ToastGui"/> class.
/// </summary>
internal class ToastGuiAddressResolver : BaseAddressResolver
{
    /// <summary>
    /// Gets the address of the native ShowNormalToast method.
    /// </summary>
    public IntPtr ShowNormalToast { get; private set; }

    /// <summary>
    /// Gets the address of the native ShowQuestToast method.
    /// </summary>
    public IntPtr ShowQuestToast { get; private set; }

    /// <summary>
    /// Gets the address of the ShowErrorToast method.
    /// </summary>
    public IntPtr ShowErrorToast { get; private set; }

    /// <inheritdoc/>
    protected override void Setup64Bit(ISigScanner sig)
    {
        this.ShowNormalToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 83 3D ?? ?? ?? ?? ??");
        this.ShowQuestToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 40 83 3D ?? ?? ?? ?? ??");
        this.ShowErrorToast = sig.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 83 3D ?? ?? ?? ?? ?? 41 0F B6 F0");
    }
}
