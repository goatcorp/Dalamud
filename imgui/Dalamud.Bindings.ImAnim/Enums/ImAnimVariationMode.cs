namespace Dalamud.Bindings.ImAnim;

public enum ImAnimVariationMode
{
    /// <summary>
    /// No variation
    /// </summary>
    None,

    /// <summary>
    /// Add amount each iteration
    /// </summary>
    Increment,

    /// <summary>
    /// Subtract amount each iteration
    /// </summary>
    Decrement,

    /// <summary>
    /// Multiply by factor each iteration
    /// </summary>
    Multiply,

    /// <summary>
    /// Random in range [-amount, +amount]
    /// </summary>
    Random,

    /// <summary>
    /// Random in range [0, amount]
    /// </summary>
    RandomAbs,

    /// <summary>
    /// Alternate +/- each iteration
    /// </summary>
    PingPong,

    /// <summary>
    /// Use custom callback
    /// </summary>
    Callback,
}
