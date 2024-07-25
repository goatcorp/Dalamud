namespace Dalamud.Interface.Internal.ReShadeHandling;

/// <summary>Available handling modes for working with ReShade.</summary>
internal enum ReShadeHandlingMode
{
    /// <summary>Use the default method, whatever it is for the current Dalamud version.</summary>
    Default = 0,

    /// <summary>Unwrap ReShade from the swap chain obtained from the game.</summary>
    UnwrapReShade,

    /// <summary>Register as a ReShade addon, and draw on <see cref="ReShadeAddonInterface.AddonEvent.Present"/> event.
    /// </summary>
    ReShadeAddonPresent,

    /// <summary>Register as a ReShade addon, and draw on <see cref="ReShadeAddonInterface.AddonEvent.ReShadeOverlay"/>
    /// event. </summary>
    ReShadeAddonReShadeOverlay,

    /// <summary>Hook <c>DXGISwapChain::on_present(UINT flags, const DXGI_PRESENT_PARAMETERS *params)</c> in
    /// <c>dxgi_swapchain.cpp</c>.</summary>
    HookReShadeDxgiSwapChainOnPresent,

    /// <summary>Do not do anything special about it. ReShade will process Dalamud rendered stuff.</summary>
    None = -1,
}
