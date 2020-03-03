using System;

namespace Dalamud.Injector.Windows
{
    internal sealed class NtException : Exception
    {
        public NtStatus Status { get; }

        public NtException(NtStatus status)
        {
            Status = status;
        }
    }
}
