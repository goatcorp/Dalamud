using System;

namespace Dalamud.Injector
{
    /// <summary>
    /// An error that is thrown when injecting Dalamud into the process failed.
    /// </summary>
    public sealed partial class DalamudLauncherException : Exception
    {
        /// <summary>
        /// A target process id that was attempted to.
        /// </summary>
        public uint ProcessId { get; }
    }

    public sealed partial class DalamudLauncherException
    {
        public DalamudLauncherException() : base() { }

        public DalamudLauncherException(string message) : base(message) { }

        public DalamudLauncherException(string message, Exception inner) : base(message, inner) { }

        public DalamudLauncherException(uint pid, string message, Exception inner) : base(message, inner)
        {
            ProcessId = pid;
        }
    }
}
