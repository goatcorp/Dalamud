namespace Dalamud.Bindings.ImAnim;

public enum ImAnimWaveType
{
    /// <summary>
    /// Smooth sine wave
    /// </summary>
    Sine,

    /// <summary>
    /// Triangle wave (linear up/down)
    /// </summary>
    Triangle,

    /// <summary>
    /// Sawtooth wave (linear up, instant reset)
    /// </summary>
    Sawtooth,

    /// <summary>
    /// Square wave (on/off pulse)
    /// </summary>
    Square,
}
