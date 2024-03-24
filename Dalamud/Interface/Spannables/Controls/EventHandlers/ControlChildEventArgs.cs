using System.Diagnostics.CodeAnalysis;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Arguments for child controls related events.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record ControlChildEventArgs : ControlEventArgs
{
    /// <summary>Gets or sets index of <see cref="Child"/> within <see cref="ControlEventArgs.Sender"/>.</summary>
    public int Index { get; set; }

    /// <summary>Gets or sets the relevant child control.</summary>
    public ISpannable Child { get; set; }

    /// <summary>Gets or sets the previous child control, if the child at <see cref="Index"/> has been changed.</summary>
    public ISpannable? OldChild { get; set; }
}
