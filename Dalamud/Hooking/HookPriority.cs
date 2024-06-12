namespace Dalamud.Hooking;

/// <summary>
/// Defines the priorities available at hook creation.
/// </summary>
public enum HookPriority
{
    /// <summary>
    /// Notify-only hook that runs last and cannot call its original function.
    /// </summary>
    AfterNotify = 0,

    /// <summary>
    /// Normal priority hook, that will run after Dalamud internal hooks.
    /// </summary>
    NormalPriority = 1,

    /// <summary>
    /// High Priority hook that runs before Dalamud internal and normal priority hooks.
    /// </summary>
    HighPriority = 2,

    /// <summary>
    /// Notify-only hook that always runs first and cannot call its original function.
    /// </summary>
    BeforeNotify = 3,
}
