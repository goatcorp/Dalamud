using System;

namespace Dalamud.Injector.Exceptions;

/// <summary>
/// An exception thrown during command line argument parsing.
/// </summary>
internal class CommandLineException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandLineException"/> class.
    /// </summary>
    /// <param name="cause">Cause of the error.</param>
    public CommandLineException(string cause)
        : base(cause)
    {
    }
}
