using System;
using System.ComponentModel;

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

    public class ProcessException : BootstrapException
    {
        public uint Pid { get; }

        internal ProcessException() : base() { }

        internal ProcessException(string message) : base(message) { }

        internal ProcessException(string message, Exception innerException) : base(message, innerException) { }

        internal ProcessException(string message, uint pid) : base(message) => Pid = pid;
        
        internal ProcessException(string message, uint pid, Exception innerException) : base(message, innerException) => Pid = pid;

        internal static ProcessException ThrowLastOsError(uint pid)
        {
            var inner = new Win32Exception();

            const string message = "";
            throw new ProcessException(message, pid, inner);
        }
    }
}
