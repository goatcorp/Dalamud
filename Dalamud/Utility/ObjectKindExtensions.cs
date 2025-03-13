using Dalamud.Game.ClientState.Objects.Enums;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for the <see cref="ObjectKind"/> enum.
/// </summary>
public static class ObjectKindExtensions
{
    /// <summary>
    /// Converts the id of an ObjectKind to the id used in the ObjStr sheet redirect.
    /// </summary>
    /// <param name="objectKind">The ObjectKind this id is for.</param>
    /// <param name="id">The id.</param>
    /// <returns>An id that can be used in the ObjStr sheet redirect.</returns>
    public static uint GetObjStrId(this ObjectKind objectKind, uint id)
    {
        // See "8D 41 FE 83 F8 0C 77 4D"
        return objectKind switch
        {
            ObjectKind.BattleNpc => id < 1000000 ? id : id - 900000,
            ObjectKind.EventNpc => id,
            ObjectKind.Treasure or
            ObjectKind.Aetheryte or
            ObjectKind.GatheringPoint or
            ObjectKind.Companion or
            ObjectKind.Housing => id + (1000000 * (uint)objectKind) - 2000000,
            ObjectKind.EventObj => id + (1000000 * (uint)objectKind) - 4000000,
            ObjectKind.CardStand => id + 3000000,
            _ => 0,
        };
    }
}
