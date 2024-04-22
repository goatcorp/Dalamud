using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// This class represents an EventObj.
/// </summary>
public unsafe class EventObj : GameObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventObj"/> class.
    /// Set up a new EventObj with the provided memory representation.
    /// </summary>
    /// <param name="address">The address of this event object in memory.</param>
    internal EventObj(IntPtr address)
        : base(address)
    {
    }
}
