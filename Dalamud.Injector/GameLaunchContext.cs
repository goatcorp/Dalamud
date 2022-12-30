namespace Dalamud.Injector;

public record GameLaunchContext
{
    /// <summary>
    /// The working directory.
    /// </summary>
    public string WorkingDir { get; init; } = null!;

    /// <summary>
    /// The path to the executable file
    /// </summary>
    public string ExePath { get; init; } = null!;

    /// <summary>
    /// Arguments to pass to the executable file.
    /// </summary>
    public string Arguments { get; init; } = null!;

    /// <summary>
    /// Don't actually fix the ACL.
    /// </summary>
    public bool DontFixAcl { get; init; } = true;

    /// <summary>
    /// Wait for the game window to be ready before proceeding.
    /// </summary>
    public bool WaitForGameWindow { get; init; } = true;

    /// <summary>
    /// Launch executable under an appcontainer.
    /// </summary>
    public bool UseAppContainer { get; init; } = false;
}
