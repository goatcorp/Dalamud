using Dalamud.Game.ClientState.Objects.Types;

namespace Dalamud.Game.ClientState.Objects.SubKinds;

/// <summary>
/// This interface represents a NPC.
/// </summary>
public interface INpc : ICharacter
{
}

/// <summary>
/// This class represents a NPC.
/// </summary>
internal class Npc : Character, INpc
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
