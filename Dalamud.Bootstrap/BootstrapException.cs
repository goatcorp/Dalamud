using System;

namespace Dalamud.Bootstrap
{
    public class BootstrapException : Exception
    {
        internal BootstrapException() : base() { }

        internal BootstrapException(string message) : base(message) { }

        internal BootstrapException(string message, Exception innerException) : base(message, innerException) { }
    }
}
