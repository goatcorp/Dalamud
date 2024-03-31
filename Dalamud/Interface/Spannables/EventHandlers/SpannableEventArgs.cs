using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.ObjectPool;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Specifies when is an event handler being called.</summary>
[Flags]
public enum SpannableEventStep
{
    /// <summary>This event argument is invalid.</summary>
    None = 0,

    /// <summary>This event is directly targeted.</summary>
    DirectTarget = 1 << 1,

    /// <summary>This event argument is being called before being dispatched to children.</summary>
    BeforeChildren = 1 << 2,

    /// <summary>This event argument is being called after being dispatched to children.</summary>
    AfterChildren = 1 << 3,
}

/// <summary>Base class for control events.</summary>
[SuppressMessage("ReSharper", "NotNullOrRequiredMemberIsNotInitialized", Justification = "Pooled object")]
public record SpannableEventArgs : IResettable
{
    /// <summary>Gets the object that generated the event.</summary>
    public object Sender { get; private set; }

    /// <summary>Gets the event step.</summary>
    public SpannableEventStep Step { get; private set; }

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
    /// <param name="step">Event step.</param>
    /// <param name="suppressHandling">Whether the event should be suppressed.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Initialize(object sender, SpannableEventStep step, bool suppressHandling = false)
    {
        this.Sender = sender;
        this.Step = step;
        this.SuppressHandling = suppressHandling;
    }
}
