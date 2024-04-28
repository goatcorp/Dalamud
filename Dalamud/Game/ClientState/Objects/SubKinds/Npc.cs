using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// This class represents a NPC.
/// </summary>
public unsafe class Npc : Character
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Npc"/> class.
    /// Set up a new NPC with the provided memory representation.
    /// </summary>
    /// <param name="address">The address of this actor in memory.</param>
    internal Npc(IntPtr address)
        : base(address)
    {
    }
}
