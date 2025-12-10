using System.Runtime.InteropServices;

namespace Dalamud.Bindings.ImAnim;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImAnimNoiseOpts
{
    public ImAnimNoiseType Type;

    /// <summary>
    /// Number of octaves for fractal noise (1-8)
    /// </summary>
    public int Octaves;

    /// <summary>
    /// Amplitude multiplier per octave (0.0-1.0)
    /// </summary>
    public float Persistence;

    /// <summary>
    /// Frequency multiplier per octave (typically 2.0)
    /// </summary>
    public float Lacunarity;

    /// <summary>
    /// Random seed for noise generation
    /// </summary>
    public int Seed;
}
