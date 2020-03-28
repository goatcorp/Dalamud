using System;
using System.ComponentModel;

namespace Dalamud.Bootstrap
{
    /// <summary>
    /// An error that is thrown when there was a problem with bootstraping.
    /// </summary>
    public sealed class BootstrapException : Exception
    {
        internal BootstrapException() : base() { }

        internal BootstrapException(string message) : base(message) { }

        internal BootstrapException(string message, Exception innerException) : base(message, innerException) { }
    }
}
