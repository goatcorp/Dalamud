using System;
using System.ComponentModel;

namespace Dalamud.Bootstrap.Windows
{
    /// <summary>
    /// An exception that is thrown when there was an error while interacting with the process.
    /// </summary>
    public class ProcessException : Exception
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
