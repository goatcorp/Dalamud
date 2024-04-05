using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Base class for control events.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableEventArgs : IResettable
{
    /// <summary>Gets the object that generated the event.</summary>
    public object Sender { get; private set; }

    /// <summary>Gets or sets a value indicating whether to suppress the default handling of the event.</summary>
    /// <value><c>true</c> to bypass the control's default handling; otherwise, <c>false</c> to also pass the event
    /// along to the default control handler.</value>
    public bool SuppressHandling { get; set; }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool TryReset()
    {
        this.Sender = null!;
        return true;
    }

    /// <summary>Initializes this instance of <see cref="SpannableEventArgs"/>.</summary>
    /// <param name="sender">Object that sent this event.</param>
    /// <param name="suppressHandling">Whether the event should be suppressed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(object sender, bool suppressHandling = false)
    {
        this.Sender = sender;
        this.SuppressHandling = suppressHandling;
    }
}
