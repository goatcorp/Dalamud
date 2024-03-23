using Dalamud.Interface.Spannables.Controls.Containers;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Arguments for child controls related events.</summary>
public struct ControlChildEventArgs
{
    /// <summary>The control that generated the event.</summary>
    public ContainerControl Sender;

    /// <summary>The previous child control, if the child at <see cref="Index"/> has been changed.</summary>
    public ISpannable? OldChild;

    /// <summary>The relevant child control.</summary>
    public ISpannable Child;

    /// <summary>Index of <see cref="Child"/> within <see cref="Sender"/>.</summary>
    public int Index;
}
