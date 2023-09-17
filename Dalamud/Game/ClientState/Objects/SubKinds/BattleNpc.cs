using Dalamud.Game.ClientState.Objects.Enums;

namespace Dalamud.Game.ClientState.Objects.Types;

/// <summary>
/// This class represents a battle NPC.
/// </summary>
public unsafe class BattleNpc : BattleChara
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BattleNpc"/> class.
    /// Set up a new BattleNpc with the provided memory representation.
    /// </summary>
    /// <param name="address">The address of this actor in memory.</param>
    internal BattleNpc(IntPtr address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the BattleNpc <see cref="BattleNpcSubKind" /> of this BattleNpc.
    /// </summary>
    public BattleNpcSubKind BattleNpcKind => (BattleNpcSubKind)this.Struct->Character.GameObject.SubKind;

    /// <inheritdoc/>
    public override ulong TargetObjectId => this.Struct->Character.TargetId;
}
