using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.Controls.EventHandlers;

/// <summary>Base class for control events.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableControlEventArgs : IResettable
{
    /// <summary>Gets or sets the control that generated the event.</summary>
    public ControlSpannable Sender { get; set; }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool TryReset()
    {
        this.Sender = null!;
        return true;
    }
}
