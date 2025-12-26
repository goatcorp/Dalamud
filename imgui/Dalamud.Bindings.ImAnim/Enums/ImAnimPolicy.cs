namespace Dalamud.Bindings.ImAnim;

public enum ImAnimPolicy
{
    /// <summary>
    /// smooth into new target
    /// </summary>
    Crossfade,

    /// <summary>
    /// snap to target
    /// </summary>
    Cut,

    /// <summary>
    /// queue one pending target
    /// </summary>
    Queue,
}
