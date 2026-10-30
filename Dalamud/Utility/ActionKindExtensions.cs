using Dalamud.Game;

namespace Dalamud.Utility;

#pragma warning disable SA1101

/// <summary>
/// Extension methods for the <see cref="ActionKind"/> enum.
/// </summary>
public static class ActionKindExtensions
{
    extension(ActionKind kind)
    {
        /// <summary>
        /// Converts the id of an ActionKind to the id used in the ActStr sheet redirect.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>An id that can be used in the ActStr sheet redirect.</returns>
        public uint GetActStrId(uint id)
        {
            // See ToActStrId in CS
            var idx = (uint)kind;

            if (idx is <= 13 or 19 or 20)
                return id + (1000000 * idx);

            return 0;
        }
    }
}
