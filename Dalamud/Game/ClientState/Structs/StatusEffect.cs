using System.Runtime.InteropServices;

namespace Dalamud.Game.ClientState.Structs;

/// <summary>
/// Native memory representation of a FFXIV status effect.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct StatusEffect
{
    /// <summary>
    /// The effect ID.
    /// </summary>
    public short EffectId;

    /// <summary>
    /// How many stacks are present.
    /// </summary>
    public byte StackCount;

    /// <summary>
    /// Additional parameters.
    /// </summary>
    public byte Param;

    /// <summary>
    /// The duration remaining.
    /// </summary>
    public float Duration;

    /// <summary>
    /// The ID of the actor that caused this effect.
    /// </summary>
    public int OwnerId;
}
