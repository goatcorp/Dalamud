using System.Numerics;

namespace Dalamud.Interface.Spannables.EventHandlers;

/// <summary>Arguments for <see cref="SpannableMeasureEventHandler"/>.</summary>
public record SpannableMeasureEventArgs : SpannableEventArgs
{
    /// <summary>Gets the preferred size.</summary>
    public Vector2 PreferredSize { get; private set; }

    /// <summary>Initializes the measure event properties.</summary>
    /// <param name="preferredSize">The preferred size.</param>
    public void InitializeMeasureEvent(Vector2 preferredSize)
    {
        this.PreferredSize = preferredSize;
    }
}

/// <summary>Event handler for measure event.</summary>
/// <param name="args">Arguments.</param>
public delegate void SpannableMeasureEventHandler(SpannableMeasureEventArgs args);
