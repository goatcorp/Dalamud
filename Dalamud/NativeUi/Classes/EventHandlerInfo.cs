using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Simple helper class for union an action with a receiveEvent delegate.
/// </summary>
internal class EventHandlerInfo
{
    /// <summary>
    /// Gets or sets a delegate with full callback args.
    /// </summary>
    public AtkEventListener.Delegates.ReceiveEvent? OnReceiveEventDelegate { get; set; }

    /// <summary>
    /// Gets or sets a delegate with no callback args.
    /// </summary>
    public Action? OnActionDelegate { get; set; }
}
