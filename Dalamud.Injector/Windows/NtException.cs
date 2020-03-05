using System;

namespace Dalamud.Injector.Windows
{
    internal sealed class NtStatusException : Exception
    {
        public NtStatus Status { get; }

        public NtStatusException(NtStatus status)
        {
            Status = status;
        }
    }
}
