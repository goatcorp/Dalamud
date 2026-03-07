using System.Collections.Generic;

using Dalamud.Game.NativeWrapper;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace Dalamud.Game.Agent.AgentArgTypes;

/// <summary>
/// Agent argument data for ReceiveEvent events.
/// </summary>
public class AgentReceiveEventArgs : AgentArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentReceiveEventArgs"/> class.
    /// </summary>
    internal AgentReceiveEventArgs()
    {
    }

    /// <inheritdoc/>
    public override AgentArgsType Type => AgentArgsType.ReceiveEvent;

    /// <summary>
    /// Gets or sets the AtkValue return value for this event message.
    /// </summary>
    public nint ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the AtkValue array for this event message.
    /// </summary>
    public nint AtkValues { get; set; }

    /// <summary>
    /// Gets or sets the AtkValue count for this event message.
    /// </summary>
    public uint ValueCount { get; set; }

    /// <summary>
    /// Gets or sets the event kind for this event message.
    /// </summary>
    public ulong EventKind { get; set; }

    /// <summary>
    /// Gets an enumerable collection of <see cref="AtkValuePtr"/> of the event's AtkValues.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> of <see cref="AtkValuePtr"/> corresponding to the event's AtkValues.
    /// </returns>
    public IEnumerable<AtkValuePtr> AtkValueEnumerable
    {
        get
        {
            for (var i = 0; i < this.ValueCount; i++)
            {
                AtkValuePtr ptr;
                unsafe
                {
                    ptr = new AtkValuePtr((nint)this.AtkValueSpan.GetPointer(i));
                }

                yield return ptr;
            }
        }
    }

    /// <summary>
    /// Gets the AtkValues in the form of a span.
    /// </summary>
    internal unsafe Span<AtkValue> AtkValueSpan => new(this.AtkValues.ToPointer(), (int)this.ValueCount);
}
