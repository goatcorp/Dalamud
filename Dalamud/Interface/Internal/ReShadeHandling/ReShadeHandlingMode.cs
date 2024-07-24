namespace Dalamud.Interface.Internal.ReShadeHandling;

/// <summary>Available handling modes for working with ReShade.</summary>
internal enum ReShadeHandlingMode
{
    /// <summary>Register as a ReShade addon, and draw on reshade_overlay event.</summary>
    ReShadeAddon,

    /// <summary>Unwraps ReShade from the swap chain obtained from the game.</summary>
    UnwrapReShade,

    /// <summary>Do not do anything special about it. ReShade will process Dalamud rendered stuff.</summary>
    None = -1,
}
