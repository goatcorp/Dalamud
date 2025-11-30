namespace Dalamud.Game.Addon.Lifecycle;

/// <summary>
/// Enumeration for available AddonLifecycle arg data.
/// </summary>
public enum AddonArgsType
{
    /// <summary>
    /// Generic arg type that contains no meaningful data.
    /// </summary>
    Generic,

    /// <summary>
    /// Contains argument data for Setup.
    /// </summary>
    Setup,

    /// <summary>
    /// Contains argument data for RequestedUpdate.
    /// </summary>
    RequestedUpdate,

    /// <summary>
    /// Contains argument data for Refresh.
    /// </summary>
    Refresh,

    /// <summary>
    /// Contains argument data for ReceiveEvent.
    /// </summary>
    ReceiveEvent,
}
