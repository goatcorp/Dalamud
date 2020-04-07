using System;
using System.ComponentModel;

namespace Dalamud.Bootstrap.Windows
{
    /// <summary>
    /// An exception that is thrown when there was an error while interacting with the process.
    /// </summary>
    internal sealed class ProcessException : Exception
    {
        internal ProcessException() : base() { }

        internal ProcessException(string message) : base(message) { }

        internal ProcessException(string message, Exception innerException) : base(message, innerException) { }

        internal static void ThrowLastOsError(string message)
        {
            var inner = new Win32Exception();
            throw new ProcessException(message, inner);
        }
    }
}
