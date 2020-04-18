using System;

namespace Dalamud.Bootstrap
{
    public class BootstraperException : BootstrapException
    {
        internal BootstraperException() : base() { }

        internal BootstraperException(string message) : base(message) { }

        internal BootstraperException(string message, Exception innerException) : base(message, innerException) { }
    }
}
