using System.Numerics;

namespace Dalamud.NativeUi.Classes;

/// <summary>
/// Data object representing a native addons properties. Akin to the properties saved to DalamudUI.ini.
/// </summary>
internal class AddonConfig
{
    /// <summary>
    /// Gets or sets the opening position for this addon.
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the scale for this addon.
    /// </summary>
    public float Scale { get; set; } = 1.0f;
}
