using Dalamud.NativeUi.Classes;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// .
/// </summary>
internal unsafe partial class NativeAddon
{
    /// <summary>
    /// Gets a value indicating whether the close button is disabled. Use with caution.
    /// </summary>
    public bool DisableClose { get; init; }

    /// <summary>
    /// Gets a value indicating whether disables closing animation. (But doesn't actually...)
    /// </summary>
    public bool DisableCloseTransition { get; init; }

    /// <summary>
    /// Gets a value indicating whether right-clicking the header should show a context menu, for scaling or resetting this addon.
    /// </summary>
    public bool EnableContextMenu { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this window should be able to be dragged off-screen.
    /// </summary>
    public bool DisableClamping { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the context menu for this addon should allow changing the scale.
    /// </summary>
    public bool DisableScaleContextOption { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this addon should close when esc is pressed with no windows focused.
    /// </summary>
    public bool RespectCloseAll { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this addon should ignore AtkUnitBase.GlobalScale.
    /// </summary>
    public bool IgnoreGlobalScale { get; set; } = false;

    private void UpdateFlags()
    {
        // Disable Native AddonConfig
        this.InternalAddon->DisableAddonConfig = true;
        this.InternalAddon->ShouldFireCallbackAndHideOrClose = this.DisableClose;
        this.InternalAddon->DisableHideTransition = this.DisableCloseTransition;
        this.InternalAddon->EnableTitleBarContextMenu = this.EnableContextMenu;
        this.InternalAddon->DisableUserScaling = this.DisableScaleContextOption;

        FlagHelper.UpdateFlag(ref this.InternalAddon->Flags1A3, 1 << 5, this.DisableClamping);

        if (this.IsOverlayAddon)
        {
            this.SetOverlayFlags();
        }
    }

    private void SetOverlayFlags()
    {
        this.OpenWindowSoundEffectId = 0;
        this.InternalAddon->ShowSoundEffectId = 0;
        this.InternalAddon->DisableFocusability = true;
        this.InternalAddon->DisableFocusOnShow = true;
        this.InternalAddon->DisableHideTransition = true;
        this.InternalAddon->DisableShowHideSoundEffects = true;
        this.InternalAddon->IgnoreUIDisplayMode = true;

        // Disable Controller Nav
        FlagHelper.UpdateFlag(ref this.InternalAddon->Flags1A2, 0x2, true);

        // Enable ClickThrough
        FlagHelper.UpdateFlag(ref this.InternalAddon->Flags1A3, 0x40, true);
    }
}
