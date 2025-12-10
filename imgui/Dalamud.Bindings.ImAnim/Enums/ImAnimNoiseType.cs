namespace Dalamud.Bindings.ImAnim;

public enum ImAnimNoiseType
{
    /// <summary>
    /// Classic Perlin noise
    /// </summary>
    Perlin,

    /// <summary>
    /// Simplex noise (faster, fewer artifacts)
    /// </summary>
    Simplex,

    /// <summary>
    /// Value noise (blocky)
    /// </summary>
    Value,

    /// <summary>
    /// Worley/cellular noise
    /// </summary>
    Worley,
}
