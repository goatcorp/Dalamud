using Dalamud.Game;

namespace Dalamud.Utility;

/// <summary>
/// Extension methods for the <see cref="ActionKind"/> enum.
/// </summary>
public static class ActionKindExtensions
{
    /// <summary>
    /// Converts the id of an ActionKind to the id used in the ActStr sheet redirect.
    /// </summary>
    /// <param name="actionKind">The ActionKind this id is for.</param>
    /// <param name="id">The id.</param>
    /// <returns>An id that can be used in the ActStr sheet redirect.</returns>
    public static uint GetActStrId(this ActionKind actionKind, uint id)
    {
        // See "83 F9 0D 76 0B"
        var idx = (uint)actionKind;

        if (idx is <= 13 or 19 or 20)
            return id + (1000000 * idx);

        return 0;
    }
}
