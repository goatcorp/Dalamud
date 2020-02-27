using System;
using System.Collections.Generic;
using System.Text;

namespace Dalamud.Injector
{
    public class DalamudException : Exception
    {
        public DalamudException() : base() { }

        public DalamudException(string message) : base(message) { }

        public DalamudException(string message, Exception inner) : base(message, inner) { }
    }

    public partial class DalamudProcessException : DalamudException
    {
        public uint ProcessId { get; }
    }

    public partial class DalamudProcessException
    {
        public DalamudProcessException(uint pid, string message) : base(message)
        {
            ProcessId = pid;
        }
    }
}
