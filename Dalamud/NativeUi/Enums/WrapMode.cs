using Dalamud.NativeUi.Nodes;

namespace Dalamud.NativeUi.Enums;

/// <summary>
/// Wrap Mode flags for <see cref="ImageNode"/>.
/// </summary>
/// <remarks>
/// For most cases you likely just want to use <see cref="ImageNode.FitTexture"/>.
/// </remarks>
internal enum WrapMode
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Tile.
    /// </summary>
    Tile = 1,

    /// <summary>
    /// Stretch.
    /// </summary>
    Stretch = 2,

    /// <summary>
    /// TileMirrored.
    /// </summary>
    TileMirrored = 3,
}
