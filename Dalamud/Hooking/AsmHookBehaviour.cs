namespace Dalamud.Hooking;

/// <summary>
/// Defines the behaviour used by the Dalamud.Hooking.AsmHook.
/// This is equivalent to the same enumeration in Reloaded and is included so you do not have to reference the assembly.
/// </summary>
public enum AsmHookBehaviour
{
    /// <summary>
    /// Executes your assembly code before the original.
    /// </summary>
    ExecuteFirst = 0,

    /// <summary>
    /// Executes your assembly code after the original.
    /// </summary>
    ExecuteAfter = 1,

    /// <summary>
    /// Do not execute original replaced code (Dangerous!).
    /// </summary>
    DoNotExecuteOriginal = 2,
}
