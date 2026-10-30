using Dalamud.Game.ClientState.Objects.Enums;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension methods for the <see cref="ObjectKind"/> enum.
/// </summary>
public static class ObjectKindExtensions
{
    extension(ObjectKind kind)
    {
        /// <summary>
        /// Converts the id of an ObjectKind to the id used in the ObjStr sheet redirect.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>An id that can be used in the ObjStr sheet redirect.</returns>
        public uint GetObjStrId(uint id)
        {
            // See ToObjStrId in CS
            return kind switch
            {
                ObjectKind.BattleNpc => id < 1000000 ? id : id - 900000,
                ObjectKind.EventNpc => id,
                ObjectKind.Treasure or
                ObjectKind.Aetheryte or
                ObjectKind.GatheringPoint or
                ObjectKind.Companion or
                ObjectKind.HousingEventObject => id + (1000000 * (uint)kind) - 2000000,
                ObjectKind.EventObj => id + (1000000 * (uint)kind) - 4000000,
                ObjectKind.CardStand => id + 3000000,
                _ => 0,
            };
        }
    }
}
