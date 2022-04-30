using System;
using System.Collections.Generic;

using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Utility
{
    /// <summary>
    /// Extension methods for working with IntPtr.
    /// </summary>
    public static class IntPtrExtensions
    {
        /// <summary>
        /// Reads a terminated pointer into a byte array.
        /// </summary>
        /// <param name="memory">pointer.</param>
        /// <returns>byte array.</returns>
        public static unsafe byte[] ReadTerminated(this IntPtr memory)
        {
            if (memory == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }

            var buf = new List<byte>();
            var ptr = (byte*)memory;
            while (*ptr != 0)
            {
                buf.Add(*ptr);
                ptr += 1;
            }

            return buf.ToArray();
        }

        /// <summary>
        /// Reads a pointer into an SeString.
        /// </summary>
        /// <param name="memory">pointer.</param>
        /// <returns>SeString.</returns>
        public static SeString ReadSeString(this IntPtr memory)
        {
            var terminated = ReadTerminated(memory);
            return SeString.Parse(terminated);
        }
    }
}
