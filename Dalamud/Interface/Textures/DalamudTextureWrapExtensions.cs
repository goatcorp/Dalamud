using Dalamud.Interface.Internal;

namespace Dalamud.Interface.Textures;

/// <summary>Extension methods for <see cref="IDalamudTextureWrap"/>.</summary>
public static class DalamudTextureWrapExtensions
{
    /// <summary>Checks if two instances of <see cref="IDalamudTextureWrap"/> point to a same underlying resource.
    /// </summary>
    /// <param name="a">The resource 1.</param>
    /// <param name="b">The resource 2.</param>
    /// <returns><c>true</c> if both instances point to a same underlying resource.</returns>
    public static bool ResourceEquals(this IDalamudTextureWrap? a, IDalamudTextureWrap? b)
    {
        if (a is null != b is null)
            return false;
        if (a is null)
            return false;
        return a.ImGuiHandle == b.ImGuiHandle;
    }
}
