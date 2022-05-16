using System;

namespace Dalamud.Injector.Exceptions;

/// <summary>
/// Exception thrown when the process has exited before a window could be found.
/// </summary>
internal class GameExitedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GameExitedException"/> class.
    /// </summary>
    public GameExitedException()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameExitedException"/> class with a specified error.
    /// message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference if no inner exception is specified.
    /// </param>
    public GameExitedException(Exception? innerException)
        : base("Game exited prematurely.", innerException)
    {
    }
}
