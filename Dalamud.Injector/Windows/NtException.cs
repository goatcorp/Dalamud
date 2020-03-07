using System;

namespace Dalamud.Injector.Windows
{
    /// <summary>
    /// An exception that is thrown when the function call to ntdll failed.
    /// </summary>
    internal sealed class NtException : Exception
    {
        private const string DefaultMessage = "TODO: NtStatus failed message goes here";

        public NtStatus Status { get; }

        public NtException(NtStatus status) : base(DefaultMessage)
        {
            Status = status;
        }

        public NtException(NtStatus status, string message) : base(message)
        {
            Status = status;
        }
    }
}
