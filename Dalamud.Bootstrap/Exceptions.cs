using System;

namespace Dalamud.Bootstrap
{
    /// <summary>
    /// An error that is thrown when bootstraping Dalamud failed.
    /// </summary>
    public class BootstrapException : Exception
    {
        internal BootstrapException() : base() { }

        internal BootstrapException(string message) : base(message) { }

        internal BootstrapException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class NtException : BootstrapException
    {

    }
}
